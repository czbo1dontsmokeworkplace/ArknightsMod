using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class VanguardChargeHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (Player.HeldItem.ModItem is not VanguardSpearWeapon weapon) return false;
        Color tint = VanguardSpearRules.Tint(weapon.Kind);
        request = new HeadBarRequest(Player.GetModPlayer<VanguardSpearPlayer>().Cooldown.Ratio,
            new HeadBarStyle(new Color(20, 23, 31, 224), tint, Color.Lerp(tint, Color.White, .65f))
                { FillSmoothing = 1, FadeInTicks = 4, FadeOutTicks = 8 });
        return true;
    }
}
