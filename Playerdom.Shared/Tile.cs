using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared
{
    [MessagePackObject]
    public struct Tile
    {
        public const uint SIZE = 64;

        [Key(0)]
        public ushort TypeId { get; set; }
        
        [Key(1)]
        public byte VarientId { get; set; }

        [IgnoreMember]
        public bool IsLiquid
        {
            get
            {
                if (TypeId == 2) return true;
                return false;
            }
        }

        [IgnoreMember]
        public bool IsGroundSolid
        {
            get
            {
                if (TypeId == 3) return true;
                return false;
            }
        }



        private static Dictionary<ushort, Texture2D[]> defaultTextures = new Dictionary<ushort, Texture2D[]>();
        private static Dictionary<ushort, Texture2D[]> textures = new Dictionary<ushort, Texture2D[]>();
        private static (float hue, float sat, float val) currentHsv = (0f, 0f, 0f);

        [IgnoreMember]
        public Texture2D Texture
        {
            get
            {
                if(textures.TryGetValue(TypeId, out Texture2D[] values))
                {
                    if (VarientId < values.Length)
                        return values[VarientId];
                    return values[0];
                }
                return null;
            }
        }

        public static void ChangeTextureHSV((float hue, float sat, float val) hsv)
        {
            if (currentHsv.Equals(hsv)) return;
            currentHsv = hsv;
                lock (textures)
                {
                    foreach (ushort key in textures.Keys)
                    {
                        Texture2D[] textureArray = textures[key];
                        Texture2D[] defaultTextureArray = defaultTextures[key];

                        for (int i = 0; i < textureArray.Length; i++)
                        {
                            Color[] pixels = new Color[defaultTextureArray[i].Width * defaultTextureArray[i].Height];

                            defaultTextureArray[i].GetData(pixels);

                            for (int p = 0; p < pixels.Length; p++)
                            {
                                pixels[p] = pixels[p].ToHSV().OffsetHSV(hsv).ToColor();
                            }
                            textureArray[i].SetData(pixels);
                        }
                    }
                }
        }


        public static void LoadTextures(ContentManager content, GraphicsDevice device)
        {
            defaultTextures.Add(1, new Texture2D[] { content.Load<Texture2D>("ground") });
            defaultTextures.Add(2, new Texture2D[] { content.Load<Texture2D>("water") });
            defaultTextures.Add(3, new Texture2D[] { content.Load<Texture2D>("stone") });
            defaultTextures.Add(4, new Texture2D[] { content.Load<Texture2D>("woodplanks") });
            defaultTextures.Add(5, new Texture2D[] { content.Load<Texture2D>("portal") });
            defaultTextures.Add(6, new Texture2D[] { content.Load<Texture2D>("sand") });


            textures.Add(1, new Texture2D[] { new Texture2D(device, defaultTextures[1][0].Width, defaultTextures[1][0].Height) });
            textures.Add(2, new Texture2D[] { new Texture2D(device, defaultTextures[2][0].Width, defaultTextures[2][0].Height) });
            textures.Add(3, new Texture2D[] { new Texture2D(device, defaultTextures[3][0].Width, defaultTextures[3][0].Height) });
            textures.Add(4, new Texture2D[] { new Texture2D(device, defaultTextures[4][0].Width, defaultTextures[4][0].Height) });
            textures.Add(5, new Texture2D[] { new Texture2D(device, defaultTextures[5][0].Width, defaultTextures[5][0].Height) });
            textures.Add(6, new Texture2D[] { new Texture2D(device, defaultTextures[6][0].Width, defaultTextures[6][0].Height) });

        }

        /*
        
        TypeId    | VarientId | Name
        ============================================
        0         | ~         | Void
        1         | ~         | Grass
        2         | ~         | Water
        3         | ~         | Stone
        4         | ~         | Wood Planks
        5         | ~         | Portal
        6         | ~         | Sand
        */
    }
}
