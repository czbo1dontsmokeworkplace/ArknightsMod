using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

internal static class MedicLaunch
{
    internal static Vector2 ClampPoint(Vector2 point) => Vector2.Clamp(point,
        new Vector2(32), new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));

    internal static void Cast(Player player, EntitySource_ItemUse_WithAmmo source, int id, int type, float power, bool automatic = false)
    {
        Vector2 target = Main.MouseWorld;
        if (!float.IsFinite(target.X) || !float.IsFinite(target.Y)) target = player.MountedCenter + Vector2.UnitX * player.direction * 120;
        target = ClampPoint(target);
        MedicBranch branch = MedicCatalog.Get(id).Branch;
        if (branch == MedicBranch.MultiTarget) {
            Projectile.NewProjectile(source, target, Vector2.Zero,
                Terraria.ModLoader.ModContent.ProjectileType<MedicHealingField>(), 0, 0, player.whoAmI,
                id, power, automatic ? -MedicCastPlayer.CooldownTicks : MedicCastPlayer.CooldownTicks);
            return;
        }
        int count = branch == MedicBranch.Chain ? 3 : 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 at, velocity;
            if (branch == MedicBranch.Therapist)
            {
                at = ClampPoint(new Vector2(target.X, MathF.Min(target.Y - 640, Main.screenPosition.Y - 96)));
                velocity = Vector2.UnitY * 7.5f; // Absolute world-down, independent of gravity inversion.
            }
            else if (branch == MedicBranch.Chain)
            {
                // Undine-style trio, staggered below the cursor and converging upward.
                at = ClampPoint(target + new Vector2((i - 1) * 60, 520 + i * 90));
                velocity = (target - at).SafeNormalize(-Vector2.UnitY) * 12;
            }
            else
            {
                Vector2 aim = (target - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                at = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(8), aim * 28, 16, 16, true, true);
                velocity = aim * MedicIdentity.Get(id).Speed;
            }
            Projectile.NewProjectile(source, at, velocity, type, 0, 0, player.whoAmI,
                id, power, MedicCastPlayer.CooldownTicks);
            MedicArtsVfx.Cast(at, velocity.SafeNormalize(Vector2.UnitY), id, false);
        }
    }
}
