﻿using Playerdom.Models;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using MessagePack;
using System.Buffers;
using System.Threading;
using Playerdom.Shared.GameEntities;
using Microsoft.Data.Sqlite;

namespace Playerdom.Server.Models;

public class GameServerClient : IDisposable
{
    readonly TcpClient _tcpClient;
    readonly NetworkStream _netStream;

    readonly GameServer server;

    public long? UserId { get; set; }
    public string EndPointString => _tcpClient.Client.RemoteEndPoint.ToString();
    public DateTime LastUpdate { get; set; }
    public Guid? FocusedObjectId { get; set; }
    public Keys[] InputState { get; set; }
    public ushort DimensionId { get; set; } = 0;
    public string Name { get; set; }
    public ConcurrentQueue<ChatMessage> MessageOutbox { get; set; } = new ConcurrentQueue<ChatMessage>();

    private readonly ConcurrentQueue<ClientPack> externalPacks = new ConcurrentQueue<ClientPack>();

    public GameServerClient(TcpClient tcpClient, GameServer server)
    {
        this.server = server;

        _tcpClient = tcpClient;
        _netStream = tcpClient.GetStream();

        FocusedObjectId = null;
        InputState = new Keys[] { };
        LastUpdate = DateTime.Now;
    }

    public void Start()
    {
        this.server.logger("Player connected from IP " + EndPointString);
        StartReceivingMessages();
        StartSendingMessages();
    }

    public void ChangeDimensions(ushort dimensionId)
    {
        server.Dimensions[DimensionId].Map.LoadedObjects.TryRemove(FocusedObjectId.Value, out GameObject p);

        server.LoadDimension(dimensionId);
        server.Dimensions[dimensionId].Map.LoadedObjects.TryAdd(FocusedObjectId.Value, p);
        DimensionId = dimensionId;

        (float hue, float sat, float val) = server.Dimensions[DimensionId].Discolorization;
        ClientPack pack = new ClientPack()
        {
            CurrentMessage = new ServerMessage()
            {
                MessageType = "changeDimension",
                MessageContent = [DimensionId.ToString(), hue.ToString(), sat.ToString(), val.ToString()]
            }
        };
        externalPacks.Enqueue(pack);

        SavePlayer();
    }


    public void InitializePlayer(Player p, ushort dimensionId)
    {
        FocusedObjectId = Guid.NewGuid();

        DimensionId = dimensionId;

        server.LoadDimension(DimensionId);
        server.Dimensions[DimensionId].Map.LoadedObjects.TryAdd(FocusedObjectId.Value, p);
        ClientPack pack = new ClientPack()
        {
            CurrentMessage = new ServerMessage()
            {
                MessageType = "focusedObjectId",
                MessageContent = [FocusedObjectId.Value.ToString()]
            }
        };

        Task.Delay(2000).Wait();
        externalPacks.Enqueue(pack);

        Task.Delay(2000).Wait();
        (float hue, float sat, float val) = server.Dimensions[DimensionId].Discolorization;
        pack = new ClientPack()
        {
            CurrentMessage = new ServerMessage()
            {
                MessageType = "changeDimension",
                MessageContent = [DimensionId.ToString(), hue.ToString(), sat.ToString(), val.ToString()]
            }
        };
        externalPacks.Enqueue(pack);

        SavePlayer();
    }

