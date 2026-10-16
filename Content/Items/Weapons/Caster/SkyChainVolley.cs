using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Items.Weapons.Caster;

/// <summary>链术士的高空落雷入口；每次 Shoot 交替发射一发、两发。</summary>
internal static class SkyChainVolley
{
    internal static int Fire(Player player, EntitySource_ItemUse_WithAmmo source, int type,
        int damage, float knockback, Vector2 target, float speed,
        int shotIndex, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
    {
        int count = 1 + (shotIndex & 1);
        for (int i = 0; i < count; i++)
        {
            // Alpha Draconis 的高处入场节奏；分散落点让同一轮闪电逐次降下。
            Vector2 origin = target + new Vector2(
                Main.rand.NextFloat(-170f, 170f),
                -Main.rand.NextFloat(540f, 720f));
            Vector2 direction = (target - origin).SafeNormalize(Vector2.UnitY);
            int index = Projectile.NewProjectile(source, origin, direction * speed,
                type, damage, knockback, player.whoAmI, ai0, ai1, ai2);
            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].CritChance = player.GetWeaponCrit(player.HeldItem);
        }
        return count;
    }
}
