using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    internal bool CycleEnraged, AxisEscapeRecorded;
    private int axisCycleSerial = -1;
    private bool visualCycleEnraged;
    internal bool EnrageDamageReduction => Phase == 5 && CycleEnraged && Attack != 6;
    internal float CycleBoost => Phase == 5 && CycleEnraged && Attack != 6 ? EvolutionRules.EnrageMultiplier : 1;
    internal bool SpinAttackLocked => Phase == 5 && Attack == 7 && Timer >= EvolutionRules.SpinBombSetup &&
        Timer < EvolutionRules.SpinBombSetup + EvolutionRules.SpinBombDuration;
    internal void BeginAxisCycle()
    {
        if (axisCycleSerial == Serial) return;
        axisCycleSerial = Serial;
        CycleEnraged = AxisEscapeRecorded = false;
        NPC.netUpdate = true;
    }
    internal bool RecordAxisEscape(float distance, float reach)
    {
        int fire=EvolutionRules.AxisSetupTicks+EvolutionRules.FireTime(EvolutionShot.Beam,
            EvolutionRules.ProjectileWarning(EvolutionShot.Beam,EvolutionRules.AxisChargeTicks,true));
        if (Main.netMode == NetmodeID.MultiplayerClient || Phase != 5 || Attack != 6 ||
            Timer<fire || Timer>=fire+EvolutionRules.AxisActiveTicks || AxisEscapeRecorded ||
            !float.IsFinite(distance) || distance <= reach) return false;
        AxisEscapeRecorded = CycleEnraged = true;
        NPC.netUpdate = true;
        return true;
    }
    private void WritePerfectCycle(BinaryWriter writer)
    { writer.Write(CycleEnraged); writer.Write(AxisEscapeRecorded); writer.Write(axisCycleSerial); }
    private void ReadPerfectCycle(BinaryReader reader)
    { CycleEnraged = reader.ReadBoolean(); AxisEscapeRecorded = reader.ReadBoolean(); axisCycleSerial = reader.ReadInt32(); }
    private void UpdateCycleVisuals()
    {
        if (CycleEnraged && !visualCycleEnraged)
        {
            // 与召唤物同一声原版Roar；状态边沿只播放一次。
            SoundEngine.PlaySound(SoundID.Roar, Main.LocalPlayer.Center);
            CombatText.NewText(Main.LocalPlayer.Hitbox, new Color(255, 65, 80), Language.GetTextValue("Mods.ArknightsMod.EvolutionText.EscapeTaunt"), dramatic: true);
            EvolutionImpactSystem.Emit(NPC.Center, 800, 6, false, 36);
        }
        visualCycleEnraged = CycleEnraged;
        if (!CycleEnraged || Phase != 5 || Timer % 3 != 0) return;
        Vector2 offset = Main.rand.NextVector2CircularEdge(90, 110);
        Dust dust = Dust.NewDustPerfect(NPC.Center + offset, DustID.FireworkFountain_Red,
            offset.SafeNormalize(Vector2.UnitY) * 2 - NPC.velocity * .08f, 0, new Color(255, 22, 40), 1.4f);
        dust.noGravity = true;
        if (Timer % 6 == 0) EvolutionArsenalVisuals.Smoke(NPC.Center + offset, offset * .016f, 33, 24, true);
    }
    private void DrawCycleOutline(SpriteBatch batch, Texture2D texture, Vector2 position, Vector2 origin,
        Vector2 scale, SpriteEffects flip, float opacity)
    {
        if (!CycleEnraged || Phase != 5) return;
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = (i * MathHelper.PiOver4).ToRotationVector2() * (3 + MathF.Sin(NPC.ai[3] * .15f) * .7f);
            batch.Draw(texture, position + offset, null, new Color(255, 12, 30, 0) * (opacity * .65f),
                NPC.rotation, origin, scale, flip, 0);
        }
        EvolutionVisuals.Glow(NPC.Center, new Color(255, 8, 34) * (.35f * opacity), new Vector2(290, 330));
    }
    private void DoWhirlingBombs(Player player)
    {
        int t = Timer, setup = EvolutionRules.SpinBombSetup;
        if (t == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            float side = NPC.Center.X < player.Center.X ? -1 : 1;
            Anchor = player.Center + new Vector2(side * 680, -230);
            Aim = player.Center;
            NPC.netUpdate = true;
        }
        if (t < setup)
        {
            MoveTo(Anchor, 19, .09f);
            if (!Main.dedServ && t % 12 == 0)
                EvolutionArsenalVisuals.Smoke(NPC.Center + Main.rand.NextVector2Circular(50, 50), Vector2.Zero, 35, 24, true);
            return;
        }
        int active = t - setup;
        if (active < EvolutionRules.SpinBombDuration)
        {
            if (active == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                DashDirection = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                NPC.netUpdate = true;
            }
            float angle = EvolutionRules.SpinBombAngle(active);
            // 自转严格两圈/105帧；中心仍持续接近玩家，不把转圈误做成原地站桩。
            Vector2 toward = (player.Center - NPC.Center).SafeNormalize(DashDirection);
            float distance = NPC.Distance(player.Center);
            Vector2 desired = toward * MathHelper.Clamp((distance - 230) * .025f, 1.5f, 7) * CycleBoost;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, .075f * CycleBoost);
            NPC.rotation = angle;
            NPC.spriteDirection = NPC.direction = 1;
            if (EvolutionRules.SpinBombEmission(active,CycleEnraged))
            {
                Vector2 outward = (DashDirection.ToRotation() + angle).ToRotationVector2();
                Shoot(EvolutionShot.SpinBomb, NPC.Center + outward * 75, (outward * 9 + NPC.velocity * .25f) * 3,
                    parameter: 180, delay: 50, lifetime: 94, quietLaunch: true);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item61 with { Volume = .24f, Pitch = -.45f, MaxInstances = 2 }, NPC.Center);
            }
            return;
        }
        NPC.velocity *= .92f;
        if (t >= setup + EvolutionRules.SpinBombDuration + 90) NextAttack();
    }
}