    private void StartSendingMessages()
    {
        Task.Run(() =>
        {
            while (server.Clients.Contains(this))
            {
                try
                {
                    if (UserId.HasValue && FocusedObjectId.HasValue && server.Dimensions[DimensionId].Map.LoadedObjects.ContainsKey(FocusedObjectId.Value))
                    {
                        GameObject go = server.Dimensions[DimensionId].Map.LoadedObjects[FocusedObjectId.Value];

                        //Deep Clone to make thread-safe
                        //TODO: Make it more efficient
                        ConcurrentDictionary<Guid, GameObject> objClone;
                        ConcurrentDictionary<Guid, GameEntity> entClone;
                        lock (server.Dimensions[DimensionId].Map.LoadedObjects)
                            objClone = MessagePackSerializer.Deserialize<ConcurrentDictionary<Guid, GameObject>>(MessagePackSerializer.Serialize(server.Dimensions[DimensionId].Map.LoadedObjects));

                        lock (server.Dimensions[DimensionId].Map.LoadedEntities)
                            entClone = MessagePackSerializer.Deserialize<ConcurrentDictionary<Guid, GameEntity>>(MessagePackSerializer.Serialize(server.Dimensions[DimensionId].Map.LoadedEntities));


                        ConcurrentDictionary<Guid, GameObject> objCloneToSend = new ConcurrentDictionary<Guid, GameObject>();
                        ConcurrentDictionary<Guid, GameEntity> entCloneToSend = new ConcurrentDictionary<Guid, GameEntity>();


                        (long, long) goChunk = Map.PositionToChunkIndex(go.Coordinates);
                        foreach ((Guid id, GameObject o) in objClone)
                        {
                            (long, long) oChunk = Map.PositionToChunkIndex(o.Coordinates);

                            if (Math.Abs(oChunk.Item1 - goChunk.Item1) <= Map.CHUNK_RANGE && Math.Abs(oChunk.Item2 - goChunk.Item2) <= Map.CHUNK_RANGE)
                                objCloneToSend.TryAdd(id, o);
                        }

                        foreach ((Guid id, GameEntity e) in entClone)
                        {
                            (long, long) eChunk = Map.PositionToChunkIndex(e.Coordinates);

                            if (Math.Abs(eChunk.Item1 - goChunk.Item1) <= Map.CHUNK_RANGE && Math.Abs(eChunk.Item2 - goChunk.Item2) <= Map.CHUNK_RANGE)
                                entCloneToSend.TryAdd(id, e);
                        }

                        lock (go)
                        {
                            while (externalPacks.TryDequeue(out ClientPack result))
                                Send(result);

                            if (objCloneToSend.ContainsKey(FocusedObjectId.Value))
                            {
                                Queue<ChatMessage> messages = new Queue<ChatMessage>();
                                while (MessageOutbox.TryDequeue(out ChatMessage m))
                                {
                                    messages.Enqueue(m);
                                }


                                ClientPack pack = new ClientPack()
                                {
                                    Chunks = server.Dimensions[DimensionId].Map.GetLocalChunks(objCloneToSend[FocusedObjectId.Value].Coordinates.Item1, objCloneToSend[FocusedObjectId.Value].Coordinates.Item2, DimensionId, Path.Combine(server.saveDirectoryPath, GameServer.DIMENSIONS_FOLDER_NAME, DimensionId.ToString()), server.Dimensions[DimensionId].DefaultSeedString),
                                    GameObjects = objCloneToSend,
                                    GameEntities = entCloneToSend,
                                    NewChats = messages
                                };
                                Send(pack);
                            }
                            else
                            {
                                //throw new Exception("Player object removed");
                                //Player object now CAN be removed when the player is shifting Dimensions. Just don't send them anything this round
                            }
                        }
                    }
                    else
                    {
                        while (externalPacks.TryDequeue(out ClientPack result))
                            Send(result);
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(Exception)) server.logger(EndPointString + " disconnected: " + e.Message);
                    server.RemoveClient(this);
                }
                Task.Delay(5).Wait();
            }
        });
    }

