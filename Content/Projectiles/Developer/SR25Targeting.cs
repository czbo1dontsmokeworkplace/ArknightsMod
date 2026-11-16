using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Items.Developer;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Developer;

public sealed class SR25Player : ModPlayer
{
    internal bool ServerScanning;
    internal bool ServerAiming, RadarEquipped, HideRadar;
    internal int TargetIndex = -1, TargetType;
    private ulong nextShot;
    internal bool CanFire => Main.GameUpdateCount >= nextShot;
    internal void Fired() => nextShot = Main.GameUpdateCount + SR25.ShotInterval;
    internal bool Alive => Player.active && !Player.dead && !Player.noItems && !Player.CCed;
    internal bool Aiming => Alive
        && Player.HeldItem.ModItem is SR25 && (Player.whoAmI == Main.myPlayer
            ? Main.mouseRight && ChargedOperatorWeapon.WorldInputAllowed(Player) : ServerAiming);
    internal bool Scanning => Alive && (RadarEquipped || Aiming);
    internal NPC Target => Scanning && (uint)TargetIndex < Main.maxNPCs
        && Main.npc[TargetIndex].type == TargetType && SR25LockSystem.Legal(Main.npc[TargetIndex])
        ? Main.npc[TargetIndex] : null;

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Aiming) modifiers.FinalDamage *= 2f;
    }
    public override void ModifyZoom(ref float zoom)
    {
        if (Aiming) zoom = Math.Max(zoom, .8f);
    }
    public override void ResetEffects() { RadarEquipped = false; HideRadar = false; }
    public override void SetControls()
    {
        // Right click belongs to the scope, including when the cursor is over a chest/NPC.
        if (Player.HeldItem.ModItem is SR25) Player.controlUseTile = false;
    }
    public override void UpdateDead() => Clear();
    public override void PlayerDisconnect() => Clear();
    internal void Clear() { ServerScanning = ServerAiming = false; TargetIndex = -1; TargetType = 0; }
    internal void ResetWorld() { Clear(); nextShot = 0; RadarEquipped = HideRadar = false; }

    internal void SetTarget(bool scanning, NPC target)
        => SetScanState(scanning, target, scanning);
    internal void SetScanState(bool scanning, NPC target, bool aiming)
    {
        int index = target?.whoAmI ?? -1, type = target?.type ?? 0;
        if (ServerScanning == scanning && ServerAiming == aiming && TargetIndex == index && TargetType == type) return;
        ServerScanning = scanning; ServerAiming = aiming; TargetIndex = index; TargetType = type;
        if (Main.netMode == NetmodeID.Server) Send(-1);
    }
    private void Send(int toWho)
    {
        var packet = Mod.GetPacket();
        packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.SR25TargetState);
        packet.Write((byte)Player.whoAmI);
        packet.Write((byte)((ServerScanning ? 1 : 0) | (ServerAiming ? 2 : 0)));
        packet.Write((short)TargetIndex);
        packet.Write(TargetType);
        packet.Send(toWho);
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode == NetmodeID.Server) Send(toWho);
    }
    internal static void Receive(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        int player = reader.ReadByte();
        byte flags = reader.ReadByte();
        int index = reader.ReadInt16(), type = reader.ReadInt32();
        // Clients can send aim/input through their holdout, never choose the server's mark.
        if ((uint)player >= Main.maxPlayers
            || index < -1 || index >= Main.maxNPCs) return;
        var state = Main.player[player].GetModPlayer<SR25Player>();
        state.ServerScanning = (flags & 1) != 0;
        state.ServerAiming = (flags & 2) != 0;
        state.TargetIndex = index;
        state.TargetType = type;
    }
}

public sealed class SR25LockSystem : ModSystem
{
    internal const float ScanRange = 2400f;
    internal const float HalfAngle = MathHelper.Pi / 12f;
    internal const float AttractionRange = 640f;
    internal static readonly List<NPC> MarkedTargets = new();
    private readonly SR25Holdout[] scanners = new SR25Holdout[Main.maxPlayers];

    internal static bool Legal(NPC npc) => npc.active && !npc.friendly && !npc.dontTakeDamage
        && !npc.immortal && npc.life > 0 && npc.CanBeChasedBy();
    internal static int Root(NPC npc) => (uint)npc.realLife < Main.maxNPCs ? npc.realLife : npc.whoAmI;
    internal static bool Boss(NPC npc) => npc.boss || Main.npc[Root(npc)].boss;

