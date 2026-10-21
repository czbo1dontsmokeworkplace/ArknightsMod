using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionBroodfallRules
{
    internal const int Lock = 0, Launch = 15, Impact = 24;
    internal const int Escape = 28, FirstFan = Impact + Escape, SecondFan = FirstFan + 104;
    internal const int Birth = SecondFan + 120, Duration = Birth + 172;
    internal const int FanWarning = 48, FanActive = 18, FanFade = 36;
    internal const float FanReach = 1100 * EvolutionRules.LaserReachMultiplier, FanWidth = 48, Gravity = .24f;
    internal const int FanRayCount = 9;
    private static readonly float[] fanAngles = { -66, -55, -44, -33, -22, 22, 44, 55, 66 };
    internal static Vector2 FanDirection(int ray) =>
        (-MathHelper.PiOver2 + MathHelper.ToRadians(fanAngles[ray])).ToRotationVector2();
    internal static bool Diving(int timer) => timer >= Launch && timer < Impact;
    internal static Vector2 DivePoint(int timer, Vector2 start, Vector2 ground)
    {
        float t = Math.Clamp((timer - Launch + 1f) / (Impact - Launch), 0, 1);
        return Vector2.Lerp(start, ground, t * t);
    }
}

public sealed partial class Evolution
{
    internal bool CrashOpeningComplete;
    internal bool CrashOpeningActive => Phase == 3 && !CrashOpeningComplete;
    internal Vector2 CrashStart, CrashAim, CrashGround;
    internal int CrashHeldPlayer = -1, CrashHoldUntil;
    private int crashVisualCue = -1;

    private void WriteCrashState(BinaryWriter writer)
    {
        writer.Write(CrashOpeningComplete); writer.WriteVector2(CrashStart);
        writer.WriteVector2(CrashAim); writer.WriteVector2(CrashGround);
        writer.Write(-1); writer.Write(0); writer.Write(false);
    }
    private void ReadCrashState(BinaryReader reader)
    {
        CrashOpeningComplete = reader.ReadBoolean(); CrashStart = reader.ReadVector2();
        CrashAim = reader.ReadVector2(); CrashGround = reader.ReadVector2();
        reader.ReadInt32(); reader.ReadInt32(); reader.ReadBoolean();
        CrashHeldPlayer = -1; CrashHoldUntil = 0;
    }

