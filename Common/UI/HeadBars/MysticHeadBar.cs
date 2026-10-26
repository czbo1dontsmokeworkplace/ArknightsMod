using ArknightsMod.Content.Items.Weapons.Caster.Indigo;
using ArknightsMod.Content.Items.Weapons.Caster.Harmonie;
using ArknightsMod.Content.Items.Weapons.Caster.Ebenholz;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class MysticHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        float progress; Color color;
        switch (Player.HeldItem.ModItem)
        {
            case IndigoStaff:
                progress = Player.GetModPlayer<IndigoStaffPlayer>().ChargeProgress; color = new(184, 157, 247); break;
            case HarmonieStaff:
                progress = Player.GetModPlayer<HarmonieStaffPlayer>().ChargeProgress; color = new(123, 229, 184); break;
            case EbenholzStaff:
                progress = Player.GetModPlayer<EbenholzStaffPlayer>().ChargeProgress; color = new(214, 179, 113); break;
            default: return false;
        }
        request = new HeadBarRequest(progress,
            new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f))
                { FillSmoothing = 1, FadeInTicks = 4, FadeOutTicks = 8, OffsetY = 96 });
        return true;
    }
}