    internal static NPC SelectTarget(Player owner, Vector2 aim, Vector2 cursor, bool panoramic = false)
    {
        NPC best = null;
        bool bestBoss = false;
        float bestDistance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!Legal(npc)) continue;
            Vector2 offset = npc.Center - owner.MountedCenter;
            if (offset.LengthSquared() > ScanRange * ScanRange
                || !panoramic && Vector2.Dot(offset.SafeNormalize(aim), aim) < MathF.Cos(HalfAngle)) continue;
            Vector2 nearest = npc.Hitbox.ClosestPointInRect(owner.MountedCenter);
            if (!Collision.CanHitLine(owner.MountedCenter, 1, 1, nearest, 1, 1)) continue;
            bool boss = !panoramic && Boss(npc);
            float distance = panoramic ? offset.LengthSquared() : Vector2.DistanceSquared(cursor, npc.Hitbox.ClosestPointInRect(cursor));
            if (best != null && (bestBoss && !boss || boss == bestBoss && distance >= bestDistance)) continue;
            best = npc; bestBoss = boss; bestDistance = distance;
        }
        return best;
    }

    public override void PostUpdateProjectiles()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Array.Clear(scanners);
            foreach (Projectile p in Main.ActiveProjectiles)
                if ((uint)p.owner < Main.maxPlayers && p.ModProjectile is SR25Holdout hold && hold.Scanning)
                    scanners[p.owner] = hold;
            foreach (Player player in Main.ActivePlayers)
            {
                var state = player.GetModPlayer<SR25Player>();
                SR25Holdout scan = scanners[player.whoAmI];
                bool valid = !player.dead && !player.noItems && !player.CCed && player.HeldItem.ModItem is SR25
                    && scan != null && (int)scan.Projectile.ai[0] == player.selectedItem;
                bool passive = state.Alive && state.RadarEquipped;
                state.SetScanState(valid || passive,
                    passive ? SelectTarget(player, Vector2.UnitX, player.Center, true)
                        : valid ? SelectTarget(player, scan.Aim, scan.Cursor) : null, valid);
            }
        }
        MarkedTargets.Clear();
        foreach (Player player in Main.ActivePlayers)
        {
            NPC target = player.GetModPlayer<SR25Player>().Target;
            if (target != null && !MarkedTargets.Contains(target)) MarkedTargets.Add(target);
        }
    }
    internal static bool Marked(NPC npc)
    {
        if (!Legal(npc)) return false;
        foreach (Player player in Main.ActivePlayers)
        {
            NPC target = player.GetModPlayer<SR25Player>().Target;
            if (target != null && Root(target) == Root(npc)) return true;
        }
        return false;
    }
    public override void OnWorldUnload()
    {
        MarkedTargets.Clear();
        Array.Clear(scanners);
        foreach (Player p in Main.player) p?.GetModPlayer<SR25Player>().ResetWorld();
    }
}

public sealed class SR25MarkedNPC : GlobalNPC
{
    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (SR25LockSystem.Marked(npc)) modifiers.FinalDamage *= 1.2f;
    }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        if (npc.lifeRegen >= 0 || !SR25LockSystem.Marked(npc)) return;
        // Integer ratios avoid flooring -120.000008 to -121 through float rounding.
        npc.lifeRegen = (int)Math.Max(int.MinValue, ((long)npc.lifeRegen * 6 - 4) / 5);
        damage = (int)Math.Min(int.MaxValue, ((long)damage * 6 + 4) / 5);
    }
    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        foreach (Player player in Main.ActivePlayers)
            if (player.GetModPlayer<SR25Player>().Target == npc)
            {
                SR25Visuals.DrawReticle(npc);
                break;
            }
    }
}

public sealed class SR25FocusProjectile : GlobalProjectile
{
    internal static bool Eligible(Projectile p)
        => p.active && p.friendly && !p.hostile && p.damage > 0 && (uint)p.owner < Main.maxPlayers
        && !p.minion && !p.sentry && !p.npcProj && !p.trap
        && !p.DamageType.CountsAsClass(DamageClass.Summon)
        && !ProjectileID.Sets.MinionShot[p.type] && !ProjectileID.Sets.SentryShot[p.type]
        && !ProjectileID.Sets.IsAWhip[p.type] && Main.player[p.owner].heldProj != p.whoAmI
        && p.aiStyle is not (ProjAIStyleID.HeldProjectile or ProjAIStyleID.Spear or ProjAIStyleID.Flail or ProjAIStyleID.Yoyo)
        && p.ModProjectile?.ShouldUpdatePosition() != false && p.velocity.LengthSquared() > .25f;

    public override void PostAI(Projectile projectile)
    {
        if (SR25LockSystem.MarkedTargets.Count == 0 || !Eligible(projectile)) return;
        NPC best = null;
        float distance = SR25LockSystem.AttractionRange * SR25LockSystem.AttractionRange;
        foreach (NPC npc in SR25LockSystem.MarkedTargets)
        {
            if (!SR25LockSystem.Marked(npc) || !npc.CanBeChasedBy(projectile)) continue;
            float next = Vector2.DistanceSquared(projectile.Center, npc.Center);
            if (next >= distance || !Collision.CanHitLine(projectile.Center, 1, 1,
                npc.Hitbox.ClosestPointInRect(projectile.Center), 1, 1)) continue;
            best = npc; distance = next;
        }
        if (best == null) return;
        float speed = projectile.velocity.Length();
        float angle = projectile.velocity.ToRotation().AngleTowards((best.Center - projectile.Center).ToRotation(),
            .11f / projectile.MaxUpdates);
        projectile.velocity = angle.ToRotationVector2() * speed;
    }
}
