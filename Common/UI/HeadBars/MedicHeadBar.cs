using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class MedicHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        int remaining = Player.GetModPlayer<MedicCastPlayer>().Cooldown;
        if (remaining == 0 && Player.HeldItem.ModItem is not MedicWeapon) return false;
        request = new HeadBarRequest(1 - remaining / (float)MedicCastPlayer.CooldownTicks,
            new HeadBarStyle(new Color(24, 24, 24, 224), new Color(120, 120, 120), new Color(210, 210, 210))
                { FillSmoothing = 1, FadeInTicks = 3, FadeOutTicks = 8 });
        return true;
    }
}
