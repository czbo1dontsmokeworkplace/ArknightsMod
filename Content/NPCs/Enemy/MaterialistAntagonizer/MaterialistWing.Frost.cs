using System;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    internal const int FrostSprayTicks = 180, FrostWarmupTicks = 30, FrostTransferTicks = 120;
    internal const float FrostShieldMultiplier = .60f;
    internal int FrostCycleTick = -1;
    internal bool FrostExposed;
    private Vector2 frostTransferStart, frostTransferEnd;
    private bool frostTransferReady;
    private float frostShieldProgress;

    internal bool FrostEmitting => FrostCycleTick >= 0 && FrostCycleTick < FrostSprayTicks;
    internal bool FrostDamaging => FrostCycleTick >= FrostWarmupTicks && FrostEmitting;

    internal void UpdateFrostProtection(NPC npc, MaterialistAntagonizer boss)
    {
        if (MaterialistAntagonizer.Authority && !FrostExposed && boss.EscortsReleased)
        {
            bool otherAlive = false;
            foreach (NPC other in Main.ActiveNPCs)
                if (other.life > 0 && other.ModNPC is not (FrostDrone or BomberDrone) &&
                    other.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(boss) && !wing.Retreating &&
                    wing.Slot >= 0 && wing.Slot < MaterialistRules.WingCap(boss.Phase))
                { otherAlive = true; break; }
            if (!otherAlive) { FrostExposed = true; npc.netUpdate = true; }
        }
        // Exposure is permanent for this airframe, even if a later Bomber sortie enters the fight.
        npc.defense = FrostExposed ? 0 : 22;
    }

    private void UpdateFrostShieldVisual(MaterialistAntagonizer boss)
    {
        if (!Main.dedServ)
        {
            bool visible = !FrostExposed && !Retreating && boss.EscortsReleased && !boss.Paused;
            frostShieldProgress = MathHelper.Clamp(frostShieldProgress + (visible ? 1 / 30f : -1 / 18f), 0, 1);
        }
    }

    internal static Vector2 FrostDestination(Vector2 playerCenter, Vector2 playerVelocity, int facing, int slot)
    {
        Vector2 direction = playerVelocity.SafeNormalize(new Vector2(facing < 0 ? -1 : 1, 0));
        float lead = MathHelper.Clamp(playerVelocity.Length() * FrostTransferTicks + 360, 720, 1920);
        Vector2 lane = new Vector2(-direction.Y, direction.X) * (slot % 2 == 0 ? -96 : 96);
        // Sample once per transfer. The mist hangs 68 pixels below the hull.
        return Vector2.Clamp(playerCenter + direction * lead + lane - new Vector2(0, 68),
            new Vector2(180, 160), new Vector2(Main.maxTilesX * 16 - 180, Main.maxTilesY * 16 - 200));
    }

    private void RunFrost(NPC npc, FrostDrone frost, MaterialistAntagonizer boss, Player target)
    {
        FrostCycleTick = (FrostCycleTick + 1) % (FrostSprayTicks + FrostTransferTicks);
        if (FrostEmitting)
        {
            npc.velocity = Vector2.Zero;
            npc.rotation = MathHelper.Lerp(npc.rotation, 0, .2f);
            if (FrostCycleTick == 0)
            {
                frostTransferReady = false;
                if (MaterialistAntagonizer.Authority)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), frost.MistCenter, Vector2.Zero,
                        ModContent.ProjectileType<MaterialistFrostMist>(), MaterialistRules.ContainmentDamage, 0,
                        Main.myPlayer, npc.whoAmI, Encounter, boss.Phase);
                    npc.netUpdate = true;
                }
            }
            frost.EmitMist(FrostCycleTick, 2);
            return;
        }

        if (FrostCycleTick == FrostSprayTicks && MaterialistAntagonizer.Authority)
        {
            frostTransferStart = npc.Center;
            frostTransferEnd = FrostDestination(target.Center, target.velocity, target.direction, Slot);
            frostTransferReady = true;
            npc.netUpdate = true;
        }
        if (!frostTransferReady) { npc.velocity = Vector2.Zero; return; }

        int elapsed = FrostCycleTick - FrostSprayTicks;
        // A fixed segment lasting exactly 120 updates. No target resampling or steering mid-flight.
        npc.Center = Vector2.Lerp(frostTransferStart, frostTransferEnd, elapsed / (float)FrostTransferTicks);
        npc.velocity = (frostTransferEnd - frostTransferStart) / FrostTransferTicks;
        if (Math.Abs(npc.velocity.X) > .1f) npc.direction = npc.spriteDirection = Math.Sign(npc.velocity.X);
        npc.rotation = MathHelper.Lerp(npc.rotation, MathHelper.Clamp(npc.velocity.X * .018f, -.35f, .35f), .2f);
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (!Bound || npc.ModNPC is not FrostDrone) return;
        var boss = MaterialistAntagonizer.Owner(OwnerIndex, Encounter);
        if (boss != null) UpdateFrostProtection(npc, boss);
        if (FrostExposed) modifiers.Defense *= 0;
        else modifiers.FinalDamage *= FrostShieldMultiplier;
    }

    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (Bound && npc.ModNPC is FrostDrone && frostShieldProgress > .001f)
            MaterialistVisuals.DrawFrostShield(npc, frostShieldProgress);
    }

    private void WriteFrostAI(BinaryWriter writer)
    {
        writer.Write(FrostCycleTick); writer.Write(FrostExposed); writer.Write(frostTransferReady);
        writer.WriteVector2(frostTransferStart); writer.WriteVector2(frostTransferEnd);
    }

    private void ReadFrostAI(NPC npc, BinaryReader reader)
    {
        FrostCycleTick = reader.ReadInt32(); FrostExposed = reader.ReadBoolean(); frostTransferReady = reader.ReadBoolean();
        frostTransferStart = reader.ReadVector2(); frostTransferEnd = reader.ReadVector2();
        npc.defense = FrostExposed ? 0 : 22;
    }
}
