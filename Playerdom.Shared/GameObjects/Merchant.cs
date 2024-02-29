using MessagePack;

namespace Playerdom.Shared.GameObjects;

[MessagePackObject]
public class Merchant : GameObject
{
    public override object Clone()
    {
        Merchant m = new Merchant();
        m.UpdateStats(this);
        return m;
    }
}
