using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    internal bool CorridorSlamActive => Phase == 3 && (CrashOpeningActive || Attack == 14);
    internal bool CorridorSlamUnconstrained => CorridorSlamActive && Timer >= 0 &&
        Timer < (CrashOpeningActive ? EvolutionBroodfallRules.Birth + 72 : EvolutionFleetRules.SlamRelease);
    internal Vector2 CorridorSlamMuzzle => new(CrashGround.X, Math.Clamp(CrashGround.Y + 26, CorridorTop + 82, CorridorBottom - 40));
    internal Vector2 CrashCeiling => new(CrashGround.X, CorridorTop + NPC.height * .5f + 6);
    internal Vector2 CeilingSlamMuzzle => new(CrashGround.X, CorridorTop + 40);
    private void CeilingImpact()
    {
        NPC.Center = CrashCeiling; NPC.velocity = Vector2.Zero; NPC.damage = 0; Charging = false;
        if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient) NPC.netUpdate = true;
        if (Main.dedServ) return;
        EvolutionImpactSystem.Emit(CrashCeiling, 430, 5, false, 24);
        EvolutionArsenalVisuals.Burst(CrashCeiling, 1.5f);
        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_ExplosiveTrapExplode with { Volume = .55f, Pitch = -.3f }, CrashCeiling);
    }
    internal void BeginCorridorSlam(Player player)
    {
        // 锁定瞬间位于玩家正上方；整段只沿这个X垂直下砸，不侧移或预判落点。
        ReleaseCrashPassenger();
        float x = player.Center.X, halfHeight = NPC.height * .5f;
        CrashAim = player.Center;
        CrashStart = new Vector2(x, player.Top.Y - halfHeight - 140);
        float floorY = CorridorBottom - 6;
        if (EvolutionTerrain.TrySurface(new Vector2(x, player.Bottom.Y - 2), out Vector2 floor,
            (int)Math.Ceiling(EvolutionRules.CorridorHeight / 16) + 2) && floor.Y <= CorridorBottom)
            floorY = floor.Y;
        CrashGround = new Vector2(x, floorY - halfHeight);
        NPC.Center = CrashStart;
        NPC.velocity = Vector2.Zero;
        NPC.damage = 0; Charging = false;
        if (NPC.oldPos != null) Array.Fill(NPC.oldPos, NPC.position);
        NPC.netUpdate = true;
    }
}
