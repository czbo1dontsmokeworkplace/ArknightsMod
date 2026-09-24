using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public abstract partial class EvolutionBroodNPC
{
    private bool Beat(int interval, int first = 90) => Age >= first + Formation * 17 && (Age - first - Formation * 17) % interval == 0;
    private void DoPreset(Evolution boss, Player player)
    {
        float side = NPC.ai[2] < 0 ? -1 : 1;
        float aggression = EvolutionRules.Aggression(boss.Phase);
        Vector2 toward = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
        if (Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb)
        {
            FlyTo(FlightAnchor + new Vector2(MathF.Sin(Age * .014f + Formation) * 35, MathF.Sin(Age * .03f) * 18), 4);
            if (Preset == EvolutionBroodPreset.Siege && Beat(108, 85)) boss.EruptAt(player.Center + new Vector2((Formation - 1) * 185, 24), 52);
            if (Preset == EvolutionBroodPreset.Seeder && Beat(108, 80))
                boss.ThrowBomb(NPC.Center, player.Center + new Vector2(side * 280, -100 + Formation * 35), true, 92);
            if (Age >= Fuse && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Preset == EvolutionBroodPreset.Minefield)
                    boss.Shoot(EvolutionShot.CrimsonBomb, NPC.Center, Vector2.Zero, 150, 0, 36);
                NPC.ai[3] = -35; NPC.damage = 0; NPC.dontTakeDamage = true; NPC.netUpdate = true;
            }
            return;
        }
        if (Preset == EvolutionBroodPreset.Ambush)
        {
            int beat = (Age - 60 + Formation * 15) % 164;
            if (beat < 52) FlyTo(player.Center + new Vector2(side * 540, 210), 18 * aggression);
            if (beat == 52 && Main.netMode != NetmodeID.MultiplayerClient)
            { FlightDirection = (player.Center + player.velocity * 6 - NPC.Center).SafeNormalize(-Vector2.UnitY); NPC.netUpdate = true; }
            if (beat >= 52 && beat < 90) { NPC.velocity = Vector2.Zero; if (beat < 86) warning = (beat - 51f) / 34; }
            if (beat >= 90 && beat < 110) { charging = true; NPC.damage = 55; NPC.velocity = FlightDirection * 20 * aggression; }
            if (beat >= 110) FlyTo(player.Center + new Vector2(-side * 580, -210), 14 * aggression);
            return;
        }
        if (Preset == EvolutionBroodPreset.Rain)
        {
            // A fixed aerial firing line, not six enemies glued to the player.
            FlyTo(FlightAnchor + new Vector2(MathF.Sin(Age * .02f + Formation) * 60, MathF.Cos(Age * .025f) * 35), 15 * aggression);
            if (Beat(96, 82)) for (int i = -1; i <= 1; i++)
                boss.Shoot(EvolutionShot.Lance, NPC.Center, Vector2.UnitY.RotatedBy(i * .18f) * 24, 1550, 34, 116);
            return;
        }
        float orbit = Age * .022f + Formation * 1.8f;
        float altitude = Preset == EvolutionBroodPreset.Artillery ? -420 : Preset == EvolutionBroodPreset.Siege && Kind == EvolutionBrood.Abomination ? 80 : -230;
        FlyTo(player.Center + new Vector2(side * (520 + MathF.Sin(orbit) * 100), altitude + MathF.Cos(orbit * .8f) * 120),
            (Kind == EvolutionBrood.Puppet ? 18 : 12) * aggression);
        if (boss.Desperate || boss.Transitioning) return;
        switch (Preset)
        {
            case EvolutionBroodPreset.Siege:
                if (Beat(120))
                {
                    if (Kind == EvolutionBrood.GiantSpider)
                        boss.Lob(EvolutionShot.Rock, NPC.Center, player.Center + new Vector2(-side * 180, 40), 64);
                    else for (int i = -1; i <= 1; i++) boss.EruptAt(player.Center + new Vector2(i * 190, 24), 48 + (i + 1) * 8);
                }
                break;
            case EvolutionBroodPreset.Artillery:
                if (Beat(125))
                {
                    boss.ThrowBomb(NPC.Center, player.Center + player.velocity * 12, Formation % 2 == 0, 94);
                    for (int i = -1; i <= 1; i++) boss.Lob(EvolutionShot.Blood, NPC.Center, player.Center + new Vector2(i * 170, 0), 74);
                }
                break;
            case EvolutionBroodPreset.Minefield:
                if (Beat(126)) boss.ThrowBomb(NPC.Center, player.Center + new Vector2(side * 210, -40), Formation % 2 == 1, 86);
                break;
            case EvolutionBroodPreset.Weaver:
                if (Beat(100)) for (int i = -1; i <= 1; i++)
                    boss.Shoot(EvolutionShot.Lance, NPC.Center, toward.RotatedBy(i * .38f) * 25, 1700, 36, 126);
                if (Beat(150, 136)) for (int i = -1; i <= 1; i += 2)
                    boss.Shoot(EvolutionShot.Spirit, NPC.Center, toward.RotatedBy(i * .75f) * 4, delay: 42, lifetime: 155);
                break;
            case EvolutionBroodPreset.Conductor:
                if (Beat(132))
                {
                    boss.ThrowBomb(NPC.Center, player.Center + new Vector2(0, 130), false, 82);
                    for (int i = -1; i <= 1; i += 2)
                        boss.Shoot(EvolutionShot.Beam, NPC.Center, toward.RotatedBy(i * .4f), 1750, 40, 76);
                }
                break;
        }
    }
}
