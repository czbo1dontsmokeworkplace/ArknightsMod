using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

public sealed class ReaperSpin : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private float phase, turn, reach, angularStep = ReaperRules.SpinStep;
    private bool started;
    private int samples, age, wispCredit;
    private Vector2 grip, tip, previousTip;
    private readonly Vector2[] history = new Vector2[18];
    internal ReaperKind Kind => (ReaperKind)(int)Projectile.ai[0];
    internal static ReaperSpin Find(Player owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles) if (p.owner == owner.whoAmI && p.ModProjectile is ReaperSpin spin) return spin;
        return null;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 12; Projectile.ownerHitCheck = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner]; var state = owner.GetModPlayer<ReaperPlayer>();
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.selectedItem != (int)Projectile.ai[1]
            || owner.HeldItem.ModItem is not ReaperWeapon weapon || weapon.Kind != Kind || state.SuppressAttacks
            || owner.whoAmI == Main.myPlayer && !state.RightHeld) { Projectile.Kill(); return; }
        int direction = Projectile.velocity.X >= 0 ? 1 : -1;
        bool resetSweep = false;
        if (!started) { started = true; turn = direction > 0 ? MathHelper.PiOver2 : 0; if (owner.whoAmI == Main.myPlayer) BeginCycle(owner); }
        if (owner.whoAmI == Main.myPlayer) {
            float speed = owner.GetTotalAttackSpeed(DamageClass.Melee) * state.AttackSpeed;
            angularStep = Math.Clamp(ReaperRules.SpinStep * speed, .05f, .845f);
            reach = ReaperRules.Reach(Kind) * state.ReachMultiplier * owner.GetAdjustedItemScale(owner.HeldItem);
        }
        else if (reach <= 0) reach = ReaperRules.Reach(Kind);
        float step = angularStep;
        float oldPhase = phase; phase += step;
        // Preserve Dragon Rage's two-revolution facing window at the faster angular speed.
        if (owner.whoAmI == Main.myPlayer && phase % (MathHelper.TwoPi * 2) > MathHelper.TwoPi) {
            int wanted = Main.MouseWorld.X >= owner.MountedCenter.X ? 1 : -1;
            if (wanted != direction) { direction = wanted; Projectile.velocity = new Vector2(direction, 0); turn -= MathHelper.Pi; samples = 0; resetSweep = true; Projectile.netUpdate = true; }
        }
        if ((int)(oldPhase / MathHelper.TwoPi) != (int)(phase / MathHelper.TwoPi) && owner.whoAmI == Main.myPlayer) BeginCycle(owner);
        turn += step * direction; Projectile.rotation = turn; Projectile.timeLeft = 2; Projectile.Center = owner.MountedCenter;
        previousTip = tip; grip = owner.MountedCenter; tip = grip + turn.ToRotationVector2() * reach;
        if (age++ == 0 || resetSweep || Vector2.DistanceSquared(previousTip, grip) > 500 * 500) previousTip = tip;
        for (int i = Math.Min(samples, history.Length - 1); i > 0; i--) history[i] = history[i - 1];
        history[0] = tip; samples = Math.Min(history.Length, samples + 1);
        owner.ChangeDir(direction); owner.heldProj = Projectile.whoAmI; owner.itemTime = owner.itemAnimation = 2;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, turn - MathHelper.PiOver2);
        // Only the owning client creates gameplay projectiles; observers receive them normally.
        if (age % ReaperRules.WispInterval == 0 && owner.whoAmI == Main.myPlayer) {
            wispCredit += ReaperRules.WispEmissionTenths(Kind);
            // Distribute complete pairs using the retained quota. Consume the
            // opportunity even at the live-projectile cap; never bank a release burst.
            if (wispCredit >= 10) {
                wispCredit -= 10;
                int type = ModContent.ProjectileType<ReaperWisp>(), active = 0;
                foreach (Projectile shot in Main.ActiveProjectiles) if (shot.owner == owner.whoAmI && shot.type == type) active++;
                if (active <= ReaperRules.WispLimit - 2) {
                    // A fresh, independent axis for each pair; neither mouse nor spin phase determines it.
                    Vector2 radial = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                    for (int i = 0; i < 2; i++) {
                        Vector2 axis = i == 0 ? radial : -radial;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), grip + axis * reach, axis * ReaperRules.WispLaunchSpeed,
                            type, Math.Max(1, (int)(Projectile.damage * ReaperRules.WispDamage)),
                            Projectile.knockBack * .2f, owner.whoAmI, (int)Kind);
                    }
                }
            }
        }
        if (age % 4 == 0) ReaperVisuals.Shed(tip, tip - previousTip, Kind);
    }
    private void BeginCycle(Player owner)
    {
        var state = owner.GetModPlayer<ReaperPlayer>(); state.BeginAttack();
        Projectile.damage = Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * state.AttackPower));
        Projectile.netUpdate = true;
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFurySwing with { Volume = .42f, Pitch = -.15f, MaxInstances = 3 }, owner.Center);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!started || age < 2) return false;
        // Only the cutting head of the physical scythe deals contact damage. The handle and
        // empty center of the rotation never become a large circular healing hitbox.
        Vector2 oldAxis = (previousTip - grip).SafeNormalize(Vector2.UnitX), axis = (tip - grip).SafeNormalize(Vector2.UnitX);
        float angle = MathHelper.WrapAngle(axis.ToRotation() - oldAxis.ToRotation());
        for (int i = 0; i <= 5; i++) {
            Vector2 ray = oldAxis.RotatedBy(angle * i / 5f); float collision = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), grip + ray * reach * .46f, grip + ray * reach, 26 * ReaperRules.LeftScale, ref collision)) return true;
        }
        return false;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => Main.player[Projectile.owner].GetModPlayer<ReaperPlayer>().ModifyCut(target, ref modifiers);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Main.player[Projectile.owner].GetModPlayer<ReaperPlayer>().OnRightHit(target, damageDone);
        ReaperVisuals.Hit(target.Hitbox.ClosestPointInRect(tip),Kind,.5f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (samples > 1) ReaperMaterial.Trail(grip, history, samples, Kind, 1);
        ReaperVisuals.Blade(grip, tip, Kind, 1);
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(phase); writer.Write(turn); writer.Write(reach); writer.Write(angularStep); }
    public override void ReceiveExtraAI(BinaryReader reader) { phase = reader.ReadSingle(); turn = reader.ReadSingle(); reach = Math.Clamp(reader.ReadSingle(), 1, 500); angularStep = Math.Clamp(reader.ReadSingle(), .05f, .845f); started = true; }
}
