using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class DreadnoughtHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        var state = Player.GetModPlayer<DreadnoughtPlayer>();
        if (Player.HeldItem.ModItem is not ChargedOperatorWeapon { Melee: true } weapon) return false;
        Color color = ChargedOperatorRules.ColorFor(weapon.Kind);
        request = new HeadBarRequest(state.ProgressFor(weapon.Kind),
            new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f)) { FillSmoothing = 1 });
        return true;
    }
}
