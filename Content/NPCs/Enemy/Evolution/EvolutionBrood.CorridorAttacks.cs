using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionBroodRangedRules
{
    internal static bool IsHatchling(EvolutionBrood kind) => kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb;
    internal static bool IsLobber(EvolutionBrood kind) => kind is EvolutionBrood.Puppet or EvolutionBrood.GiantSpider;
    internal static int ShotBeat(EvolutionBrood kind) => kind switch
    {
        EvolutionBrood.Spider => 112, EvolutionBrood.GiantSpider or EvolutionBrood.Puppet => 105,
        EvolutionBrood.Abomination => 125, _ => -1
    };
    internal static int FlightTicks(int flight, float scale) => Math.Max(16, (int)MathF.Round(flight / scale));
    internal static Vector2 LobVelocity(Vector2 origin, Vector2 target, int flight) =>
        new((target.X - origin.X) / flight, (target.Y - origin.Y) / flight - .22f * (flight + 1) * .5f);
    internal static bool HatchlingBlast(int beat, float distance) => beat >= 60 && (distance <= 64 || beat >= 82);
}

public sealed partial class Evolution
{
    internal void HatchlingExplosion(Vector2 center)
    {
        // Local blast only: never feed this into the corridor's extra-projectile fan-out.
        SpawnHazard(EvolutionShot.SpinBomb, center, Vector2.Zero, 72, 0, 28, false,
            EvolutionBeamStyle.Standard, false, false, false, true);
    }
}

public abstract partial class EvolutionBroodNPC
{
    private bool hatchlingDetonated;
    private void UpdateEvolvedRangedAttack(Evolution boss, Player player, int beat)
    {
        int fire = EvolutionBroodRangedRules.ShotBeat(Kind);
        if (fire < 0) return;
        if (beat == fire - 24 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            FlightAnchor = player.Center;
            NPC.netUpdate = true;
        }
        if (beat >= fire - 24 && beat < fire)
        {
            warning = Math.Max(warning, (beat - fire + 25) / 24f);
            if (Kind is EvolutionBrood.Puppet or EvolutionBrood.Abomination) NPC.velocity.X *= .85f;
        }
        if (beat != fire) return;
        NPC.direction = NPC.spriteDirection = FlightAnchor.X < NPC.Center.X ? -1 : 1;
        if (EvolutionBroodRangedRules.IsLobber(Kind)) boss.LobBroodDust(GroundMuzzle, FlightAnchor, 70);
        else if (Kind == EvolutionBrood.Abomination)
        {
            foreach (int side in new[] { -1, 1 })
                boss.Shoot(EvolutionShot.BroodSpit, NPC.Center + new Vector2(side * NPC.width * .4f, -NPC.height * .18f),
                    Vector2.UnitX * (side * 8.5f), parameter: -1, delay: 0, lifetime: 100);
        }
        else boss.Shoot(EvolutionShot.BroodSpit, GroundMuzzle, Vector2.UnitX * (NPC.direction * 8.5f),
            parameter: -1, delay: 0, lifetime: 100);
    }
    private bool UpdateEvolvedHatchling(Evolution boss, Player player, int beat)
    {
        NPC.damage = 0; charging = false; // The local detonation supplies the damage, without a double contact hit.
        if (beat == 45 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            FlightDirection = new Vector2(player.Center.X < NPC.Center.X ? -1 : 1, 0);
            NPC.netUpdate = true;
        }
        if (beat >= 45 && beat < 60)
        { NPC.velocity.X *= .65f; warning = (beat - 44) / 15f; }
        if (beat >= 60)
        {
            float side = FlightDirection.X == 0 ? NPC.direction : Math.Sign(FlightDirection.X);
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, side * 12, .28f);
            warning = 1;
        }
        if (!hatchlingDetonated && EvolutionBroodRangedRules.HatchlingBlast(beat, NPC.Distance(player.Center)) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            hatchlingDetonated = true;
            boss.HatchlingExplosion(NPC.Center);
            NPC.StrikeInstantKill();
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            return true;
        }
        return false;
    }
}
