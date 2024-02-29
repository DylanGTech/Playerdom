using MessagePack;
using Playerdom.Shared.GameEntities;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Playerdom.Shared;

[MessagePackObject]
public class ClientPack
{
    [Key(0)]
    public Chunk[,] Chunks { get; set; }

    [Key(1)]
    public ConcurrentDictionary<Guid, GameObject> GameObjects { get; set; }

    [Key(2)]
    public ConcurrentDictionary<Guid, GameEntity> GameEntities { get; set; }

    [Key(3)]
    public ServerMessage CurrentMessage { get; set; }

    [Key(4)]
    public Queue<ChatMessage> NewChats { get; set; }
}