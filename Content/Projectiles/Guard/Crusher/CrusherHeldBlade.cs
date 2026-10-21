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
    private float age = -1, length = BladeSwingTemplate.BaseTicks, recovery;
    private int idle, sequence, special, skillMode, hitCount, manifestAge, skillLevel = 1, stroke, followTicks;
    private float aim, attackAim, radius, power = 1, previousAngle, sweepStartAngle, sweepEndAngle;
    private int facing = 1, baseDamage;
    private float baseKnockback;
    private bool appeared, waveReleased, singleStrike;
    private Item boundItem;
    private Vector2 grip, tip, previousTip, previousCenter, sweepStartCenter, sweepEndCenter;
    private readonly bool[] effectHits = new bool[Main.maxNPCs];
    internal CrusherKind Kind => (CrusherKind)(int)Projectile.ai[0];
    public bool Busy => age >= 0 || recovery > 0;
    private bool Swinging => age >= 0 && age < length;
    internal float Progress => age < 0 ? 1 : Math.Clamp(age / length, 0, 1);
    internal float Opacity => Math.Clamp((CrusherRules.IdleTicks + CrusherRules.FadeTicks - idle) / (float)CrusherRules.FadeTicks, 0, 1);
    private float Angle => BladeSwingTemplate.Angle(aim, facing, Progress);
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
    internal void BeginSwing(Player owner, int damage, float knockback, float forcedPower = 0, int forcedSpecial = 0)
    {
        if (Busy && forcedPower <= 0) return;
        boundItem = owner.HeldItem; baseDamage = damage; baseKnockback = knockback;
        stroke = 0; singleStrike = forcedPower > 0; recovery = 0;
        BeginStroke(owner, forcedPower, forcedSpecial);
    }
    private void BeginStroke(Player owner, float forcedPower = 0, int forcedSpecial = 0)
    {
        var state = owner.GetModPlayer<CrusherPlayer>();
        power = forcedPower > 0 ? forcedPower : state.PrepareSwing(out special);
        if (forcedPower > 0) special = forcedSpecial;
        skillMode = state.ActiveMode; skillLevel = state.SkillLevel;
        length = forcedSpecial == 3 ? 62 : BladeSwingTemplate.Duration(owner.GetAttackSpeed(DamageClass.Melee) * state.SwingSpeed);
        radius = CrusherRules.Reach(Kind) * state.ReachMultiplier * (forcedSpecial == 3 ? 1.22f : 1) * owner.GetAdjustedItemScale(owner.HeldItem);
        attackAim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(new Vector2(owner.direction, 0)).ToRotation();
        aim = attackAim; facing = (MathF.Cos(aim) >= 0 ? 1 : -1) * (owner.gravDir < 0 ? -1 : 1);
        age = 0; idle = hitCount = 0; sequence++; waveReleased = false;
        Projectile.damage = Math.Max(1, (int)(baseDamage * power)); Projectile.originalDamage = Projectile.damage;
        Projectile.knockBack = baseKnockback; Projectile.CritChance = owner.GetWeaponCrit(owner.HeldItem);
        Array.Clear(Projectile.localNPCImmunity); Array.Clear(effectHits); Projectile.numHits = 0;
        UpdatePose(owner); previousAngle = Angle; previousCenter = grip; previousTip = tip;
        sweepStartAngle = sweepEndAngle = Angle; sweepStartCenter = sweepEndCenter = grip;
        Projectile.netUpdate = true;
    }
    internal void StopForCharge() { age = -1; recovery = 0; idle = 0; }
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
        if (!appeared) { appeared = true; CrusherVisuals.Manifest(owner.MountedCenter, Kind); }
        if (Projectile.numUpdates == 0) manifestAge++;
        var state = owner.GetModPlayer<CrusherPlayer>();
        if (state.Charging) StopForCharge();
        else {
            // A click commits a pair. Releasing input never truncates its second stroke.
            // Peers wait for the owner to start each stroke, so no duplicate waves/skill use.
            if (age >= length && local) {
                if (!singleStrike && stroke == 0) { stroke = 1; BeginStroke(owner); }
                else { age = -1; recovery = length; Projectile.netUpdate = true; }
            }
            if (recovery > 0) recovery = Math.Max(0, recovery - 1f / (Projectile.extraUpdates + 1));
            if (!Busy && local && HoldingAttack(owner)) BeginSwing(owner, owner.GetWeaponDamage(w.Item), owner.GetWeaponKnockback(w.Item));
        }
        previousTip = tip;
        if (Swinging) {
            float before = Progress;
            age = BladeSwingTemplate.Advance(age, length, Projectile.extraUpdates);
            UpdatePose(owner);
            sweepStartAngle = previousAngle; sweepEndAngle = Angle;
            sweepStartCenter = previousCenter; sweepEndCenter = grip;
            if (before < .36f && Progress >= .36f && !Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -.3f + (int)Kind * .05f, Volume = .75f, MaxInstances = 4 }, grip);
            }
            if (!waveReleased && Progress >= .4f) {
                waveReleased = true;
                if (local) {
                    Vector2 forward = attackAim.ToRotationVector2();
                    int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + forward * 28,
                        forward * 60, ModContent.ProjectileType<CrusherWave>(), Math.Max(1, (int)(Projectile.damage * .65f)),
                        Projectile.knockBack * .6f, Projectile.owner, (int)Kind, facing, owner.GetAdjustedItemScale(w.Item) * (1.35f + (int)Kind * .08f));
                    if (id < Main.maxProjectiles) Main.projectile[id].netUpdate = true;
                }
                Projectile.netUpdate = true;
            }
            if (Projectile.numUpdates == 0 && Progress > .3f && Progress < .7f) CrusherVisuals.Shed(grip, tip, previousTip, Kind, manifestAge);
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
            UpdatePose(owner);
            if (Busy || state.Charging) idle = 0;
            else if (Projectile.numUpdates == 0 && ++idle >= CrusherRules.IdleTicks + CrusherRules.FadeTicks) { Projectile.Kill(); return; }
            if (Projectile.numUpdates == 0 && manifestAge % 4 == 0) CrusherVisuals.IdleMotes(grip, tip, Kind, Opacity);
        }
        owner.ChangeDir(MathF.Cos(aim) >= 0 ? 1 : -1); Projectile.direction = owner.direction;
        owner.heldProj = Projectile.whoAmI;
        if (Busy || state.Charging) owner.itemTime = owner.itemAnimation = 2;
        BladeSwingTemplate.Pose(owner, Angle, true);
        previousAngle = Angle; previousCenter = grip;
        if (!Main.dedServ) Lighting.AddLight(tip, CrusherRules.Tint(Kind).ToVector3() * .45f * Opacity);
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
        if (Main.myPlayer == Projectile.owner && hitCount <= 5) {
            Vector2 point = target.Hitbox.ClosestPointInRect(tip);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, (tip - previousTip).SafeNormalize((tip - grip).SafeNormalize(Vector2.UnitX)),
                ModContent.ProjectileType<CrusherImpact>(), 0, 0, Projectile.owner, (int)Kind, first ? 1 : .55f, hitCount);
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
        if (age >= 0) DreadnoughtArcTrail.Draw(grip, aim, facing, Progress, radius,
            CrusherRules.Tint(Kind), CrusherRules.Tint(Kind) * .2f, CrusherRules.Edge(Kind), Opacity);
        CrusherVisuals.DrawBlade(grip, tip, Kind, Opacity, false);
        return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(age); w.Write(length); w.Write(recovery); w.Write(idle); w.Write(sequence); w.Write(stroke); w.Write(singleStrike);
        w.Write(special); w.Write(skillMode); w.Write(aim); w.Write(attackAim); w.Write(radius); w.Write(facing); w.Write(power); w.Write(skillLevel);
        w.Write(manifestAge); w.Write(waveReleased); w.Write(Projectile.damage); w.Write(Projectile.knockBack); w.Write(Projectile.CritChance);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        int old = sequence;
        age = r.ReadSingle(); length = Math.Clamp(r.ReadSingle(), 8, 600); recovery = Math.Clamp(r.ReadSingle(), 0, 600);
        idle = r.ReadInt32(); sequence = r.ReadInt32(); stroke = Math.Clamp(r.ReadInt32(), 0, 1); singleStrike = r.ReadBoolean();
        special = r.ReadInt32(); skillMode = r.ReadInt32(); aim = r.ReadSingle(); attackAim = r.ReadSingle(); radius = Math.Clamp(r.ReadSingle(), 1, 1200);
        facing = r.ReadInt32() >= 0 ? 1 : -1; power = r.ReadSingle(); skillLevel = Math.Clamp(r.ReadInt32(), 1, 10);
        manifestAge = Math.Clamp(r.ReadInt32(), 0, 1000000); waveReleased = r.ReadBoolean();
        Projectile.damage = r.ReadInt32(); Projectile.knockBack = r.ReadSingle(); Projectile.CritChance = r.ReadInt32();
        if (sequence != old) { Array.Clear(Projectile.localNPCImmunity); Array.Clear(effectHits); hitCount = 0; }
        UpdatePose(Main.player[Projectile.owner]); previousAngle = sweepStartAngle = sweepEndAngle = Angle;
        previousCenter = sweepStartCenter = sweepEndCenter = grip; previousTip = tip;
    }
}
