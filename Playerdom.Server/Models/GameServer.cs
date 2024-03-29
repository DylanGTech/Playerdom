﻿using Playerdom.Server.Models;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Playerdom.Shared.GameEntities;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Playerdom.Models;

public class GameServer : IDisposable
{
    public const string DIMENSIONS_FOLDER_NAME = "Dimensions";
    public const string DATABASE_FILE_NAME = "data.db";

    public ConcurrentDictionary<ushort, Dimension> Dimensions { get; init; } = new ConcurrentDictionary<ushort, Dimension>();
    public List<GameServerClient> Clients { get; init; } = new List<GameServerClient>();
    public ConcurrentQueue<ChatMessage> MessageQueue { get; init; } = new ConcurrentQueue<ChatMessage>();

    public ConcurrentQueue<(GameServerClient, ushort)> DimensionalShiftQueue { get; init; } = new ConcurrentQueue<(GameServerClient, ushort)>();


    public SqliteConnection connection
    {
        get;
        private set;
    } = null;

    public delegate void LogFunction(string s);
    public LogFunction logger;

    private TcpListener listener;
    private readonly Thread acceptClientsThread;
    private readonly Thread updateThread;

    public bool IsRunning { get; private set; } = false;

    public DateTime LastSaveTime { get; private set; } = DateTime.Now;

    public readonly string saveDirectoryPath;

    public GameServer(LogFunction logger, string saveDirectoryPath)
    {
        IsRunning = false;
        acceptClientsThread = new Thread(AcceptClients);
        updateThread = new Thread(UpdateAll);
        this.logger = logger;
        this.saveDirectoryPath = saveDirectoryPath;
    }

    public void Start()
    {
        if (!Directory.Exists(this.saveDirectoryPath)) Directory.CreateDirectory(this.saveDirectoryPath);

        string dbFilePath = Path.Combine(saveDirectoryPath, DATABASE_FILE_NAME);

        SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder()
        {
            DataSource = dbFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        connection = new SqliteConnection(builder.ToString());

        connection.Open();
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS players (
                        id INTEGER PRIMARY KEY,
                        username TEXT UNIQUE NOT NULL,
                        password TEXT NOT NULL,
                        posx REAL NOT NULL,
                        posy REAL NOT NULL,
                        dimension INTEGER NOT NULL,
                        level INTEGER NOT NULL,
                        health INTEGER NOT NULL
                    )
                ";
            command.ExecuteReader();
        }

        LoadDimension(0);

        if (acceptClientsThread.ThreadState == ThreadState.Unstarted && acceptClientsThread.ThreadState == ThreadState.Unstarted)
        {
            IsRunning = true;
            acceptClientsThread.Start();
            updateThread.Start();
        }
    }

    public void RemoveClient(GameServerClient client)
    {
        lock (Clients)
        {
            if (Clients.Contains(client))
            {
                client.SavePlayer();

                logger("Player left from IP " + client.EndPointString);
                Clients.Remove(client);

                if (client.FocusedObjectId.HasValue && Dimensions.ContainsKey(client.DimensionId)) Dimensions[client.DimensionId].Map.LoadedObjects.TryRemove(client.FocusedObjectId.Value, out GameObject value);
                client.Dispose();
            }
        }
    }

    private void AcceptClients()
    {
        listener = new TcpListener(IPAddress.IPv6Any, 25565);
        listener.Start();

        logger("Server started on port " + listener.LocalEndpoint.ToString());

        while (IsRunning)
        {
            if (!listener.Pending())
            {
                Thread.Sleep(500);
                continue;
            }
            TcpClient tcpClient = listener.AcceptTcpClient();

            GameServerClient sc = new GameServerClient(tcpClient, this);
            lock (Clients)
            {
                Clients.Add(sc);
                sc.Start();
            }
        }
    }

