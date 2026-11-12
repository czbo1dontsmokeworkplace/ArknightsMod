using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using ArknightsMod.Content.Projectiles.Sniper.Typhon;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class TyphonHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (!global::ArknightsMod.Content.Items.Weapons.Sniper.Archers.ArcherCharge.Supports(Player.HeldItem)) return false;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner != Player.whoAmI || projectile.ModProjectile is not TyphonMagazine magazine
                || !magazine.BelongsTo(Player.HeldItem)) continue;
            request = new HeadBarRequest(magazine.TotalLoadProgress,
                new HeadBarStyle(new Color(16, 13, 30, 224), new Color(237, 172, 73), new Color(255, 236, 180))
                    { FillSmoothing = 1f, FadeInTicks = 4, FadeOutTicks = 8 });
            return true;
        }
        return false;
    }
}
