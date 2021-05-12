using MessagePack;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared
{
    [MessagePackObject]
    public class ServerPack
    {
        [Key(0)]
        public Keys[] KeysPressed { get; set; }

        [Key(1)]
        public ServerMessage CurrentMessage { get; set; }
    }

    [MessagePackObject]
    public class ServerMessage
    {
        [Key(0)]
        public string MessageType { get; set; }
        [Key(1)]
        public string[] MessageContent { get; set; }

    }
}