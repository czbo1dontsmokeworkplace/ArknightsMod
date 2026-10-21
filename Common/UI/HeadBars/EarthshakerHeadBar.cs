using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class EarthshakerHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        var state = Player.GetModPlayer<EarthshakerPlayer>();
        request = new HeadBarRequest(state.ChargeProgress,
            new HeadBarStyle(new Color(26,22,20,220),EarthshakerRules.Tint(state.Kind),EarthshakerRules.Accent(state.Kind))
                {FillSmoothing=1,FadeInTicks=4});
        return state.Charging;

    }
}
