using MessagePack;
using System;

namespace Playerdom.Shared.GameObjects;

[MessagePack.Union(0, typeof(Player))]
[MessagePack.Union(1, typeof(Merchant))]
public abstract class GameObject : ICloneable
{
    [Key(0)]
    public string Name
    {
        get; set;
    } = "";

    [Key(1)]
    public uint Level
    {
        get; set;
    } = 0;


    [Key(2)]
    public (double, double) Size
    {
        get; set;
    } = (0.8, 0.8);


    [Key(3)]
    public (double, double) Coordinates
    {
        get; set;
    } = (0, 0);

    [Key(4)]
    public BasicDirections DirectionFacing
    {
        get; set;
    } = BasicDirections.South;

    [Key(5)]
    public ulong Health
    {
        get; set;
    }

    public virtual void UpdateStats(GameObject o)
    {
        Name = o.Name;
        Level = o.Level;
        Size = o.Size;
        Coordinates = o.Coordinates;
        DirectionFacing = o.DirectionFacing;
        Health = o.Health;
    }

    public virtual void Update(Map map, Guid objectId) {}

    public void MoveWithTileCollision(Map map, (double, double) velocity, bool canFly, bool canSwim)
    {
        (double, double) firstBound = (this.Coordinates.Item1 - this.Size.Item1 / 2, this.Coordinates.Item2 - this.Size.Item2 / 2);
        (double, double) secondBound = (this.Coordinates.Item1 + this.Size.Item1 / 2, this.Coordinates.Item2 + this.Size.Item2 / 2);

        (long, long) firstChunkIndex = Map.PositionToChunkIndex((firstBound.Item1 - 1, firstBound.Item2 - 1));
        (long, long) secondChunkIndex = Map.PositionToChunkIndex((secondBound.Item1 + 2, secondBound.Item2 + 2));

        (byte, byte) firstTileIndex = Map.PositionToTileIndex((firstBound.Item1 - 1, firstBound.Item2 - 1));
        (byte, byte) secondTileIndex = Map.PositionToTileIndex((secondBound.Item1 + 2, secondBound.Item2 + 2));

        byte maxTilesX = (byte)Math.Ceiling(secondBound.Item1 - firstBound.Item1 + 3);
        byte maxTilesY = (byte)Math.Ceiling(secondBound.Item2 - firstBound.Item2 + 3);


        if (map.LoadedChunks.ContainsKey((firstChunkIndex.Item1, firstChunkIndex.Item2)) && map.LoadedChunks.ContainsKey((secondChunkIndex.Item1, secondChunkIndex.Item2)))
        {
            for (int x = 0; x < maxTilesX; x++)
            {
                for (int y = 0; y < maxTilesY; y++)
                {
                    (double, double) tilePos = Map.IndecesToPosition(firstChunkIndex, firstTileIndex);
                    tilePos = (tilePos.Item1 + x, tilePos.Item2 + y);

                    (long, long) chunkIndex = Map.PositionToChunkIndex(tilePos);
                    (byte, byte) tileIndex = Map.PositionToTileIndex(tilePos);

                    Tile t = map.LoadedChunks[(chunkIndex.Item1, chunkIndex.Item2)].Tiles[tileIndex.Item1, tileIndex.Item2];


                    if ((t.IsLiquid && !canSwim) || (!canFly && (t.IsLiquid || t.IsGroundSolid)))
                    {
                        (double, double) newCoords = (Coordinates.Item1 + velocity.Item1, Coordinates.Item2 + velocity.Item2);

                        (decimal, decimal) depth = GetIntersectionDepth(tilePos, (1.00, 1.00), newCoords);

                        if (depth == (0.00M, 0.00M)) continue;

                        if (depth.Item1 != 0.00M && velocity.Item1 != 0.00 && Math.Abs(Coordinates.Item1 - tilePos.Item1) > Math.Abs(Coordinates.Item2 - tilePos.Item2))
                            velocity = (velocity.Item1 + (double)depth.Item1, velocity.Item2);
                        if (depth.Item2 != 0.00M && velocity.Item2 != 0.00 && Math.Abs(Coordinates.Item1 - tilePos.Item1) < Math.Abs(Coordinates.Item2 - tilePos.Item2))
                            velocity = (velocity.Item1, velocity.Item2 + (double)depth.Item2);

                    }
                }
            }



            foreach ((Guid id, GameObject otherObject) in map.LoadedObjects)
            {
                if (this == otherObject || !this.CheckCollision(otherObject)) continue;
                (decimal x, decimal y) depth = GetIntersectionDepth(otherObject.Coordinates, (otherObject.Size.Item1, otherObject.Size.Item2), (this.Coordinates.Item1 + velocity.Item1, this.Coordinates.Item2 + velocity.Item2));

                if (depth == (0.00M, 0.00M)) continue;

                if (depth.Item1 != 0.00M && velocity.Item1 != 0.00 && Math.Abs(Coordinates.Item1 - otherObject.Coordinates.Item1) > Math.Abs(Coordinates.Item2 - otherObject.Coordinates.Item2))
                    velocity = (velocity.Item1 + (double)depth.Item1, velocity.Item2);
                if (depth.Item2 != 0.00M && velocity.Item2 != 0.00 && Math.Abs(Coordinates.Item1 - otherObject.Coordinates.Item1) < Math.Abs(Coordinates.Item2 - otherObject.Coordinates.Item2))
                    velocity = (velocity.Item1, velocity.Item2 + (double)depth.Item2);

                this.HandleCollision(id, otherObject, map);
            }

            this.Coordinates = (Coordinates.Item1 + velocity.Item1, Coordinates.Item2 + velocity.Item2);

        }

    }


