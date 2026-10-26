using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class VectorHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (Player.HeldItem.ModItem is not ExusiaiVector gun) return false;
        bool reload = gun.Magazine.Reloading;
        request = new HeadBarRequest(gun.Magazine.Progress,
            new HeadBarStyle(new Color(28, 13, 10, 224), reload ? new Color(211, 64, 36) : new Color(238, 155, 44),
                reload ? new Color(255, 178, 94) : new Color(255, 239, 176)) { FillSmoothing = 1 });
        return true;
    }
}
