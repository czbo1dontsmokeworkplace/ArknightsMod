using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Guard.Crusher;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class CrusherHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (Player.HeldItem.ModItem is not CrusherWeapon weapon) return false;
        var state = Player.GetModPlayer<CrusherPlayer>();
        var blade = CrusherHeldBlade.Find(Player);
        bool skillCharge = state.Charging;
        if (!skillCharge && blade is not { IsCharging: true }) return false;
        request = new HeadBarRequest(skillCharge ? state.Charge : blade.ChargeProgress,
            new HeadBarStyle(new Color(20, 23, 31, 224), CrusherRules.Tint(weapon.Kind), CrusherRules.Edge(weapon.Kind))
                { FillSmoothing = 1, FadeInTicks = 4, FadeOutTicks = 8 });
        return true;
    }
}
