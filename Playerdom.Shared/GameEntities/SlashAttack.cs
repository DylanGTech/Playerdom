using MessagePack;
using Playerdom.Shared.GameObjects;
using System;
using System.Collections.Generic;

namespace Playerdom.Shared.GameEntities;

[MessagePackObject]
public class SlashAttack : GameEntity
{
    [IgnoreMember]
    public List<Guid> ObjectsAttacked
    {
        get; set;
    } = new List<Guid>();

    [Key(4)]
    public short AttackPower
    {
        get; set;
    } = 0;

    [Key(5)]
    public uint TicksToLive
    {
        get; set;
    } = 0;

    public override object Clone()
    {
        SlashAttack a = new SlashAttack();
        a.UpdateStats(this);
        return a;
    }

    public override void Update(Map map, Guid EntityId)
    {
        if (TicksToLive == 0)
        {
            map.LoadedEntities.TryRemove(EntityId, out GameEntity value);
            return;
        }
        TicksToLive--;

        switch (DirectionFacing)
        {
            case BasicDirections.North:
                MoveWithTileCollision(map, (0, -0.05), true, false);
                break;
            case BasicDirections.South:
                MoveWithTileCollision(map, (0, 0.05), true, false);
                break;
            case BasicDirections.West:
                MoveWithTileCollision(map, (-0.05, 0), true, false);
                break;
            case BasicDirections.East:
                MoveWithTileCollision(map, (0.05, 0), true, false);
                break;
            case BasicDirections.NorthWest:
                MoveWithTileCollision(map, (-0.05, -0.05), true, false);
                break;
            case BasicDirections.NorthEast:
                MoveWithTileCollision(map, (0.05, -0.05), true, false);
                break;
            case BasicDirections.SouthWest:
                MoveWithTileCollision(map, (-0.05, 0.05), true, false);
                break;
            case BasicDirections.SouthEast:
                MoveWithTileCollision(map, (0.05, 0.05), true, false);
                break;
        }
    }
    public override void HandleCollision(Guid id, GameObject g, Map map)
    {
        if (id != CreatorObjectId && !ObjectsAttacked.Contains(id))
        {
            ObjectsAttacked.Add(id);
            if (g.DealDamage(AttackPower))
            {
                if (g is Player) g.Coordinates = (0, 0);
                else map.LoadedObjects.Remove(id, out GameObject value);
            }
        }
        base.HandleCollision(id, g, map);
    }

    public override void UpdateStats(GameEntity o)
    {
        base.UpdateStats(o);
        if (o is SlashAttack a) ObjectsAttacked = new List<Guid>(a.ObjectsAttacked);
    }
}