    public abstract object Clone();

    public bool CheckCollision(GameObject other)
    {
        return Math.Abs(other.Coordinates.Item1 - this.Coordinates.Item1) <= other.Size.Item1 / 2 + this.Size.Item1 / 2
            || Math.Abs(other.Coordinates.Item2 - this.Coordinates.Item2) <= other.Size.Item2 / 2 + this.Size.Item2 / 2;
    }

    public virtual void HandleCollision(Guid id, GameObject g2, Map map) {}

    public static ulong GetMaxHealth(uint level)
    {
        return (level + 1) * 5;
    }

    public bool DealDamage(int damage)
    {
        if (damage < 0)
        {
            if (GetMaxHealth(Level) < Health + (ulong)-damage)
                Health = GetMaxHealth(Level);
            else Health += (ulong)-damage;
            return false;
        }
        if (damage > 0 && (ulong)damage >= Health)
        {
            Health = 0;
            return true;
        }

        Health -= (ulong)damage;
        return false;
    }

    public (decimal, decimal) GetIntersectionDepth((double, double) otherPosition, (double, double) otherSize, (double, double)? currentPosition = null, (double, double)? currentSize = null)
    {
        if (!currentPosition.HasValue) currentPosition = this.Coordinates;
        if (!currentSize.HasValue) currentSize = ((double)this.Size.Item1, (double)this.Size.Item2);

        // Calculate current and minimum-non-intersecting distances between centers.
        decimal distanceX = (decimal)currentPosition.Value.Item1 - (decimal)otherPosition.Item1;
        decimal distanceY = (decimal)currentPosition.Value.Item2 - (decimal)otherPosition.Item2;
        decimal minDistanceX = (decimal)currentSize.Value.Item1 / 2 + (decimal)otherSize.Item1 / 2;
        decimal minDistanceY = (decimal)currentSize.Value.Item2 / 2 + (decimal)otherSize.Item2 / 2;

        // If we are not intersecting at all, return (0, 0).
        if (Math.Abs(distanceX) >= minDistanceX || Math.Abs(distanceY) >= minDistanceY)
            return (0.00M, 0.00M);

        // Calculate and return intersection depths.
        decimal depthX = distanceX > 0 ? minDistanceX - distanceX : -minDistanceX - distanceX;
        decimal depthY = distanceY > 0 ? minDistanceY - distanceY : -minDistanceY - distanceY;
        return (depthX, depthY);

        //return (minDistanceX - distanceX, minDistanceY - distanceY);
    }
}