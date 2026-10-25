using System;
using System.IO;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

public sealed class CrusherHeldBlade : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private float age = -1, length = BladeSwingTemplate.BaseTicks;
    private int idle, sequence, special, skillMode, hitCount, manifestAge, skillLevel = 1, followTicks;
    private float aim, attackAim, radius, power = 1, previousAngle, sweepStartAngle, sweepEndAngle;
    private int facing = 1, baseDamage;
    private float baseKnockback;
    private bool pursuitReleased, poseInitialized, charging, readyPlayed;
    private int chargeTicks, chargeLimit = CrusherRules.BaseChargeTicks;
    private float swingCharge;
    private float restAngle, entryOffset;
    private Item boundItem;
    private Vector2 grip, tip, previousTip, previousCenter, sweepStartCenter, sweepEndCenter;
    private readonly bool[] effectHits = new bool[Main.maxNPCs];
    internal CrusherKind Kind => (CrusherKind)(int)Projectile.ai[0];
    public bool Busy => charging || age >= 0;
    public bool IsCharging => charging;
    public float ChargeProgress => Math.Clamp(chargeTicks / (float)chargeLimit, 0, 1);
    internal float Outline => charging ? ChargeProgress : swingCharge;
    private bool Swinging => age >= 0 && age < length;
    internal float Progress => age < 0 ? 1 : Math.Clamp(age / length, 0, 1);
    internal float Opacity => Math.Clamp((CrusherRules.IdleTicks + CrusherRules.FadeTicks - idle) / (float)CrusherRules.FadeTicks, 0, 1);
    // Re-aim during the slow wind-up, never teleport the arms at a stroke boundary.
    private float SwingAim => aim + entryOffset * (1 - MathHelper.SmoothStep(0, 1, Math.Clamp(Progress / .3f, 0, 1)));
    private float Angle => age >= 0 ? BladeSwingTemplate.Angle(SwingAim, facing, Progress) : restAngle;
    internal static CrusherHeldBlade Find(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is CrusherHeldBlade blade
                && (int)p.ai[1] == player.selectedItem && player.HeldItem.ModItem is CrusherWeapon w && w.Kind == blade.Kind) return blade;
        return null;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = 2; Projectile.netImportant = true; Projectile.extraUpdates = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPvp(Player target) => false;
    internal void BeginCharge(Player owner)
    {
        if (Busy || owner.GetModPlayer<CrusherPlayer>().Charging) return;
        boundItem = owner.HeldItem;
        charging = true; chargeTicks = 0; readyPlayed = false; swingCharge = 0; idle = 0;
        var state = owner.GetModPlayer<CrusherPlayer>();
        chargeLimit = CrusherRules.ChargeDuration(owner.GetAttackSpeed(DamageClass.Melee) * state.SwingSpeed);
        aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0)).ToRotation();
        facing = (MathF.Cos(aim) >= 0 ? 1 : -1) * (owner.gravDir < 0 ? -1 : 1);
        if (!poseInitialized) { restAngle = BladeSwingTemplate.Angle(aim, facing, 0); poseInitialized = true; }
        radius = CrusherRules.Reach(Kind) * state.ReachMultiplier * owner.GetAdjustedItemScale(owner.HeldItem);
        UpdatePose(owner); Projectile.netUpdate = true;
    }
    internal void BeginSwing(Player owner, int damage, float knockback, float forcedPower = 0, int forcedSpecial = 0, float releasedCharge = 0)
    {
        if (age >= 0 && forcedPower <= 0) return;
        charging = false; swingCharge = Math.Clamp(releasedCharge, 0, 1);
        boundItem = owner.HeldItem; baseDamage = damage; baseKnockback = knockback;
        BeginStroke(owner, forcedPower, forcedSpecial);
    }
    private void BeginStroke(Player owner, float forcedPower = 0, int forcedSpecial = 0)
    {
        float incomingAngle = Angle;
        var state = owner.GetModPlayer<CrusherPlayer>();
        power = forcedPower > 0 ? forcedPower : state.PrepareSwing(out special, swingCharge >= 1);
        if (forcedPower > 0) special = forcedSpecial;
        skillMode = state.ActiveMode; skillLevel = state.SkillLevel;
        length = forcedSpecial == 3 ? 62 : BladeSwingTemplate.Duration(owner.GetAttackSpeed(DamageClass.Melee) * state.SwingSpeed);
        radius = CrusherRules.Reach(Kind) * state.ReachMultiplier * (forcedSpecial == 3 ? 1.22f : 1) * owner.GetAdjustedItemScale(owner.HeldItem);
        attackAim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0)).ToRotation();
        aim = attackAim; facing = (MathF.Cos(aim) >= 0 ? 1 : -1) * (owner.gravDir < 0 ? -1 : 1);
        entryOffset = poseInitialized
            ? MathHelper.WrapAngle(incomingAngle - BladeSwingTemplate.Angle(aim, facing, 0)) : 0;
        poseInitialized = true;
        age = 0; idle = hitCount = 0; sequence++; pursuitReleased = false;
        // The manual charge and skill multiplier each apply once. Dedicated skill
        // finishers keep their own damage formula and only inherit visual charge.
        Projectile.damage = Math.Max(1, (int)(baseDamage * power * (forcedPower > 0 ? 1 : CrusherRules.ChargeDamage(swingCharge))));
        Projectile.originalDamage = Projectile.damage;
        Projectile.knockBack = baseKnockback * MathHelper.Lerp(1, 1.6f, swingCharge); Projectile.CritChance = owner.GetWeaponCrit(owner.HeldItem);
        Array.Clear(Projectile.localNPCImmunity); Array.Clear(effectHits); Projectile.numHits = 0;
        UpdatePose(owner); previousAngle = Angle; previousCenter = grip; previousTip = tip;
        sweepStartAngle = sweepEndAngle = Angle; sweepStartCenter = sweepEndCenter = grip;
        Projectile.netUpdate = true;
    }
    internal void StopForCharge()
    {
        if (age >= 0) restAngle = Angle;
        age = -1; charging = false; chargeTicks = 0; swingCharge = 0; idle = 0;
    }
    private static bool HoldingAttack(Player owner) => owner.controlUseItem && !ArknightsKeybinds.SkillActivatePressed(owner)
        && ChargedOperatorWeapon.WorldInputAllowed(owner);
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner]; bool local = owner.whoAmI == Main.myPlayer;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.selectedItem != (int)Projectile.ai[1]
            || owner.HeldItem.ModItem is not CrusherWeapon w || w.Kind != Kind
            || local && boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        Projectile.timeLeft = 2; Projectile.Center = owner.MountedCenter + new Vector2(0, owner.gfxOffY);
        if (Projectile.numUpdates == 0) manifestAge++;
        var state = owner.GetModPlayer<CrusherPlayer>();
        if (state.Charging) { StopForCharge(); swingCharge = state.Charge; }
        else {
            if (age >= length && local) {
                restAngle = Angle; age = -1; Projectile.netUpdate = true;
            }
            if (!Busy && local && HoldingAttack(owner)) BeginCharge(owner);
            if (charging) {
                if (local && !ChargedOperatorWeapon.WorldInputAllowed(owner)) { Projectile.Kill(); return; }
                // Check release before advancing: one frame short is never full charge.
                if (local && !owner.controlUseItem)
                    BeginSwing(owner, owner.GetWeaponDamage(w.Item), owner.GetWeaponKnockback(w.Item), releasedCharge: ChargeProgress);
                else if (Projectile.numUpdates == 0) {
                    chargeTicks = Math.Min(chargeLimit, chargeTicks + 1);
                    if (local && chargeTicks % 6 == 0 && chargeTicks < chargeLimit) Projectile.netUpdate = true;
                    if (chargeTicks == chargeLimit && !readyPlayed) {
                        readyPlayed = true;
                        if (local) {
                            Projectile.netUpdate = true;
                            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item29 with { Volume = .8f, Pitch = .25f }, owner.Center);
                        }
                    }
                }
            }
        }
        previousTip = tip;
        if (Swinging) {
            float before = Progress;
            // Advance the entire full-charge swing curve 1.5x faster, including
            // its wind-up and follow-through; pose, trail and sweep share Progress.
            age = Math.Min(length, age + (swingCharge >= 1 ? 1.5f : 1f) / (Projectile.extraUpdates + 1));
            UpdatePose(owner);
            sweepStartAngle = previousAngle; sweepEndAngle = Angle;
            sweepStartCenter = previousCenter; sweepEndCenter = grip;
            if (before < .36f && Progress >= .36f && !Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -.3f + (int)Kind * .05f, Volume = .75f, MaxInstances = 4 }, grip);
            }
            if (Projectile.numUpdates == 0 && Progress > .3f && Progress < .7f) CrusherVisuals.Shed(grip, tip, previousTip, Kind, manifestAge, swingCharge);
            idle = 0;
        }
        else {
            if (local && !state.Charging) {
                float wanted = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0)).ToRotation();
                aim = aim.AngleLerp(wanted, .1f / (Projectile.extraUpdates + 1));
                if (Projectile.numUpdates == 0 && ++followTicks >= 6 && Math.Abs(MathHelper.WrapAngle(wanted - aim)) > .001f) {
                    followTicks = 0; Projectile.netUpdate = true;
                }
            }
            if (!poseInitialized) {
                restAngle = BladeSwingTemplate.Angle(aim, facing, 1);
                poseInitialized = true;
            }
            restAngle = restAngle.AngleLerp(BladeSwingTemplate.Angle(aim, facing, charging ? 0 : 1),
                .1f / (Projectile.extraUpdates + 1));
            UpdatePose(owner);
            if (Busy || state.Charging || HoldingAttack(owner)) idle = 0;
            else if (Projectile.numUpdates == 0 && ++idle >= CrusherRules.IdleTicks + CrusherRules.FadeTicks) { Projectile.Kill(); return; }
            if (Projectile.numUpdates == 0 && manifestAge % 4 == 0 && Outline > 0) CrusherVisuals.IdleMotes(grip, tip, Kind, Opacity * Outline);
        }
        owner.ChangeDir(MathF.Cos(aim) >= 0 ? 1 : -1); Projectile.direction = owner.direction;
        owner.heldProj = Projectile.whoAmI;
        if (Busy || state.Charging) owner.itemTime = owner.itemAnimation = 2;
        BladeSwingTemplate.Pose(owner, Angle, true);
        previousAngle = Angle; previousCenter = grip;
        if (!Main.dedServ) Lighting.AddLight(tip, CrusherRules.Tint(Kind).ToVector3() * .65f * Opacity * Outline);
    }
    private void UpdatePose(Player owner)
    {
        grip = owner.MountedCenter + new Vector2(0, owner.gfxOffY);
        tip = grip + Angle.ToRotationVector2() * Math.Max(1, radius);
    }
    public override bool? CanDamage() => Swinging && age > 0 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CanDamage() != false
        && BladeSwingTemplate.Contact(targetHitbox, sweepStartAngle, sweepEndAngle, sweepStartCenter, sweepEndCenter,
            radius, Kind == CrusherKind.Ulpianus ? 38 : 24, facing, out _, out _);
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == CrusherKind.Hoederer && OperatorStunNPC.HasStun(target)) modifiers.FinalDamage *= 1.2f;
        modifiers.HitDirectionOverride = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        bool first = hitCount++ == 0;
        var owner = Main.player[Projectile.owner];
        owner.GetModPlayer<CrusherPlayer>().Hit(target, first, special, skillMode);
        CrusherCombatNet.Hit(Projectile, target);
        if (Main.myPlayer == Projectile.owner && swingCharge >= 1 && !pursuitReleased) {
            pursuitReleased = true;
            CrusherUpheaval.Release(Projectile, target, Kind, special, skillLevel);
            Projectile.netUpdate = true;
        }
        if (Main.myPlayer == Projectile.owner && hitCount <= 5) {
            Vector2 point = target.Hitbox.ClosestPointInRect(tip);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, (tip - previousTip).SafeNormalize((tip - grip).SafeNormalize(Vector2.UnitX)),
                ModContent.ProjectileType<CrusherImpact>(), 0, 0, Projectile.owner, (int)Kind,
                MathHelper.Lerp(.65f, 1.7f, swingCharge) * (first ? 1 : .55f), hitCount);
        }
    }
    internal void ApplyHitEffects(NPC target)
    {
        if (effectHits[target.whoAmI]) return;
        effectHits[target.whoAmI] = true;
        Main.player[Projectile.owner].GetModPlayer<CrusherPlayer>().ApplyHitEffects(target, skillMode, skillLevel);
        if (special == 3 && !target.boss && target.realLife < 0 && target.knockBackResist > 0) OperatorStunNPC.TryApply(target, 60);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!CrusherVisuals.Visible(Projectile.Center)) return false;
        if (age >= 0) DreadnoughtArcTrail.Draw(grip, SwingAim, facing, Progress, radius,
            CrusherRules.Tint(Kind), CrusherRules.Tint(Kind) * .2f, CrusherRules.Edge(Kind), Opacity * MathHelper.Lerp(.35f, 1, swingCharge));
        CrusherVisuals.DrawBlade(grip, tip, Kind, Opacity, Swinging && swingCharge >= 1, Outline);
        return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(age); w.Write(length); w.Write(idle); w.Write(sequence);
        w.Write(special); w.Write(skillMode); w.Write(aim); w.Write(attackAim); w.Write(radius); w.Write(facing); w.Write(power); w.Write(skillLevel);
        w.Write(manifestAge); w.Write(pursuitReleased); w.Write(Projectile.damage); w.Write(Projectile.knockBack); w.Write(Projectile.CritChance);
        w.Write(poseInitialized); w.Write(restAngle); w.Write(entryOffset);
        w.Write(charging); w.Write(chargeTicks); w.Write(chargeLimit); w.Write(swingCharge);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        int old = sequence;
        age = r.ReadSingle(); length = Math.Clamp(r.ReadSingle(), 8, 600);
        idle = r.ReadInt32(); sequence = r.ReadInt32();
        special = r.ReadInt32(); skillMode = r.ReadInt32(); aim = r.ReadSingle(); attackAim = r.ReadSingle(); radius = Math.Clamp(r.ReadSingle(), 1, 1200);
        facing = r.ReadInt32() >= 0 ? 1 : -1; power = r.ReadSingle(); skillLevel = Math.Clamp(r.ReadInt32(), 1, 10);
        manifestAge = Math.Clamp(r.ReadInt32(), 0, 1000000); pursuitReleased = r.ReadBoolean();
        Projectile.damage = r.ReadInt32(); Projectile.knockBack = r.ReadSingle(); Projectile.CritChance = r.ReadInt32();
        poseInitialized = r.ReadBoolean(); restAngle = r.ReadSingle(); entryOffset = r.ReadSingle();
        charging = r.ReadBoolean(); chargeTicks = Math.Max(0, r.ReadInt32()); chargeLimit = Math.Clamp(r.ReadInt32(), 12, 600);
        chargeTicks = Math.Min(chargeTicks, chargeLimit); swingCharge = Math.Clamp(r.ReadSingle(), 0, 1);
        if (sequence != old) { Array.Clear(Projectile.localNPCImmunity); Array.Clear(effectHits); hitCount = 0; }
        UpdatePose(Main.player[Projectile.owner]); previousAngle = sweepStartAngle = sweepEndAngle = Angle;
        previousCenter = sweepStartCenter = sweepEndCenter = grip; previousTip = tip;
    }
    public override void OnKill(int timeLeft)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.heldProj != Projectile.whoAmI) return;
        owner.heldProj = -1;
        if (owner.selectedItem == (int)Projectile.ai[1] && owner.HeldItem.ModItem is CrusherWeapon w && w.Kind == Kind) {
            owner.itemTime = owner.itemAnimation = 0; owner.channel = false;
        }
    }
}
