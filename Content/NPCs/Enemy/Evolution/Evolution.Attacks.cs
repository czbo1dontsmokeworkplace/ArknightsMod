using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    private void DoNewborn(Player player)
    {
        int t = Timer;
        if (t == 0) ClearBrood();
        if (t == 45 || t == 105)
        {
            var groups = EvolutionRules.NewbornGroups(Attack);
            int groupIndex = t == 45 ? 0 : 1;
            if (groupIndex < groups.Length)
            {
                var group = groups[groupIndex];
                for (int i = 0; i < group.Count; i++)
                {
                    float side = i % 2 == 0 ? -1 : 1;
                    Vector2 offset = group.Preset == EvolutionBroodPreset.Rain ? new Vector2((i - 2.5f) * 190, -430) :
                        group.Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb ? (i * MathHelper.TwoPi / group.Count + .4f).ToRotationVector2() * 690 :
                        new Vector2(side * (530 + i % 3 * 90), group.Preset == EvolutionBroodPreset.Ambush ? 260 : -220 + i * 80);
                    SpawnBrood(group.Kind, Arena + offset, Attack == 3 ? 200 + i * 22 : 265 + i * 24,
                        (player.Center - Arena - offset).ToRotation(), side, group.Preset, i);
                }
            }
        }
        // Each programme has its own pacing; the body no longer repeats the same support volley.
        if (t == 205 && Attack == 0) BloodFan(player, 3);
        if (t == 190 && Attack == 4) Shoot(EvolutionShot.Core, NPC.Center, -Vector2.UnitY * 5, delay: 50, lifetime: 75);
        if (t >= EvolutionRules.NewbornDuration(Attack)) NextAttack();
    }
    private void DoEvolved(Player player)
    {
        int t = Timer;
        if (Attack != 0) Hover(player, t * .011f);
        switch (Attack)
        {
            case 0: DoCharges(player, false); break;
            case 1:
                if (t == 0) ClearBrood();
                if (t == 45) for (int side = -1; side <= 1; side += 2) SpawnBrood(EvolutionBrood.Puppet, player.Center + new Vector2(side * 600, -280), side: side);
                if (t >= 95 && t <= 275 && (t - 95) % 60 == 0) LaserPattern(player, (t - 95) / 60 % 5, 34);
                if (t >= 365) NextAttack();
                break;
            case 2:
                if (t == 0) ClearBrood();
                if (t == 45)
                {
                    for (int i = 0; i < 4; i++) SpawnBrood(EvolutionBrood.Spider, player.Center + new Vector2(i % 2 == 0 ? -600 : 600, -180 + i * 120), side: i % 2 == 0 ? -1 : 1);
                    SpawnBrood(EvolutionBrood.GiantSpider, player.Center + new Vector2(600, -400), preset: EvolutionBroodPreset.Siege);
                }
                if (t == 125 || t == 200 || t == 275) BloodFan(player, 3);
                if (t >= 350) NextAttack();
                break;
            case 3:
                if (t == 0) ClearBrood();
                if (t == 45) SpawnBrood(EvolutionBrood.Abomination, player.Center + new Vector2(-600, -160));
                if (t == 100 || t == 180 || t == 260) SpiritFan(player, NPC.Center, 5, 32);
                if (t == 145 || t == 225) ThrowBomb(NPC.Center, player.Center + player.velocity * 15, t == 225, 84);
                if (t == 140 || t == 230) LanceComb(player.Center, MathHelper.PiOver4, 4, 36);
                if (t >= 355) NextAttack();
                break;
            case 4:
                if (t == 0) ClearBrood();
                if (t == 45) for (int i = 0; i < 3; i++)
                {
                    Vector2 point = player.Center + (i * MathHelper.TwoPi / 3).ToRotationVector2() * 550;
                    SpawnBrood(EvolutionBrood.Tumor, point, 220 + i * 40, (player.Center - point).ToRotation(), preset: EvolutionBroodPreset.Siege, formation: i);
                }
                if (t == 135 || t == 225) Beam(player, delay: 36);
                if (t >= 450) NextAttack();
                break;
            case 5:
                if (t >= 30 && t <= 270 && (t - 30) % 60 == 0) LaserPattern(player, (t - 30) / 60, 34);
                if (t >= 355) NextAttack();
                break;
        }
    }
    private void DoPerfect(Player player)
    {
        int t = Timer;
        if (Attack == 0) { DoCharges(player, true); return; }
        Hover(player, t * .019f);
        int interval = Desperate ? 42 : 48;
        if (t >= 28 && t <= 28 + interval * 5 && (t - 28) % interval == 0)
        {
            int wave = (t - 28) / interval;
            switch (Attack)
            {
                case 1: LaserWall(player.Center, MathHelper.PiOver2, wave % 2 == 0 ? -1 : 1, 28); break;
                case 2:
                    LaserWall(player.Center, wave % 2 == 0 ? 0 : MathHelper.PiOver4, 0, 28);
                    if (wave % 2 == 0) GroundWave(player.Center, wave % 4 == 0 ? 1 : -1);
                    break;
                case 3:
                    LaserPattern(player, wave % 2 == 0 ? 0 : 4, 28);
                    if (wave % 2 == 0) for (int side = -1; side <= 1; side += 2)
                        ThrowBomb(player.Center + new Vector2(side * 600, -300), player.Center + new Vector2(side * 280, 40), wave == 2, 74);
                    break;
                case 4:
                    LaserWall(player.Center, MathHelper.PiOver2, 0, 30);
                    for (int side = -1; side <= 1; side += 2) for (int i = -2; i <= 2; i++)
                        Shoot(EvolutionShot.Spirit, player.Center + new Vector2(side * 680, i * 125), new Vector2(-side * 4, 0), delay: 42, lifetime: 150);
                    break;
                default:
                    LaserPattern(player, wave % 5, 28);
                    if (wave % 2 == 1) LanceComb(player.Center + new Vector2(0, -160), MathHelper.PiOver2, 5, 32);
                    break;
            }
            if (Attack != 4 && wave % 2 == 0) SpiritFan(player, NPC.Center, 7, 36);
        }
        if (t >= 28 + interval * 5 + 125) NextAttack();
    }
    private void DoCharges(Player player, bool perfect)
    {
        int stride = perfect ? 86 : 100;
        int cycle = Timer / stride, t = Timer % stride;
        const int lockTime = 22;
        int warning = perfect ? 26 : 30;
        int launch = lockTime + warning + 4, end = launch + (perfect ? 18 : 20);
        if (cycle >= 3)
        {
            NPC.velocity *= .87f;
            if (Timer == stride * 3 + 18) { LaserPattern(player, perfect ? 3 : 4, 32); SpiritFan(player, NPC.Center, perfect ? 9 : 5, 36); }
            if (Timer >= stride * 3 + 145) NextAttack();
            return;
        }
        if (t < lockTime) MoveTo(player.Center + new Vector2(cycle % 2 == 0 ? -620 : 620, -80), perfect ? 28 : 22, .18f);
        if (t == lockTime)
        {
            NPC.velocity = Vector2.Zero;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Aim = player.Center + player.velocity * 8;
                DashDirection = (Aim - NPC.Center).SafeNormalize(Vector2.UnitX);
                NPC.netUpdate = true;
            }
            EvolutionImpactSystem.Emit(NPC.Center, 620, 0, true, warning);
        }
        if (t >= lockTime && t < launch)
        {
            NPC.velocity = Vector2.Zero;
            if (t < lockTime + warning) DashWarning = (t - lockTime + 1f) / warning;
        }
        if (t >= launch && t < end)
        {
            Charging = true;
            NPC.velocity = DashDirection * (perfect ? 36 : 32) * EvolutionRules.Aggression(Phase) * (Desperate ? 1.1f : 1);
        }
        if (t == launch) { EvolutionVisuals.Burst(NPC.Center, 1.4f); EvolutionImpactSystem.Emit(NPC.Center, 340, perfect ? 8 : 6); }
        if (t >= end) NPC.velocity *= .76f;
        if (perfect && t == end) SpiritFan(player, NPC.Center, 5, stride * 3 - Timer + 18);
    }
    private void SpiritFan(Player player, Vector2 origin, int count, int delay)
    {
        Vector2 toward = (player.Center - origin).SafeNormalize(Vector2.UnitY);
        for (int i = 0; i < count; i++) Shoot(EvolutionShot.Spirit, origin, toward.RotatedBy((i - (count - 1) * .5f) * .23f) * 4, delay: delay, lifetime: delay + 125);
    }
    // Snapshot aim; three omitted columns leave a wide, stationary escape corridor.
    private void LaserWall(Vector2 center, float angle, int corridor, int warning)
    {
        Vector2 direction = angle.ToRotationVector2(), normal = direction.RotatedBy(MathHelper.PiOver2);
        for (int column = 0; column <= EvolutionRules.WallHalfColumns * 2; column++)
        {
            int i = EvolutionRules.Column(column);
            if (Math.Abs(i - corridor) <= 1) continue;
            Shoot(EvolutionShot.Beam, center - direction * 1000 + normal * (i * EvolutionRules.WallSpacing), direction, 2000, warning, warning + 32, peripheral: Math.Abs(i) > 5);
        }
    }
    private void LanceComb(Vector2 center, float angle, int count, int warning)
    {
        Vector2 direction = angle.ToRotationVector2(), normal = direction.RotatedBy(MathHelper.PiOver2);
        count = Math.Max(count, 14);
        for (int column = 0; column <= count * 2; column++)
        {
            int i = EvolutionRules.Column(column);
            if (Math.Abs(i) <= 1) continue;
            Shoot(EvolutionShot.Lance, center - direction * 650 + normal * (i * 110), direction * 34, 1500, warning, warning + 65, peripheral: Math.Abs(i) > 5);
        }
    }
    private void LaserPattern(Player player, int pattern, int warning)
    {
        switch (pattern % 5)
        {
            case 0:
                for (int i = 0; i < 4; i++) Shoot(EvolutionShot.Beam, NPC.Center, (i * MathHelper.PiOver2 + NPC.ai[2] * .17f).ToRotationVector2(), 2100, warning, warning + 32);
                break;
            case 1: LaserWall(player.Center, MathHelper.PiOver2, 0, warning); break;
            case 2: LaserWall(player.Center, 0, 0, warning); break;
            case 3: LaserWall(player.Center, MathHelper.PiOver4, 0, warning); break;
            default: for (int i = -2; i <= 2; i++) Beam(player, i * .23f, delay: warning); break;
        }
    }
    private void DoTransition(Player player)
    {
        int t = Timer;
        NPC.dontTakeDamage = true;
        if (Phase == 2)
        {
            NPC.Center = Arena; NPC.velocity = Vector2.Zero;
            if (t == 20) BloodFan(player, 7);
            if (t == 70) for (int i = -1; i <= 1; i++) Beam(player, i * .4f, delay: 36);
            if (t == 120) LaserWall(player.Center, MathHelper.PiOver2, 0, 36);
            if (t == 175) LanceComb(player.Center, 0, 5, 36);
            if (t == 230) SpiritFan(player, NPC.Center, 9, 36);
            if (t == 285) LanceComb(player.Center, MathHelper.PiOver2, 5, 36);
            if (t == 340) LaserWall(player.Center, MathHelper.PiOver4, 0, 36);
            if (t == 395) for (int i = -1; i <= 1; i++) ThrowBomb(NPC.Center, player.Center + new Vector2(i * 270, 0), i != 0, 70);
            if (t == 450) LaserWall(player.Center, 0, 0, 36);
            if (t == 505) LaserPattern(player, 4, 34);
            if (t == 550) LaserPattern(player, 0, 30);
        }
        else
        {
            if (t < 60) MoveTo(player.Center + new Vector2(-520, -180), 17, .06f);
            else NPC.velocity *= .85f;
            if (t == 30) for (int i = 0; i < 4; i++)
            {
                Vector2 point = player.Center + (MathHelper.PiOver4 + i * MathHelper.PiOver2).ToRotationVector2() * 620;
                SpawnBrood(EvolutionBrood.Bomb, point, 220 + i * 60, (player.Center - point).ToRotation(), preset: EvolutionBroodPreset.Minefield, formation: i);
            }
            if (t >= 54 && t <= 654 && (t - 54) % 48 == 0) LaserPattern(player, (t - 54) / 48 % 5, 30);
            if (t == 180 || t == 372 || t == 564) SpiritFan(player, NPC.Center, 7, 40);
            if (t == 294 || t == 486) GroundWave(player.Center, t == 294 ? 1 : -1);
        }
        if (t >= EvolutionRules.TransitionDuration(Phase) - 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            EnterPhase(Phase == 2 ? 3 : 5); NPC.ai[1] = -24; NPC.dontTakeDamage = false;
        }
    }
}
