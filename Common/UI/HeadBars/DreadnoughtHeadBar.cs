using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class DreadnoughtHeadBar : WeaponHeadBar
{
    private ChargedOperatorKind lastKind;

    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        var state = Player.GetModPlayer<DreadnoughtPlayer>();
        bool holding = Player.HeldItem.ModItem is ChargedOperatorWeapon { Melee: true };
        if (holding) lastKind = ((ChargedOperatorWeapon)Player.HeldItem.ModItem).Kind;
        // Dreadnought keeps showing its remaining momentum after switching away.
        // Other head bars retain the shared base's normal hide/fade behavior.
        if (!holding && state.Progress <= 0) return false;
        Color color = ChargedOperatorRules.ColorFor(lastKind);
        request = new HeadBarRequest(state.Progress,
            new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f)));
        return true;
    }
}
