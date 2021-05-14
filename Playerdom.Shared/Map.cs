using MessagePack;
using Microsoft.Xna.Framework;
using Playerdom.Shared.GameEntities;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Playerdom.Shared
{

    [MessagePackObject]
    public class Map
    {
        public const uint CHUNK_RANGE = 1;

        [Key(0)]
        public ConcurrentDictionary<(long, long), Chunk> LoadedChunks
        {
            get; set;
        } = new ConcurrentDictionary<(long, long), Chunk>();

        [Key(1)]
        public ConcurrentDictionary<Guid, GameObject> LoadedObjects
        {
            get; set;
        } = new ConcurrentDictionary<Guid, GameObject>();

        [Key(2)]
        public ConcurrentDictionary<Guid, GameEntity> LoadedEntities
        {
            get; set;
        } = new ConcurrentDictionary<Guid, GameEntity>();



        public Chunk[,] GetLocalChunks(double xPos, double yPos, ushort dimensionId, string dimensionPath, string dimensionRngString)
        {
            Chunk[,] chunks = new Chunk[2 * CHUNK_RANGE + 1, 2 * CHUNK_RANGE + 1];


            //Because position can be negative, division will index negagtive positions improperly. This corrects it.


            //if (xPos == 0.0) xSupplement = 1;
            //if (yPos == 0.0) ySupplement = 1;

            (long, long) currentChunk = PositionToChunkIndex((xPos, yPos));

            lock(LoadedChunks)
            for (long y = -(long)CHUNK_RANGE; y <= (long)CHUNK_RANGE; y++ )
            {
                for (long x = -(long)CHUNK_RANGE; x <= (long)CHUNK_RANGE; x++)
                {
                    chunks[x + CHUNK_RANGE, y + CHUNK_RANGE] = LoadChunk(currentChunk.Item1 + x, currentChunk.Item2 + y, dimensionId, dimensionPath, dimensionRngString);
                }
            }

            return chunks;
        }

        public Chunk LoadChunk(long xCoord, long yCoord, ushort dimensionId, string dimensionPath, string dimensionRngString)
        {
            lock (LoadedChunks)
            {
                //TODO: Generate chunks
                if (!LoadedChunks.ContainsKey((xCoord, yCoord)))
                {
                    string tilesPath = Path.Combine(dimensionPath, "chunks", $"{xCoord}_{yCoord}_tiles.bin");
                    string objectsPath = Path.Combine(dimensionPath, "chunks", $"{xCoord}_{yCoord}_objects.bin");
                    bool chunkSuccessfullyCreated = false;
                    if(File.Exists(tilesPath) && File.Exists(objectsPath))
                    {
                        byte[] tilesData = File.ReadAllBytes(tilesPath);
                        byte[] objectsData = File.ReadAllBytes(objectsPath);

                        try
                        {
                            LoadedChunks.TryAdd((xCoord, yCoord), (Chunk)MessagePackSerializer.Deserialize<Chunk>(tilesData, PlayerdomGame.SerializerSettings));


                            Dictionary<Guid, GameObject> objects = MessagePackSerializer.Deserialize<Dictionary<Guid, GameObject>>(objectsData, PlayerdomGame.SerializerSettings);


                            foreach (KeyValuePair<Guid, GameObject> kvp in objects)
                            {
                                if(LoadedObjects.GetType() != typeof(Player))
                                LoadedObjects.TryAdd(kvp.Key, kvp.Value);
                            }

                            chunkSuccessfullyCreated = true;
                        }
                        catch(Exception)
                        {
                            //Create new chunk for now, if loading fails
                            //TODO: Report this to the console
                        }
                    }

                    if (!chunkSuccessfullyCreated)
                    {
                        Tile[,] t = new Tile[Chunk.SIZE, Chunk.SIZE];

                        string fullRngString = xCoord + dimensionRngString + yCoord;
                        CubicNoise noiseGenerator = new CubicNoise(dimensionRngString.ToSeed(), 1);



                        for (int y = 0; y < Chunk.SIZE; y++)
                        {
                            for (int x = 0; x < Chunk.SIZE; x++)
                            {
                                float f = noiseGenerator.Sample(((double)xCoord * Chunk.SIZE + x) / Chunk.SIZE, ((double)yCoord * Chunk.SIZE + y) / Chunk.SIZE);

                                if(f < 0.125F)
                                {
                                    t[x, y].TypeId = 1;
                                    t[x, y].VarientId = 0;
                                }
                                else
                                {
                                    t[x, y].TypeId = 2;
                                    t[x, y].VarientId = 0;
                                }
                            }
                        }

                        Chunk c = new Chunk() { Tiles = t };
                        LoadedChunks.TryAdd((xCoord, yCoord), c);

                        Dictionary<Guid, GameObject> d = new Dictionary<Guid, GameObject>();
                        d.Add(Guid.NewGuid(), new Merchant() { Name = "Merchant", Coordinates = IndecesToPosition((xCoord, yCoord), ((byte)1, (byte)1)), Health = Merchant.GetMaxHealth(0)});

                        LoadedObjects.TryAdd(d.Keys.First(), d.Values.First());

                        //Save new chunk
                        try
                        {
                            string chunksFolder = Path.Combine(dimensionPath, "chunks");
                            if (!Directory.Exists(chunksFolder)) Directory.CreateDirectory(chunksFolder);

                            using (FileStream fs = new FileStream(tilesPath, FileMode.OpenOrCreate))
                            {
                                MessagePackSerializer.Serialize(fs, c, PlayerdomGame.SerializerSettings);
                                fs.Flush();
                            }

                            using (FileStream fs = new FileStream(objectsPath, FileMode.OpenOrCreate))
                            {
                                MessagePackSerializer.Serialize(fs, d, PlayerdomGame.SerializerSettings);
                                fs.Flush();
                            }
                        }
                        catch(Exception)
                        {
                            //TODO: Report this to the console
                        }
                    }
                }

                return LoadedChunks[(xCoord, yCoord)];
            }
        }
        public void UnloadChunk(long xCoord, long yCoord, string dimensionPath)
        {
            lock (this.LoadedChunks)
            {
                //TODO: Generate chunks
                if (LoadedChunks.ContainsKey((xCoord, yCoord)))
                {
                    LoadedChunks.TryRemove((xCoord, yCoord), out Chunk value);

                    //Save final chunk
                    try
                    {
                        string chunksFolder = Path.Combine(dimensionPath, "chunks");
                        if (!Directory.Exists(chunksFolder)) Directory.CreateDirectory(chunksFolder);

                        string chunkPath = Path.Combine(dimensionPath, "chunks", $"{xCoord}_{yCoord}_tiles.bin");
                        using (FileStream fs = new FileStream(chunkPath, FileMode.OpenOrCreate))
                        {
                            MessagePackSerializer.Serialize(fs, value, PlayerdomGame.SerializerSettings);
                            fs.Flush();
                        }


                        Dictionary<Guid, GameObject> objectsToRemove = new Dictionary<Guid, GameObject>();

                        foreach (KeyValuePair<Guid, GameObject> o in LoadedObjects)
                        {
                            (long, long) chunkIndex1 = PositionToChunkIndex(o.Value.Coordinates);
                            if (xCoord == chunkIndex1.Item1 && yCoord == chunkIndex1.Item2)
                            {

                                if (o.Key.GetType() == typeof(Player))
                                    LoadedObjects.TryRemove(o.Key, out GameObject o1); //Don't save players as objects
                                else objectsToRemove.Add(o.Key, o.Value);
                            }
                        }
                        foreach(KeyValuePair<Guid, GameObject> g in objectsToRemove)
                        {
                            LoadedObjects.TryRemove(g.Key, out GameObject v);
                        }

                        chunkPath = Path.Combine(dimensionPath, "chunks", $"{xCoord}_{yCoord}_objects.bin");
                        using (FileStream fs = new FileStream(chunkPath, FileMode.OpenOrCreate))
                        {
                            MessagePackSerializer.Serialize(fs, objectsToRemove, PlayerdomGame.SerializerSettings);
                            fs.Flush();
                        }

                    }
                    catch (Exception)
                    {
                        //TODO: Report this to the console
                    }
                }
            }
        }

        public static (long, long) PositionToChunkIndex((double, double) position)
        {
            sbyte xSupplement = (sbyte)(position.Item1 < 0.0 ? -1 : 0);
            sbyte ySupplement = (sbyte)(position.Item2 < 0.0 ? -1 : 0);
            return ((long)(position.Item1 / (long)Chunk.SIZE) + xSupplement, (long)(position.Item2 / (long)Chunk.SIZE) + ySupplement);
        }
        public static (byte, byte) PositionToTileIndex((double, double) position)
        {
            return
            (
                (byte)(position.Item1 >= 0 ? (Math.Abs(position.Item1 % (double)Chunk.SIZE)) : Chunk.SIZE - 1 - (byte)(Math.Abs(position.Item1 % (double)Chunk.SIZE))),
                (byte)(position.Item2 >= 0 ? (Math.Abs(position.Item2 % (double)Chunk.SIZE)) : Chunk.SIZE - 1 - (byte)(Math.Abs(position.Item2 % (double)Chunk.SIZE)))
            );
        }

        public static (double, double) IndecesToPosition((long, long) chunkIndex, (byte, byte) tileIndex)
        {
            return ((double)chunkIndex.Item1 * (double)Chunk.SIZE + (double)tileIndex.Item1 + 0.5, (double)chunkIndex.Item2 * (double)Chunk.SIZE + (double)tileIndex.Item2 + 0.5);
        }
    }
}