    private void DoCrashOpening(Player player)
    {
        int t = Timer;
        NPC.dontTakeDamage = true; // 额外的完整开场，不消耗原80%～40%血量段。
        if (t == 0 && Main.netMode != NetmodeID.MultiplayerClient) BeginCorridorSlam(player);
        if (t < EvolutionBroodfallRules.Launch) NPC.velocity = Vector2.Zero;
        if (EvolutionBroodfallRules.Diving(t))
        {
            NPC.velocity = EvolutionBroodfallRules.DivePoint(t, CrashStart, CrashGround) - NPC.Center;
            if (!Main.dedServ && t % 3 == 0) EvolutionArsenalVisuals.Smoke(NPC.Center, -NPC.velocity * .14f, 52, 22, true);
        }
        if (t >= EvolutionBroodfallRules.Impact)
        {
            if (t < EvolutionBroodfallRules.Birth + 72) { NPC.Center = CrashGround; NPC.velocity = Vector2.Zero; }
            else HoverLow(player);
            ReleaseCrashPassenger();
        }
        if (t == EvolutionBroodfallRules.FirstFan || t == EvolutionBroodfallRules.SecondFan)
            Shoot(EvolutionShot.CrashFan, CorridorSlamMuzzle, Vector2.Zero,
                delay: EvolutionBroodfallRules.FanWarning,
                lifetime: EvolutionBroodfallRules.FanWarning + EvolutionBroodfallRules.FanActive + EvolutionBroodfallRules.FanFade + 16,
                beamStyle: EvolutionBeamStyle.Refracted);
        if (t >= EvolutionBroodfallRules.Birth && t <= EvolutionBroodfallRules.Birth + 54 &&
            (t - EvolutionBroodfallRules.Birth) % 18 == 0)
        {
            int slot = (t - EvolutionBroodfallRules.Birth) / 18;
            EvolutionBrood kind = slot switch { 0 => EvolutionBrood.Spider, 1 => EvolutionBrood.GiantSpider,
                2 => EvolutionBrood.Puppet, _ => EvolutionBrood.Bomb };
            float side = slot % 2 == 0 ? -1 : 1;
            Vector2 near = CrashGround + new Vector2(side * (180 + slot * 46), NPC.height * .5f - 20);
            int height = EvolutionBroodNPC.GroundHeight(kind);
            Vector2 point = TryNestFloor(near, kind == EvolutionBrood.GiantSpider ? 58 : 36, height, out Vector2 floor) &&
                floor.Y + height * .5f < CorridorBottom ? floor : new Vector2(near.X, CorridorBottom - height * .5f - 8);
            SpawnBrood(kind, point, side: side, formation: slot);
        }
        if (t == EvolutionBroodfallRules.Birth + 20)
            for (int side = -1; side <= 1; side += 2)
                LobGravityFlare(NPC.Center + new Vector2(side * 56, 12), player.Center + new Vector2(side * 330, 80), 42);
        if (!Main.dedServ && t >= EvolutionBroodfallRules.Impact && crashVisualCue < EvolutionBroodfallRules.Impact)
        {
            crashVisualCue = EvolutionBroodfallRules.Impact;
            if (t < EvolutionBroodfallRules.Impact + 8)
            {
                EvolutionImpactSystem.Emit(CrashGround, 550, 7, false, 30);
                EvolutionVisuals.Burst(CrashGround, 1.8f);
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = .8f, Pitch = -.5f }, CrashGround);
                for (int i = 0; i < 14; i++) EvolutionArsenalVisuals.Smoke(CrashGround + new Vector2(0, 60),
                    new Vector2((i - 6.5f) * 1.3f, -Main.rand.NextFloat(1, 4)), 80, 38, i % 3 == 0);
            }
        }
        if (t >= EvolutionBroodfallRules.Duration - 1 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            CrashOpeningComplete = true;
            ClearHazards(); Serial++;
            NPC.ai[1] = -36; NPC.ai[2] = 0; Anchor = NPC.Center; NPC.netUpdate = true;
            // 保留刚孵化的地面小怪；原有循环从第0招开始，不能调用会递增招序的NextAttack。
        }
    }

    // 保留旧网络消息入口供消息分发读取；下砸不再伤人、裹挟、禁用输入或移动玩家。
    internal void TryCaptureCrashPassenger(Player player) { }
    internal bool HasCrashPassenger(Player player) => false;
    internal void CarryCrashPassenger(Player player) { }
    internal void ReleaseCrashPassenger() { CrashHeldPlayer = -1; CrashHoldUntil = 0; }
    private void DrawCrashOpening()
    {
        if (!CrashOpeningActive) return;
        if (Timer >= EvolutionBroodfallRules.Lock && Timer < EvolutionBroodfallRules.Launch)
        {
            float opacity = Math.Clamp((Timer - EvolutionBroodfallRules.Lock) / 12f, 0, 1);
            EvolutionVisuals.Line(CrashStart, CrashGround, EvolutionVisuals.Core * (.4f * opacity), 2, false);
            Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
            Main.spriteBatch.Draw(ring, CrashGround + new Vector2(0, 65) - Main.screenPosition, null,
                (EvolutionVisuals.Core with { A = 0 }) * opacity, 0, ring.Size() * .5f, new Vector2(230, 35) / ring.Size(), SpriteEffects.None, 0);
        }
        if (EvolutionBroodfallRules.Diving(Timer))
            EvolutionArsenalVisuals.Flame(NPC.Center, NPC.velocity.SafeNormalize(Vector2.UnitY), Timer, .9f, 150);
        if (Timer >= EvolutionBroodfallRules.Impact && Timer < EvolutionBroodfallRules.Birth + 72)
        {
            // 在无地面场地仍显示明确的育生落点；不修改真实地形。
            EvolutionVisuals.Glow(CrashGround + new Vector2(0, 75), EvolutionVisuals.Blood * .4f, new Vector2(300, 34));
        }
    }
    internal void LobGravityFlare(Vector2 origin, Vector2 destination, int flight)
    {
        float speed = Phase == 3 ? EvolutionCorridorPressureRules.Speed : 1;
        flight = Math.Max(1, (int)MathF.Round(flight / speed));
        float drag = MathF.Pow(.995f, speed), gravity = EvolutionBroodfallRules.Gravity * speed * speed;
        // Resolve the same destination with the faster discrete gravity/drag clock.
        float travel = drag * (1 - MathF.Pow(drag, flight)) / (1 - drag);
        Vector2 velocity = new((destination.X - origin.X) / travel,
            (destination.Y - origin.Y) / flight - gravity * (flight + 1) * .5f);
        Shoot(EvolutionShot.GravityFlare, origin, velocity / speed, delay: 24, lifetime: flight + 90);
    }
    internal void FireBroodRocket(Vector2 origin, Vector2 destination)
    {
        Shoot(EvolutionShot.BroodRocket, origin, (destination - origin).SafeNormalize(Vector2.UnitY) * 8,
            62, 36, 158);
    }
}

// 兼容既有消息编号，移除玩家控制/携带钩子；忽略旧的抓取请求。
public static class EvolutionCrashPlayer
{
    internal static void ReceiveCapture(BinaryReader reader, int sender)
    {
        reader.ReadInt16(); reader.ReadInt32();
    }
}
