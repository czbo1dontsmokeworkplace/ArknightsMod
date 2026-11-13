using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

internal static class FrostNovaDomainRules
{
    internal const float Radius = 560f;
    internal const int ChillAt = 60, FrostAt = 120, WhiteoutAt = 180;
    internal static int Exposure(int previous, bool outside) => outside
        ? Math.Min(WhiteoutAt, previous + 1) : Math.Max(0, previous - 4);
    internal static bool InDomain(FrostNova boss) => boss.NPC.active && boss.FNBirthCompleted
        && !boss.AirbornePhase && boss.CombatHazardsActive;
    internal static FrostNova Nearest(Player player)
    {
        FrostNova nearest = null;
        float distance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is FrostNova boss && InDomain(boss)) {
                float candidate = Vector2.DistanceSquared(boss.HaloCenter, player.Center);
                if (candidate < distance) { distance = candidate; nearest = boss; }
            }
        return nearest;
    }
}

// Per-player exposure; the boss's current target never decides another player's penalty.
public sealed class FrostNovaDomainPlayer : ModPlayer
{
    internal int ExposureTicks;
    internal float Obscurity => ExposureTicks / (float)FrostNovaDomainRules.WhiteoutAt;
    public override void PostUpdate()
    {
        if (!Player.active || Player.dead || Player.ghost) { ExposureTicks = 0; return; }
        FrostNova boss = FrostNovaDomainRules.Nearest(Player);
        bool outside = boss != null && Vector2.DistanceSquared(boss.HaloCenter, Player.Center) > FrostNovaDomainRules.Radius * FrostNovaDomainRules.Radius;
        ExposureTicks = FrostNovaDomainRules.Exposure(ExposureTicks, outside);
        if (boss == null || !outside) return;
        if (ExposureTicks >= FrostNovaDomainRules.ChillAt) Player.AddBuff(BuffID.Chilled, 30);
        if (ExposureTicks >= FrostNovaDomainRules.FrostAt) Player.AddBuff(BuffID.Frostburn, 30);
        if (ExposureTicks >= FrostNovaDomainRules.WhiteoutAt) Player.AddBuff(ModContent.BuffType<FrostNovaWhiteout>(), 30);
    }
    public override void UpdateDead() => ExposureTicks = 0;
    public override void OnEnterWorld() => ExposureTicks = 0;
}

public sealed class FrostNovaWhiteout : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Frostburn2;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
    public override void Update(Player player, ref int buffIndex) => player.GetDamage(DamageClass.Generic) *= .25f;
}
