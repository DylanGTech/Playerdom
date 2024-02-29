using Playerdom.Shared.GameEntities;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Concurrent;

namespace Playerdom.Shared;

public class CurrentClientState
{
    public Chunk[,] Chunks { get; private set; }
    public ConcurrentDictionary<Guid, GameObject> Objects { get; private set; }
    public ConcurrentDictionary<Guid, GameEntity> Entities { get; private set; }

    public CurrentClientState(Chunk[,] chunks, ConcurrentDictionary<Guid, GameObject> objects, ConcurrentDictionary<Guid, GameEntity> entities)
    {
        if (chunks == null) chunks = new Chunk[0, 0];
        if (objects == null) objects = new ConcurrentDictionary<Guid, GameObject>();
        if (entities == null) entities = new ConcurrentDictionary<Guid, GameEntity>();
        Chunks = chunks;
        Objects = objects;
        Entities = entities;
    }
}