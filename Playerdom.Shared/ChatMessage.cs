using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared
{
    [MessagePackObject]
    public struct ChatMessage
    {
        [Key(0)]
        public string Content;

        [Key(1)]
        public string Sender;

        [Key(2)]
        public DateTime TimeSent { get; set; }

        [Key(3)]
        public ushort DimensionSent { get; set; }

        [Key(4)]
        public (double X, double Y) PlaceSent { get; set; }

        [Key(5)]
        public Guid SenderObjectId { get; set; }

        [Key(6)]
        public ChatMessageTypes MessageType { get; set; }

        [Key(7)]
        public ChatMessageScopes MessageScope { get; set; }
    }

    public enum ChatMessageTypes : byte
    {
        Server,
        Owner,
        Admin,
        Mod,
        Player,
        NPC,
    }

    public enum ChatMessageScopes : byte
    {
        Global,
        Dimension,
        Party,
        Area
    }
}
