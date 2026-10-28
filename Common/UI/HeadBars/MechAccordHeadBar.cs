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
            if (projectile.owner != Player.whoAmI) continue;
            // The unit's actual lock is authoritative, not the staff's attack-cycle timer or cursor.
            bool locked = Player.HeldItem.ModItem switch
            {
                ClickStaff => projectile.ModProjectile is ClickDrone { HasLockedTarget: true },
                RockrockStaff => projectile.ModProjectile is MechAccordDrone { HasLockedTarget: true },
                GoldenglowWand => projectile.ModProjectile is GoldenglowBeacon { HasLockedTarget: true },
                _ => false
            };
            if (locked) { progress = 1; break; }
        }
        request = new HeadBarRequest(progress,
            new HeadBarStyle(new Color(5, 14, 28, 224), color, Color.Lerp(color, Color.White, .72f))
            {
                // Binary lock state: full or empty, without a misleading interpolation delay.
                FillSmoothing = 1f
            });
        return true;
    }
}