    private void UpdateAll()
    {
        while (IsRunning)
        {
            lock (Clients)
            {
                for (int i = Clients.Count - 1; i >= 0; i--)
                {
                    GameServerClient client = Clients[i];

                    if (client.LastUpdate.AddSeconds(45) > DateTime.Now)
                    {
                        if (Dimensions.ContainsKey(client.DimensionId))
                        {
                            lock (Dimensions[client.DimensionId].Map.LoadedObjects)
                            {
                                if (client.FocusedObjectId.HasValue && Dimensions[client.DimensionId].Map.LoadedObjects.TryGetValue(client.FocusedObjectId.Value, out GameObject obj))
                                    (obj as Player).Keyboard = client.InputState;
                            }
                        }

                        if (client.InputState != null && client.InputState.Contains(Keys.Q)
                            && client.FocusedObjectId.HasValue && Dimensions[client.DimensionId].Map.LoadedObjects.TryGetValue(client.FocusedObjectId.Value, out GameObject gameObj1))
                        {
                            lock (gameObj1)
                            {
                                if (Dimensions[client.DimensionId].Map.TryGetTile(gameObj1.Coordinates, out Tile tile) && tile.TypeId == 5)
                                    DimensionalShiftQueue.Enqueue((client, (ushort)(client.DimensionId + 1)));
                            }
                        }
                        else if (client.InputState != null && client.InputState.Contains(Keys.E)
                            && client.FocusedObjectId.HasValue && Dimensions[client.DimensionId].Map.LoadedObjects.TryGetValue(client.FocusedObjectId.Value, out GameObject gameObj2))
                        {
                            lock (gameObj2)
                            {
                                if (Dimensions[client.DimensionId].Map.TryGetTile(gameObj2.Coordinates, out Tile tile) && tile.TypeId == 5)
                                    DimensionalShiftQueue.Enqueue((client, (ushort)(client.DimensionId - 1)));
                            }
                        }
                    }
                    else RemoveClient(client);
                }
            }

            while (MessageQueue.TryDequeue(out ChatMessage message))
            {
                if (message.Content == null) continue;

                if (message.Content.StartsWith('/') && message.MessageType != ChatMessageTypes.Server)
                {
                    if (message.Content.StartsWith("/tp"))
                    {
                        string[] args = message.Content.Remove(0, 1).Split(" ");
                        if (args.Length == 3 && double.TryParse(args[1], out double xCoord)
                            && double.TryParse(args[2], out double yCoord)
                            && Dimensions.TryGetValue(message.DimensionSent, out Dimension d)
                            && d.Map.LoadedObjects.TryGetValue(message.SenderObjectId, out GameObject teleportedObject))
                            teleportedObject.Coordinates = (xCoord, yCoord);
                    }
                    else if (message.Content.StartsWith("/ascend") && message.SenderObjectId != Guid.Empty)
                    {
                        GameServerClient client = Clients.First(c => c.FocusedObjectId == message.SenderObjectId);
                        DimensionalShiftQueue.Enqueue((client, (ushort)(client.DimensionId + 1)));
                    }
                    else if (message.Content.StartsWith("/descend") && message.SenderObjectId != Guid.Empty)
                    {
                        GameServerClient client = Clients.First(c => c.FocusedObjectId == message.SenderObjectId);
                        DimensionalShiftQueue.Enqueue((client, (ushort)(client.DimensionId - 1)));
                    }
                    continue;
                }

                logger($"{message.TimeSent.ToLocalTime().ToShortTimeString()} - {message.Sender}: {message.Content}");

                if (message.MessageScope == ChatMessageScopes.Global)
                {
                    foreach (GameServerClient client in Clients)
                    {
                        client.MessageOutbox.Enqueue(message);
                    }
                }
                else if (message.MessageScope == ChatMessageScopes.Dimension)
                {
                    foreach (GameServerClient client in Clients)
                    {
                        if (client.DimensionId == message.DimensionSent)
                            client.MessageOutbox.Enqueue(message);
                    }
                }
                else if (message.MessageScope == ChatMessageScopes.Party)
                {
                    //TODO
                }
                else
                {
                    foreach (GameServerClient client in Clients)
                    {
                        if (client.DimensionId != message.DimensionSent) continue;

                        Guid? objectId = client.FocusedObjectId;
                        if (objectId.HasValue && Dimensions[message.DimensionSent].Map.LoadedObjects.TryGetValue(objectId.Value, out GameObject gameObject))
                        {
                            if (Math.Sqrt(Math.Pow(gameObject.Coordinates.Item1 - message.PlaceSent.X, 2) + Math.Pow(gameObject.Coordinates.Item2 - message.PlaceSent.Y, 2)) < Chunk.SIZE * 2)
                                client.MessageOutbox.Enqueue(message);
                        }
                    }
                }
            }

            foreach ((ushort id, Dimension d) in Dimensions)
            {
                lock (d.Map.LoadedObjects)
                {
                    List<(long, long)> playerChunkPositions = new List<(long, long)>();
                    foreach ((Guid key, GameObject value) in d.Map.LoadedObjects)
                    {
                        value.Update(d.Map, key);

                        if (value.GetType() == typeof(Player))
                        {
                            (long, long) p = Map.PositionToChunkIndex(value.Coordinates);
                            playerChunkPositions.Add((p.Item1, p.Item2));

                        }
                    }
                    foreach ((Guid key, GameEntity value) in d.Map.LoadedEntities)
                    {
                        value.Update(d.Map, key);
                    }


                    List<(long, long)> chunksToBeUnloaded = new List<(long, long)>();
                    foreach (((long, long) coords, Chunk c) in d.Map.LoadedChunks)
                    {
                        bool shouldStayLoaded = false;

                        if (playerChunkPositions.Count > 0)
                        {
                            foreach ((long, long) pos in playerChunkPositions)
                            {
                                if (Math.Abs(coords.Item1 - pos.Item1) <= Map.CHUNK_RANGE
                                    && Math.Abs(coords.Item2 - pos.Item2) <= Map.CHUNK_RANGE)
                                {
                                    shouldStayLoaded = true;
                                    break;
                                }
                            }
                        }

                        if (!shouldStayLoaded) chunksToBeUnloaded.Add(coords);
                    }

                    foreach ((long, long) coords in chunksToBeUnloaded)
                    {
                        d.Map.UnloadChunk(coords.Item1, coords.Item2, Path.Combine(this.saveDirectoryPath, DIMENSIONS_FOLDER_NAME, id.ToString()));
                    }
                }
            }




            while (DimensionalShiftQueue.TryDequeue(out (GameServerClient c, ushort d) result))
            {
                result.c.ChangeDimensions(result.d);
            }

            if ((DateTime.Now - LastSaveTime).TotalSeconds >= 60)
            {
                SavePlayers();
                LastSaveTime = DateTime.Now;
            }

            Task.Delay(5).Wait();
        }
    }


    private void SavePlayers()
    {
        foreach (GameServerClient c in Clients)
        {
            c.SavePlayer();
        }
    }

    public void LoadDimension(ushort id)
    {
        string dimensionsPath = Path.Combine(this.saveDirectoryPath, DIMENSIONS_FOLDER_NAME);
        if (!Directory.Exists(dimensionsPath)) Directory.CreateDirectory(dimensionsPath);

        Dimensions.TryAdd(id, new Dimension("Hello", id));
        if (!Directory.Exists(Path.Combine(dimensionsPath, id.ToString())))
            Directory.CreateDirectory(Path.Combine(dimensionsPath, id.ToString()));
    }

    public void UnloadDimension(ushort id)
    {
        Dimensions.TryRemove(id, out Dimension value);
    }


    public void Dispose()
    {
        IsRunning = false;
        if (connection != null) connection.Dispose();
    }
}