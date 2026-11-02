using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

public sealed class DreadnoughtThrownBlade : ModProjectile
{
    private enum BladeState { Flying = 1, Embedded, Grounded, Returning, Extracting }
    internal const int BladesPerTarget = 5;
    internal const int ExtractionTicks = 8;
    internal ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    private BladeState State { get => (BladeState)(int)Projectile.ai[1]; set => Projectile.ai[1] = (int)value; }
    private int TargetIndex { get => (int)Projectile.ai[2] - 1; set => Projectile.ai[2] = value + 1; }
    internal bool Returning => State is BladeState.Returning or BladeState.Extracting;
    private bool Local => Projectile.owner == Main.myPlayer;
    private int targetType, skill, sequence;
    private float age, targetRotation;
    private Vector2 targetOffset, previousCenter;
    private bool ripping;
    internal float Length => (72 + (int)Kind * 12) * Projectile.scale
        * (Kind == ChargedOperatorKind.NearlRadiant && skill is 1 or 3 ? 1.15f : 1f);
    private Vector2 Axis => Projectile.rotation.ToRotationVector2();
    public override string Texture => "Terraria/Images/MagicPixel";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 28;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.timeLeft = 3600 * 3;
        Projectile.ignoreWater = true; Projectile.tileCollide = false;
        Projectile.extraUpdates = 2;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
    }
    public override void OnSpawn(IEntitySource source)
    {
        var owner = Main.player[Projectile.owner];
        skill = ChargedOperatorPlayer.ActiveSkill(owner);
        sequence = Projectile.identity;
        Projectile.scale = owner.GetAdjustedItemScale(owner.HeldItem);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.rotation = aim.ToRotation();
        Projectile.Center = owner.MountedCenter + Collision.TileCollision(owner.MountedCenter - new Vector2(6), aim * 26, 12, 12, true, true);
        Projectile.velocity = aim * ((14 + (int)Kind * .7f) * (1 + .075f * MathF.Sin(sequence * 2.4f)));
        State = BladeState.Flying; Projectile.tileCollide = true;
        previousCenter = Projectile.Center;
        if (Local) owner.GetModPlayer<DreadnoughtPlayer>().Attacking(owner.HeldItem);
        DreadnoughtThrowVisuals.Release(Projectile.Center, aim, Kind);
    }
    public override bool ShouldUpdatePosition() => State == BladeState.Flying || Returning;
    public override bool? CanDamage() => State == BladeState.Flying || Returning ? null : false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanHitNPC(NPC target) => ripping ? target.whoAmI == TargetIndex
        : State != BladeState.Flying && !Returning ? false
        : State == BladeState.Flying && !Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (ripping) return true;
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousCenter,
            Projectile.Center + Axis * Length, 14 * Projectile.scale, ref collision);
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(targetType); w.Write(skill); w.Write(sequence);
        w.Write(age); w.Write(targetRotation);
        w.Write(targetOffset.X); w.Write(targetOffset.Y);
        w.Write(Projectile.rotation); w.Write(Projectile.scale);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        targetType = r.ReadInt32(); skill = r.ReadInt32(); sequence = r.ReadInt32();
        age = r.ReadSingle(); targetRotation = r.ReadSingle();
        targetOffset = new(r.ReadSingle(), r.ReadSingle());
        Projectile.rotation = r.ReadSingle(); Projectile.scale = r.ReadSingle();
        previousCenter = Projectile.Center;
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || (int)Kind is < 0 or > 3) { Projectile.Kill(); return; }
        previousCenter = Projectile.Center;
        age += 1f / (Projectile.extraUpdates + 1);
        if (State == BladeState.Flying)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (age > 16) Projectile.velocity.Y += .035f;
            if (age > 90 && Local) StartRecall();
        }
        else if (State == BladeState.Embedded)
        {
            NPC target = TargetIndex >= 0 && TargetIndex < Main.maxNPCs ? Main.npc[TargetIndex] : null;
            if (target == null || !target.active || target.life <= 0 || target.type != targetType)
            { if (Local) StartRecall(); return; }
            Projectile.Center = target.Center + targetOffset.RotatedBy(target.rotation - targetRotation);
            Projectile.rotation = Projectile.velocity.ToRotation() + target.rotation - targetRotation;
            if (age >= 1800 && Local) StartRecall();
        }
        else if (State == BladeState.Grounded)
        {
            if (age >= 1800 && Local) StartRecall();
        }
        else if (State == BladeState.Extracting)
        {
            // SHPCKFast: an outward kick loses speed while the weapon turns,
            // followed by a separate fast return. Ages are measured in real ticks.
            Projectile.velocity *= .88f;
            float facing = (owner.MountedCenter - Projectile.Center).ToRotation();
            Projectile.rotation = Projectile.rotation.AngleTowards(facing, .1f);
            if (age >= ExtractionTicks)
            {
                State = BladeState.Returning; age = 0;
                Projectile.velocity = (owner.MountedCenter - Projectile.Center).SafeNormalize(Vector2.UnitX) * 28;
                Projectile.netUpdate = true;
            }
        }
        else if (State == BladeState.Returning)
        {
            Vector2 toOwner = owner.MountedCenter - Projectile.Center;
            const float speed = 28;
            if (toOwner.LengthSquared() < (speed + 18) * (speed + 18) || age > 180)
            { Projectile.Kill(); return; }
            Vector2 desired = toOwner.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .16f);
            if (Projectile.velocity.LengthSquared() < 64) Projectile.velocity = desired;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        if (Projectile.numUpdates == 0) DreadnoughtThrowVisuals.Flight(Projectile, Kind, Returning,
            State == BladeState.Flying || Returning);
    }
    internal void StartRecall()
    {
        if (!Local || Returning || !Projectile.active) return;
        bool embedded = State == BladeState.Embedded && TargetIndex >= 0 && TargetIndex < Main.maxNPCs
            && Main.npc[TargetIndex].active && Main.npc[TargetIndex].type == targetType;
        State = BladeState.Extracting; age = 0; Projectile.tileCollide = false;
        Array.Clear(Projectile.localNPCImmunity); Projectile.numHits = 0;
        // Resolve the extraction once through Terraria's actual damage pipeline,
        // then retain that target's local immunity during the piercing return.
        if (embedded) { ripping = true; Projectile.Damage(); ripping = false; }
        TargetIndex = -1;
        // Pull out along the blade, then fan slightly so multiple blades do not
        // occupy a single return path. Keep this deterministic across peers.
        Vector2 kick = (-Axis).RotatedBy((sequence % 2 == 0 ? 1 : -1) * (1.05f + sequence % 3 * .16f));
        Projectile.velocity = kick * (14 + sequence % 3);
        previousCenter = Projectile.Center;
        Array.Clear(Projectile.oldPos);
        DreadnoughtThrowVisuals.Extract(Projectile.Center, kick, Kind);
        Projectile.netUpdate = true;
    }
    internal static int EmbeddedCount(Player owner, ChargedOperatorKind kind, int target, int type)
    {
        if (!Main.npc.IndexInRange(target) || !Main.npc[target].active || Main.npc[target].life <= 0 || Main.npc[target].type != type) return 0;
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner.whoAmI && p.ModProjectile is DreadnoughtThrownBlade blade
                && blade.Kind == kind && blade.State == BladeState.Embedded && blade.TargetIndex == target && blade.targetType == type) count++;
        return Math.Min(count, BladesPerTarget);
    }
    internal static void Recall(Player owner, ChargedOperatorKind kind)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner.whoAmI && p.ModProjectile is DreadnoughtThrownBlade blade && blade.Kind == kind) blade.StartRecall();
    }
    internal static void MakeRoom(Player owner, ChargedOperatorKind kind)
    {
        int count = 0; DreadnoughtThrownBlade oldest = null;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner.whoAmI && p.ModProjectile is DreadnoughtThrownBlade blade && blade.Kind == kind && !blade.Returning)
            { count++; if (oldest == null || p.identity < oldest.Projectile.identity) oldest = blade; }
        if (count >= 24) oldest?.StartRecall();
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (!Returning) { modifiers.SourceDamage *= 3; modifiers.Knockback *= 3; modifiers.SetCrit(); }
        if (Kind == ChargedOperatorKind.Franka && Main.rand.NextFloat() < (skill == 2 ? .75f : .25f)
            || Kind == ChargedOperatorKind.NearlRadiant && skill == 3) modifiers.DefenseEffectiveness *= 0;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Local || damageDone <= 0) return;
        Main.player[Projectile.owner].GetModPlayer<DreadnoughtPlayer>().RegisterHit();
        if (Kind == ChargedOperatorKind.Franka) target.AddBuff(BuffID.OnFire3, 180);
        Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Hitbox.ClosestPointInRect(Projectile.Center + Axis * Length * .7f),
            Axis, ModContent.ProjectileType<DreadnoughtThrowImpact>(), 0, 0, Projectile.owner, (int)Kind, Returning ? 1 : 0);
        if (Returning) return;
        int count = 0; DreadnoughtThrownBlade oldest = null;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.ModProjectile is DreadnoughtThrownBlade other && other.Kind == Kind
                && other.State == BladeState.Embedded && other.TargetIndex == target.whoAmI)
            { count++; if (oldest == null || p.identity < oldest.Projectile.identity) oldest = other; }
        if (count >= BladesPerTarget) oldest?.StartRecall();
        targetOffset = Projectile.Center - target.Center;
        if (targetOffset.Length() > Length) targetOffset = -Axis * Length * .65f;
        TargetIndex = target.whoAmI; targetType = target.type; targetRotation = target.rotation;
        State = BladeState.Embedded; age = 0; Projectile.tileCollide = false; Projectile.netUpdate = true;
        Main.player[Projectile.owner].GetModPlayer<DreadnoughtPlayer>().TrackTarget(target, Kind);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = oldVelocity; Projectile.rotation = oldVelocity.ToRotation();
        State = BladeState.Grounded; age = 0; Projectile.tileCollide = false; Projectile.netUpdate = true;
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Dig with { Volume = .5f }, Projectile.Center);
        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    { DreadnoughtThrowVisuals.Draw(Projectile, Kind, Returning, State == BladeState.Flying || Returning, Length, lightColor); return false; }
}
