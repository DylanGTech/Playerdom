using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared
{
    [MessagePackObject]
    public class Dimension
    {
        [Key(0)]
        public string DefaultSeedString { get; set; }
        
        [Key(1)]
        public Map Map { get; set; }

        [Key(2)]
        public (float hue, float sat, float val) Discolorization { get; set; } = (0f, 0f, 0f);

        //Add other values needed to generate the dimensions as-needed
        public Dimension(string defaultSeedString, ushort id)
        {
            DefaultSeedString = defaultSeedString;

            string dimensionSeed = DefaultSeedString + id.ToString();

            Discolorization = ((float)MGUtils.GetNormalRandomDouble(dimensionSeed + "h", 0, 0.10, 1, -1),
                (float)MGUtils.GetNormalRandomDouble(dimensionSeed + "s", 0, 0.15, 1, -1),
                (float)MGUtils.GetNormalRandomDouble(dimensionSeed + "v", 0, 0.05, 1, -1));

            Map = new Map();
        }
    }
}
