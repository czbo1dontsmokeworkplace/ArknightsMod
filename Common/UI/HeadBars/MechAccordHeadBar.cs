using ArknightsMod.Content.Items.Weapons.Caster.Click;
using ArknightsMod.Content.Items.Weapons.Caster.Goldenglow;
using ArknightsMod.Content.Items.Weapons.Caster.Rockrock;
using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Common.UI.HeadBars;

public sealed class MechAccordHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        Color color;
        switch (Player.HeldItem.ModItem)
        {
            case ClickStaff: color = new Color(65, 211, 241); break;
            case RockrockStaff: color = new Color(255, 145, 55); break;
            case GoldenglowWand: color = new Color(245, 139, 215); break;
            default: return false;
        }

        float progress = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner != Player.whoAmI || (int)projectile.ai[1] != Player.selectedItem)
                continue;
            if (Player.HeldItem.ModItem is GoldenglowWand && projectile.ModProjectile is GoldenglowHeldStaff goldenglow)
                progress = goldenglow.CycleProgress;
            else if (Player.HeldItem.ModItem is ClickStaff or RockrockStaff &&
                projectile.ModProjectile is MechAccordHeldStaff mechAccord)
                progress = mechAccord.CycleProgress;
        }
        request = new HeadBarRequest(progress,
            new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f))
            {
                // Follow the real phase exactly: full on the first empowered tick, then drain.
                FillSmoothing = 1f
            });
        return true;
    }
}
