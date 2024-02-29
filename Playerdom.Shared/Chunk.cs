using MessagePack;

namespace Playerdom.Shared;

[MessagePackObject]
public struct Chunk
{
    public const byte SIZE = 32;

    [Key(0)]
    public Tile[,] Tiles
    {
        get; set;
    }
}