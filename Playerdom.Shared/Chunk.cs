using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared
{
    [MessagePackObject]
    public struct Chunk
    {
        public const uint SIZE = 32;

        [Key(0)]
        public Tile[,] Tiles
        {
            get; set;
        }
    }
}
