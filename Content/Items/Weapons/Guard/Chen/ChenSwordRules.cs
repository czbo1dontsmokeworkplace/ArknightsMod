using System;
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
    internal const int Skill3StartupTicks = 8; // ceil(previous 25 * .3)
    internal const int Skill3FirstStrikeTicks = 2; // ceil(previous 6 * .3)
    internal const int Skill3Interval = 6, Skill3Strikes = 10;
    internal const int Skill3SlashTicks = 14; // ceil(previous 45 * .3)
    internal static float Skill3Speed(int strike) => MathF.Pow(1.6f, Math.Clamp(strike, 0, Skill3Strikes - 1) / 2);
    internal static int Skill3Gap(int completed) => completed == 0 ? Skill3FirstStrikeTicks
        : Math.Max(1, (int)MathF.Ceiling(Skill3Interval / Skill3Speed(completed)));
    internal static int Skill3SlashDuration(float speed) => Math.Max(1, (int)MathF.Ceiling(Skill3SlashTicks / MathF.Max(1, speed)));
    internal static float Skill3SwingProgress(float progress) => 1 - MathF.Pow(1 - Math.Clamp(progress, 0, 1), 4);
    internal const float Skill3Range = 1000, Skill3OwnerRange = 2000;
    internal static void EndSkill(Player owner, int skill)
    {
        if (owner.whoAmI != Main.myPlayer || owner.HeldItem.ModItem is not ChenSword_Item) return;
        var state = owner.GetModPlayer<WeaponPlayer>();
        // The held-item guard already identifies Chen. A just-ended attack
        // must not wait for the cached skill descriptor to refresh first.
        if (state.Skill == skill) {
            state.SkillActive = false; state.SkillTimer = 0;
        }
    }

    internal static void FinishSkillOne(Projectile source)
    {
        if (source.owner != Main.myPlayer) return;
        Player player = Main.player[source.owner];
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == source.owner && p.ai[2] == 1 && p.ModProjectile is sw_Proj_1 or sw_Proj_2) p.Kill();
        EndSkill(player, 0);
        player.itemAnimation = player.itemTime = 0;
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

}
