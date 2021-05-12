using MessagePack;
using Microsoft.Xna.Framework;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Playerdom.Shared.GameEntities
{
    [MessagePack.Union(0, typeof(SlashAttack))]
    public abstract class GameEntity : ICloneable
    {

        [Key(0)]
        public (double, double) Size
        {
            get; set;
        } = (1.0, 1.0);


        [Key(1)]
        public (double, double) Coordinates
        {
            get; set;
        } = (0, 0);

        [Key(2)]
        public BasicDirections DirectionFacing
        {
            get; set;
        } = BasicDirections.South;

        [Key(3)]
        public Guid? CreatorObjectId
        {
            get; set;
        } = null;

        public virtual void UpdateStats(GameEntity o)
        {
            Size = o.Size;
            Coordinates = o.Coordinates;
        }

        public virtual void Update(Map map, Guid EntityId)
        {
            
        }

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

                        if ((t.IsLiquid && !canSwim) && (!canFly && (t.IsLiquid || t.IsGroundSolid)))
                        {
                            (double, double) newCoords = (Coordinates.Item1 + velocity.Item1, Coordinates.Item2 + velocity.Item2);

                            (double, double) depth = GetIntersectionDepth(tilePos, (1.00, 1.00), newCoords);

                            if (depth == (0.00, 0.00)) continue;

                            if (depth.Item1 != 0.00 && velocity.Item1 != 0.00 && Math.Abs(Coordinates.Item1 - tilePos.Item1) > Math.Abs(Coordinates.Item2 - tilePos.Item2))
                                velocity = (velocity.Item1 + depth.Item1, velocity.Item2);
                            if (depth.Item2 != 0.00 && velocity.Item2 != 0.00 && Math.Abs(Coordinates.Item1 - tilePos.Item1) < Math.Abs(Coordinates.Item2 - tilePos.Item2))
                                velocity = (velocity.Item1, velocity.Item2 + depth.Item2);

                        }
                    }
                }



                foreach ((Guid id, GameObject obj) in map.LoadedObjects)
                {
                    if (!this.CheckCollision(obj)) continue;
                    (double x, double y) depth = GetIntersectionDepth(obj.Coordinates, (obj.Size.Item1, obj.Size.Item2), (this.Coordinates.Item1 + velocity.Item1, this.Coordinates.Item2 + velocity.Item2));

                    if (depth == (0.00, 0.00)) continue;


                    //GameEntities are not inhibited in movement by objects
                    /*
                    if (depth.Item1 != 0.00 && velocity.Item1 != 0.00 && Math.Abs(Coordinates.Item1 - obj.Coordinates.Item1) > Math.Abs(Coordinates.Item2 - obj.Coordinates.Item2))
                        velocity = (velocity.Item1 + depth.Item1, velocity.Item2);
                    if (depth.Item2 != 0.00 && velocity.Item2 != 0.00 && Math.Abs(Coordinates.Item1 - obj.Coordinates.Item1) < Math.Abs(Coordinates.Item2 - obj.Coordinates.Item2))
                        velocity = (velocity.Item1, velocity.Item2 + depth.Item2);
                    */

                    this.HandleCollision(id, obj, map);
                }

               
                this.Coordinates = (Coordinates.Item1 + velocity.Item1, Coordinates.Item2 + velocity.Item2);

            }

        }


        public abstract object Clone();

        public bool CheckCollision(GameObject obj)
        {
            return Math.Abs(obj.Coordinates.Item1 - this.Coordinates.Item1) <= obj.Size.Item1 / 2 + this.Size.Item1 / 2
                || Math.Abs(obj.Coordinates.Item2 - this.Coordinates.Item2) <= obj.Size.Item2 / 2 + this.Size.Item2 / 2;
        }

        public virtual void HandleCollision(Guid id, GameObject g, Map map)
        {

        }

        public (double, double) GetIntersectionDepth((double, double) otherPosition, (double, double) otherSize, (double, double)? currentPosition = null, (double, double)? currentSize = null)
        {
            if (!currentPosition.HasValue) currentPosition = this.Coordinates;
            if (!currentSize.HasValue) currentSize = ((double)this.Size.Item1, (double)this.Size.Item2);

            // Calculate current and minimum-non-intersecting distances between centers.
            double distanceX = currentPosition.Value.Item1 - otherPosition.Item1;
            double distanceY = currentPosition.Value.Item2 - otherPosition.Item2;
            double minDistanceX = (double)currentSize.Value.Item1 / 2 + (double)otherSize.Item1 / 2;
            double minDistanceY = (double)currentSize.Value.Item2 / 2 + (double)otherSize.Item2 / 2;

            // If we are not intersecting at all, return (0, 0).
            if (Math.Abs(distanceX) >= minDistanceX || Math.Abs(distanceY) >= minDistanceY)
                return (0.00, 0.00);

            // Calculate and return intersection depths.
            double depthX = distanceX > 0 ? minDistanceX - distanceX : -minDistanceX - distanceX;
            double depthY = distanceY > 0 ? minDistanceY - distanceY : -minDistanceY - distanceY;
            return (depthX, depthY);

            //return (minDistanceX - distanceX, minDistanceY - distanceY);
        }
    }
}
