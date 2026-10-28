using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Items.Weapons.Caster;

// The native item animation calls Shoot three times; each call launches ONE bolt from the staff.
internal static class ChainCasterVolley
{
    internal static int Fire(Player player, EntitySource_ItemUse_WithAmmo source, int type,
        int damage, float knockback, Vector2 target, float speed,
        float ai0 = 0, float ai1 = 0, float ai2 = 0)
    {
        Vector2 direction = (target - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 origin = player.MountedCenter;
        if (Collision.CanHitLine(origin, 1, 1, origin + direction * 22, 1, 1)) origin += direction * 22;
        int index = Projectile.NewProjectile(source, origin, direction * speed,
            type, damage, knockback, player.whoAmI, ai0, ai1, ai2);
        if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = player.GetWeaponCrit(player.HeldItem);
        return 1;
    }
}
