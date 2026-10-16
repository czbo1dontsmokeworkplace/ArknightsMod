using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

// ai[0] operator, ai[1] rise/fall/thrust/volley, ai[2] elapsed real ticks.
public sealed class VanguardSpearHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public VanguardSpearKind Kind => (VanguardSpearKind)(int)Projectile.ai[0];
    public int Stage => (int)Projectile.ai[1];
    private int duration = 28, skill, slot, echoTargets;
    private float size = 1, gravity = 1;
    private bool accent, configured, soundPlayed, hitEffect;
    private Item boundItem;
    private readonly Vector2[] history = new Vector2[12];
    private Vector2 grip, tip;
    private float armRotation;
    private Player.CompositeArmStretchAmount stretch;
    private float poseProgress;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.ownerHitCheck = true; Projectile.timeLeft = 150; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public void Configure(Player player, Item item, int activeSkill, bool accented)
    {
        skill = activeSkill; accent = accented; slot = player.selectedItem; boundItem = item;
        size = Math.Clamp(player.GetAdjustedItemScale(item), .65f, 1.6f); gravity = player.gravDir;
        if (Kind == VanguardSpearKind.Grani && skill == 2) size *= .85f;
        if (Kind == VanguardSpearKind.WildMane && skill == 2) size *= 1.2f;
        duration = Stage == 3 ? VanguardSpearRules.VolleyDuration(Kind)
            : VanguardSpearRules.SwingDuration(Kind, skill, Stage,
                player.GetTotalAttackSpeed(DamageClass.Melee) * 28f / Math.Max(1, item.useTime));
        configured = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(configured); writer.Write((byte)skill); writer.Write((short)duration);
        writer.Write((short)slot); writer.Write(size); writer.Write(gravity); writer.Write(accent);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        configured = reader.ReadBoolean(); skill = Math.Clamp((int)reader.ReadByte(), 0, 3);
        duration = Math.Clamp((int)reader.ReadInt16(), 12, 110); slot = reader.ReadInt16();
        float received = reader.ReadSingle(); size = float.IsFinite(received) ? Math.Clamp(received, .5f, 1.92f) : 1;
        gravity = reader.ReadSingle() < 0 ? -1 : 1; accent = reader.ReadBoolean();
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => configured && Stage < 3 && VanguardSpearRules.Active(poseProgress) ? null : false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!VanguardSpearRules.ValidKind((int)Kind) || Stage < 0 || Stage > 3 || !owner.active || owner.dead || owner.CCed || owner.noItems
            || owner.HeldItem.ModItem is not VanguardSpearWeapon weapon || weapon.Kind != Kind)
        { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (!configured) return; // never simulate a default skill/pose before the initial snapshot arrives
        if (local && (owner.selectedItem != slot || boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)))
        { Projectile.Kill(); return; }
        if (Stage == 3 && local && !Main.mapFullscreen && !Main.blockMouse && !owner.mouseInterface)
        {
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Projectile.velocity);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .001f) { Projectile.velocity = aim; Projectile.netUpdate = true; }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.Center = owner.MountedCenter; Projectile.direction = direction.X < 0 ? -1 : 1;
        owner.ChangeDir(Projectile.direction); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (direction * owner.direction).ToRotation();
        if (Stage == 3)
        {
            int tick = (int)Projectile.ai[2];
            int shot = VanguardSpearRules.ShotAt(tick, Kind);
            float throwPhase = tick < VanguardSpearRules.ThrowWindup ? tick / (float)VanguardSpearRules.ThrowWindup
                : (tick - VanguardSpearRules.ThrowWindup) % VanguardSpearRules.ThrowSpacing / (float)VanguardSpearRules.ThrowSpacing;
            float pull = tick < VanguardSpearRules.ThrowWindup ? MathHelper.Lerp(0, -.85f, throwPhase)
                : MathHelper.Lerp(.3f, -.85f, throwPhase);
            armRotation = direction.ToRotation() - MathHelper.PiOver2 + pull * Projectile.direction * gravity;
            stretch = Player.CompositeArmStretchAmount.Full;
            grip = owner.GetFrontHandPosition(stretch, armRotation);
            tip = grip + direction * VanguardSpearRules.Length(Kind) * size * .78f;
            if (shot >= 0)
            {
                if (!Main.dedServ) SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { Volume = .65f, Pitch = shot * .055f }, grip);
                VanguardSpearVfx.Throw(grip, direction, Kind);
                if (local) Throw(owner, direction, shot);
            }
        }
        else
        {
            if (local) owner.GetModPlayer<VanguardSpearPlayer>().KeepComboAlive();
            poseProgress = Projectile.ai[2] / duration;
            MeleePose(owner, poseProgress, out grip, out tip, out armRotation, out stretch);
            if (VanguardSpearRules.Active(poseProgress))
            {
                if (!soundPlayed)
                {
                    soundPlayed = true;
                    if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item71 with { Volume = .5f, Pitch = Stage == 2 ? -.25f : .15f }, grip);
                }
                Array.Copy(history, 0, history, 1, history.Length - 1); history[0] = tip;
                if ((int)Projectile.ai[2] % 2 == 0) VanguardSpearVfx.Sweep(tip, (tip - grip).SafeNormalize(direction), Kind, Stage);
            }
            else if (poseProgress >= .8f) Array.Clear(history);
        }
        owner.SetCompositeArmFront(true, stretch, armRotation);
        Projectile.ai[2]++;
        if (Projectile.ai[2] >= duration) Projectile.Kill();
    }
    private void MeleePose(Player owner, float progress, out Vector2 hand, out Vector2 head,
        out float rotation, out Player.CompositeArmStretchAmount armStretch)
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        VanguardSpearRules.Pose(owner.MountedCenter, direction, Projectile.direction, gravity, Stage, progress,
            VanguardSpearRules.Length(Kind) * size, out Vector2 a, out Vector2 b);
        Vector2 blade = b - a;
        rotation = blade.ToRotation() - MathHelper.PiOver2;
        armStretch = Stage == 2 && progress < .2f ? Player.CompositeArmStretchAmount.Quarter
            : Stage == 2 && progress >= .8f ? Player.CompositeArmStretchAmount.ThreeQuarters : Player.CompositeArmStretchAmount.Full;
        hand = owner.GetFrontHandPosition(armStretch, rotation);
        head = hand + blade;
    }
    private void Throw(Player owner, Vector2 aim, int shot)
    {
        // Each spear comes from the hand. Collision clips the release point at nearby walls.
        Vector2 forward = grip - owner.MountedCenter + aim * 18;
        Vector2 muzzle = owner.MountedCenter + Collision.TileCollision(owner.MountedCenter - new Vector2(3), forward, 6, 6);
        float spread = (shot % 3 - 1) * .035f;
        Projectile spear = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), muzzle,
            aim.RotatedBy(spread) * VanguardSpearRules.FlightSpeed(Kind) / VanguardSpearRules.FlightUpdates,
            ModContent.ProjectileType<VanguardThrownSpear>(), Projectile.damage, Projectile.knockBack,
            Projectile.owner, (int)Kind, shot == VanguardSpearRules.VolleyCount(Kind) - 1 ? 1 : 0);
        spear.CritChance = Projectile.CritChance;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (CanDamage() == false) return false;
        Player owner = Main.player[Projectile.owner];
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        // Sample the swept blade between this and the previous tick: fast prefixes cannot skip
        // thin targets. Every sample uses the exact same finite pose as the rendered spear.
        MeleePose(owner, Math.Max(.2f, poseProgress - 1f / duration), out _, out Vector2 previousTip, out _, out _);
        int samples = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(previousTip, tip) / 8), 2, 24);
        for (int i = 0; i <= samples; i++)
        {
            float p = Math.Max(.2f, poseProgress - (1 - i / (float)samples) / duration);
            MeleePose(owner, p, out Vector2 a, out Vector2 b, out _, out _);
            float collisionPoint = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, b, 12 * size, ref collisionPoint)) return true;
        }
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Stage >= 3 || target.friendly || target.immortal || target.type == NPCID.TargetDummy || damageDone <= 0) return;
        Vector2 impact = target.Hitbox.ClosestPointInRect(tip);
        VanguardSpearVfx.Impact(impact, (tip - grip).SafeNormalize(Vector2.UnitX), Kind, Stage == 2 || accent);
        if (Projectile.owner != Main.myPlayer) return;
        Main.player[Projectile.owner].GetModPlayer<VanguardSpearPlayer>().Reserve.FillFromMelee();
        if (Kind == VanguardSpearKind.Bagpipe && skill is 2 or 3 && echoTargets++ < 6)
        {
            int echoes = skill == 3 ? 2 : 1;
            for (int i = 0; i < echoes; i++)
            {
                Projectile follow = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), impact, Vector2.Zero,
                    ModContent.ProjectileType<VanguardFollowupStrike>(), Projectile.damage, Projectile.knockBack,
                    Projectile.owner, target.whoAmI, 4 + i * 4, target.type);
                follow.CritChance = Projectile.CritChance;
            }
        }
        if (!hitEffect && Stage == 2)
        {
            hitEffect = true;
            VanguardSpearImpact.Spawn(Projectile, impact, Kind, 0, 34 + (int)Kind * 6);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!configured) return false;
        if (Stage != 3) VanguardSpearVfx.Ribbon(tip, history, Kind, 7 + (int)Kind * 2);
        VanguardSpearVfx.DrawSpear(grip, tip, Kind, lightColor, accent);
        return false;
    }
}
