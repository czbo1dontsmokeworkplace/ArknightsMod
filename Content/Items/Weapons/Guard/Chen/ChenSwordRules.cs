using System;
using ArknightsMod.Content.Projectiles.Guard.BladeRoutes;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Players;

namespace ArknightsMod.Content.Items.Weapons.Guard.Chen;

internal static class ChenSwordRules
{
    internal const int Skill2StartupTicks = 3; // ceil(21 * .1)
    internal const int Skill2FadeTicks = 25; // ceil(35 * .7)
    internal const int Skill3StartupTicks = 25; // ceil(81 * .3)
    internal const int Skill3Duration = 75, Skill3Interval = 6, Skill3Strikes = 10;
    internal const float Skill3Range = 1000, Skill3OwnerRange = 2000;
    internal static void EndSkill(Player owner, int skill)
    {
        if (owner.whoAmI != Main.myPlayer || owner.HeldItem.ModItem is not ChenSword_Item) return;
        var state = owner.GetModPlayer<WeaponPlayer>();
        if (state.Skill == skill && state.CurrentSkill?.Key.Item == nameof(ChenSword_Item)) {
            state.SkillActive = false; state.SkillTimer = 0;
        }
    }

    internal static NPC FindTarget(Player owner, Vector2 origin)
    {
        NPC result = null;
        int bestRank = -1, bestDamage = -1;
        float bestDistance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs) {
            if (!npc.CanBeChasedBy() || npc.friendly || npc.life <= 0) continue;
            float distance = Vector2.DistanceSquared(origin, npc.Center);
            if (distance > Skill3Range * Skill3Range
                || Vector2.DistanceSquared(owner.MountedCenter, npc.Center) > Skill3OwnerRange * Skill3OwnerRange) continue;
            int rank = npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type] ? 1 : 0;
            // Prefer bosses, then higher contact damage, then the nearest peer.
            if (rank < bestRank || rank == bestRank && (npc.damage < bestDamage
                || npc.damage == bestDamage && distance >= bestDistance)) continue;
            result = npc; bestRank = rank; bestDamage = npc.damage; bestDistance = distance;
        }
        return result;
    }

    internal static void Blade(Projectile sword, float progress, ref int count)
    {
        if (sword.owner != Main.myPlayer) return;
        Player owner = Main.player[sword.owner];
        while (count < 3 && progress >= .22f + count * .18f) {
            Vector2 radial = (sword.rotation - MathHelper.PiOver2).ToRotationVector2();
            Projectile.NewProjectile(sword.GetSource_FromThis(), owner.MountedCenter + radial * 95 * owner.GetAdjustedItemScale(owner.HeldItem),
                Vector2.Zero, ModContent.ProjectileType<OperatorScuuxunSpike>(), Math.Max(1, (int)(sword.damage * .35f)),
                sword.knockBack * .3f, sword.owner, 8, radial.ToRotation(), owner.direction * owner.gravDir);
            count++;
        }
    }
}
