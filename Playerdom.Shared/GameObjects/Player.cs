using MessagePack;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.GameEntities;
using System;
using System.Linq;

namespace Playerdom.Shared.GameObjects;

[MessagePackObject]
public class Player : GameObject
{
    [Key(6)]
    public Keys[] Keyboard
    {
        get;
        set;
    }

    [Key(7)]
    public DateTime LastAttack
    {
        get;
        set;
    }

    public override object Clone()
    {
        Player p = new Player();
        p.UpdateStats(this);
        return p;
    }

    public override void Update(Map map, Guid objectId)
    {

        if (Keyboard != null)
        {
            (sbyte, sbyte) direction = (0, 0);

            if (Keyboard.Contains(Keys.W))
                direction = (direction.Item1, (sbyte)(direction.Item2 - 1));
            if (Keyboard.Contains(Keys.S))
                direction = (direction.Item1, (sbyte)(direction.Item2 + 1));
            if (Keyboard.Contains(Keys.A))
                direction = ((sbyte)(direction.Item1 - 1), direction.Item2);
            if (Keyboard.Contains(Keys.D))
                direction = ((sbyte)(direction.Item1 + 1), direction.Item2);

            if (direction.Item1 != 0 && direction.Item2 != 0)
            {
                double angle = Math.Atan2(direction.Item2, direction.Item1);
                MoveWithTileCollision(map, (0.05 * Math.Cos(angle), 0.05 * Math.Sin(angle)), false, false);
            }
            else if (direction.Item1 != 0 || direction.Item2 != 0)
                MoveWithTileCollision(map, (0.05 * direction.Item1, 0.05 * direction.Item2), false, false);

            if (direction.Item1 != 0 || direction.Item2 != 0)
                switch (direction)
                {
                    default:
                    case (0, 0):
                        break;
                    case (-1, 0):
                        DirectionFacing = BasicDirections.West;
                        break;
                    case (1, 0):
                        DirectionFacing = BasicDirections.East;
                        break;
                    case (0, -1):
                        DirectionFacing = BasicDirections.North;
                        break;
                    case (0, 1):
                        DirectionFacing = BasicDirections.South;
                        break;
                    case (-1, -1):
                        DirectionFacing = BasicDirections.NorthWest;
                        break;
                    case (1, -1):
                        DirectionFacing = BasicDirections.NorthEast;
                        break;
                    case (1, 1):
                        DirectionFacing = BasicDirections.SouthEast;
                        break;
                    case (-1, 1):
                        DirectionFacing = BasicDirections.SouthWest;
                        break;
                }

            if (Keyboard.Contains(Keys.Z) && (DateTime.Now - LastAttack).Seconds > 2)
            {
                LastAttack = DateTime.Now;

                (double X, double Y) coords = this.Coordinates;
                (double X, double Y) size = (1.2, 1.2);

                if (DirectionFacing == BasicDirections.North
                    || DirectionFacing == BasicDirections.NorthEast
                    || DirectionFacing == BasicDirections.NorthWest)
                    coords.Y -= 1.5;
                else if (DirectionFacing == BasicDirections.South
                    || DirectionFacing == BasicDirections.SouthEast
                    || DirectionFacing == BasicDirections.SouthWest)
                    coords.Y += 1.5;

                if (DirectionFacing == BasicDirections.West
                    || DirectionFacing == BasicDirections.NorthWest
                    || DirectionFacing == BasicDirections.SouthWest)
                    coords.X -= 1.5;
                else if (DirectionFacing == BasicDirections.East
                    || DirectionFacing == BasicDirections.NorthEast
                    || DirectionFacing == BasicDirections.SouthEast)
                    coords.X += 1.5;


                map.LoadedEntities.TryAdd(Guid.NewGuid(), new SlashAttack()
                {
                    AttackPower = 1,
                    Coordinates = coords,
                    CreatorObjectId = objectId,
                    DirectionFacing = this.DirectionFacing,
                    Size = size,
                    TicksToLive = 35
                });
            }
        }

        base.Update(map, objectId);
    }

    public override void UpdateStats(GameObject o)
    {
        base.UpdateStats(o);
        if (o is Player p)
        {
            Keyboard = p.Keyboard;
            LastAttack = p.LastAttack;
        }
    }
}