    private void StartReceivingMessages()
    {
        Task.Run(async () =>
        {
            try
            {
                using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                {
                    // Keep receiving packets from the client and respond to them
                    // Eventually when the client disconnects we'll just get an exception and end the thread...

                    using (var streamReader = new MessagePackStreamReader(_netStream, true))
                    {
                        while (server.Clients.Contains(this))
                        {

                            if (_netStream.DataAvailable)
                                try
                                {
                                    ReadOnlySequence<byte>? sequence = await streamReader.ReadAsync(tokenSource.Token);
                                    if (sequence.HasValue)
                                    {
                                        ServerPack obj = MessagePackSerializer.Deserialize<ServerPack>(sequence.Value, PlayerdomGame.SerializerSettings, tokenSource.Token);
                                        HandleMessage(obj);

                                    }
                                }
                                catch (MessagePackSerializationException) {}

                            else Task.Delay(10).Wait();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(Exception)) server.logger("Exception thrown: " + e.Message);
                server.RemoveClient(this);
            }
        });
    }
    private void HandleMessage(ServerPack obj)
    {
        InputState = obj.KeysPressed;
        LastUpdate = DateTime.Now;

        if (obj.CurrentMessage == null) return;

        if (obj.CurrentMessage.MessageType == "login" && !FocusedObjectId.HasValue)
        {
            using (SqliteCommand command = server.connection.CreateCommand())
            {
                command.CommandText =
                    @"
                        SELECT * FROM players WHERE username=$username
                    ";
                command.Parameters.AddWithValue("$username", obj.CurrentMessage.MessageContent[0]);

                Name = obj.CurrentMessage.MessageContent[0];

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        lock (server.Clients)
                        {
                            if (GetHash(obj.CurrentMessage.MessageContent[1]) != reader.GetString(2))
                                throw new Exception("Incorrect Password");

                            if (server.Clients.Where(c => c.UserId == reader.GetInt64(0)).Count() > 0)
                                throw new Exception("Player already logged in");
                        }

                        UserId = reader.GetInt64(0);

                        Player p = new Player() { Name = this.Name, Coordinates = (reader.GetDouble(3), reader.GetDouble(4)), Level = (uint)reader.GetInt32(6), Health = (ulong)reader.GetInt64(7) };
                        DimensionId = reader.GetByte(5);
                        InitializePlayer(p, DimensionId);
                    }
                    else
                    {
                        InitializePlayer(new Player() { Name = this.Name, Health = Player.GetMaxHealth(0) }, DimensionId);
                    }
                }

                if (!UserId.HasValue)
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lock (server.Clients)
                            {
                                if (server.Clients.Where(c => c.UserId == reader.GetInt64(0)).Count() > 0)
                                    throw new Exception("Player already logged in");
                            }

                            UserId = reader.GetInt64(0);
                            SetPassword(obj.CurrentMessage.MessageContent[1]);
                        }
                    }
                }
            }
        }
        else if (FocusedObjectId.HasValue)
        {
            if (obj.CurrentMessage.MessageType.StartsWith("chat_") && obj.CurrentMessage.MessageType.Length > "chat_".Length && obj.CurrentMessage.MessageContent.Length == 1)
            {
                ChatMessageScopes scope = ChatMessageScopes.Area;
                switch (obj.CurrentMessage.MessageType.Substring("chat_".Length))
                {
                    default:
                    case "area":
                        break;
                    case "party":
                        scope = ChatMessageScopes.Party;
                        break;
                    case "dimension":
                        scope = ChatMessageScopes.Dimension;
                        break;
                    case "global":
                        scope = ChatMessageScopes.Global;
                        break;
                }

                if (server.Dimensions.TryGetValue(DimensionId, out Dimension d) && d.Map.LoadedObjects.TryGetValue(FocusedObjectId.Value, out GameObject go))
                {
                    server.MessageQueue.Enqueue(new ChatMessage()
                    {
                        Content = obj.CurrentMessage.MessageContent[0],
                        Sender = Name,
                        TimeSent = DateTime.Now,
                        DimensionSent = DimensionId,
                        PlaceSent = go.Coordinates,
                        SenderObjectId = FocusedObjectId.Value,
                        MessageType = ChatMessageTypes.Player,
                        MessageScope = scope
                    });
                }
            }
        }
    }

    private readonly object sendLock = new object();
    private void Send(ClientPack obj)
    {
        lock (sendLock)
        {
            if (server.Clients.Contains(this))
                MessagePackSerializer.Serialize(_netStream, obj, PlayerdomGame.SerializerSettings);
        }
    }

    public void SavePlayer()
    {
        if (server.Dimensions.ContainsKey(DimensionId))
        {
            lock (server.Dimensions[DimensionId].Map.LoadedObjects)
            {
                if (FocusedObjectId.HasValue && server.Dimensions[DimensionId].Map.LoadedObjects.TryGetValue(FocusedObjectId.Value, out GameObject player))
                {
                    using (SqliteCommand command = server.connection.CreateCommand())
                    {
                        command.CommandText =
                            @"
                                INSERT INTO players(username, password, posx, posy, dimension, level, health) VALUES($username, $password, $posx, $posy, $dimension, $level, $health)
                                ON CONFLICT(username) DO UPDATE SET posx=$posx, posy=$posy, dimension=$dimension, level=$level, health=$health
                            ";

                        command.Parameters.AddWithValue("$username", Name);
                        command.Parameters.AddWithValue("$password", "ignored");
                        command.Parameters.AddWithValue("$posx", player.Coordinates.Item1);
                        command.Parameters.AddWithValue("$posy", player.Coordinates.Item2);
                        command.Parameters.AddWithValue("$dimension", DimensionId);
                        command.Parameters.AddWithValue("$level", player.Level);
                        command.Parameters.AddWithValue("$health", player.Health);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    private static string GetHash(string password)
    {
        SHA512 hash = SHA512.Create();
        UnicodeEncoding ue = new UnicodeEncoding();
        return ue.GetString(hash.ComputeHash(ue.GetBytes(password)));
    }

    private void SetPassword(string password)
    {
        using (SqliteCommand command = server.connection.CreateCommand())
        {
            command.CommandText =
                @"
                    UPDATE players SET password=$password WHERE id=$id
                ";

            command.Parameters.AddWithValue("$password", GetHash(password));
            command.Parameters.AddWithValue("$id", UserId.Value);

            command.ExecuteNonQuery();
        }
    }
    public void Dispose()
    {
        _tcpClient.Close();
        _tcpClient.Dispose();
    }
}
