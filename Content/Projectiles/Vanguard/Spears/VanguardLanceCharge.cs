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

// A held lance and swept hitbox. The owning player's native movement resolves tiles;
// remote peers only follow the synchronized player and never move another player.
public sealed class VanguardLanceCharge : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public VanguardSpearKind Kind => (VanguardSpearKind)(int)Projectile.ai[0];
    private int slot, pulseCount;
    private bool configured, accent, started, stopped, impactSpent, movementEnded, moved;
    private float size = 1;
    private Item boundItem;
    private Vector2 grip, tip, previousGrip, previousTip, previousCenter, start;
    private readonly Vector2[] history = new Vector2[24];
    internal bool ProtectsContact => Projectile.active && started && moved && !movementEnded && configured
        && Projectile.ai[1] >= 1 && Projectile.ai[1] <= VanguardSpearRules.ChargeDuration + 1
        && (Projectile.owner != Main.myPlayer || Main.player[Projectile.owner].GetModPlayer<VanguardSpearPlayer>().ChargeHeld);
    private Vector2 Aim => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.timeLeft = 90; Projectile.netImportant = true; Projectile.ownerHitCheck = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public void Configure(Player player, Item item, bool accented)
    {
        slot = player.selectedItem; boundItem = item; accent = accented;
        size = Math.Clamp(player.GetAdjustedItemScale(item), .65f, 1.6f);
        configured = true;
        player.GetModPlayer<VanguardSpearPlayer>().ActiveCharge = this;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(configured); writer.Write((short)slot); writer.Write(size);
        writer.Write(accent); writer.Write(stopped);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        configured = reader.ReadBoolean(); slot = Math.Clamp((int)reader.ReadInt16(), 0, 58);
        float scale = reader.ReadSingle(); size = float.IsFinite(scale) ? Math.Clamp(scale, .65f, 1.6f) : 1;
        accent = reader.ReadBoolean(); stopped = reader.ReadBoolean();
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.active && !movementEnded && configured && started && Projectile.ai[1] > 1
        && Projectile.ai[1] <= VanguardSpearRules.ChargeDuration + 1 ? null : false;
    private bool ValidOwner(Player owner) => owner.active && !owner.dead && !owner.CCed && !owner.noItems
        && !owner.mount.Active && owner.grapCount == 0 && owner.selectedItem == slot
        && owner.HeldItem.ModItem is VanguardSpearWeapon weapon && weapon.Kind == Kind
        && (Projectile.owner != Main.myPlayer || boundItem == null || ReferenceEquals(boundItem, owner.HeldItem));

    // Called before native player movement, after movement accessories have updated.
    internal void ApplyMovement(Player owner)
    {
        if (Projectile.owner != Main.myPlayer) return;
        if (!Projectile.active || !configured || !ValidOwner(owner) || stopped
            || Projectile.ai[1] > VanguardSpearRules.ChargeDuration
            || !owner.GetModPlayer<VanguardSpearPlayer>().ChargeHeld)
        {
            EndMovement(owner);
            if (Projectile.active) Projectile.Kill();
            return;
        }
        if (Projectile.ai[1] < 1) return;
        Vector2 previousAim = Aim;
        Vector2 cursorAim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(previousAim);
        Projectile.velocity = previousAim.ToRotation().AngleTowards(cursorAim.ToRotation(),
            VanguardSpearRules.ChargeTurnRate).ToRotationVector2();
        bool turning = Vector2.DistanceSquared(previousAim, Aim) > .000001f;
        Vector2 requested = Aim * VanguardSpearRules.ChargeSpeed(Kind);
        Vector2 safe = Collision.TileCollision(owner.position, requested, owner.width, owner.height, false, false, (int)owner.gravDir);
        // Never slide around a wall at charge speed or teleport to the mouse.
        stopped = Vector2.DistanceSquared(safe, requested) > .25f;
        if (stopped || turning || (int)Projectile.ai[1] % 6 == 1) Projectile.netUpdate = true;
        owner.velocity = safe;
        moved = safe.LengthSquared() > .01f;
        if (Main.netMode == NetmodeID.MultiplayerClient && (turning || (int)Projectile.ai[1] % 6 == 1))
            NetMessage.SendData(MessageID.PlayerControls, number: owner.whoAmI);
        owner.fallStart = (int)(owner.position.Y / 16f);
        // Avoid contact damage while passing through an enemy; no immunity is granted at rest.
        owner.ChangeDir(Aim.X < 0 ? -1 : 1);
    }
    private void EndMovement(Player owner)
    {
        if (movementEnded) return;
        movementEnded = true;
        if (Projectile.owner == Main.myPlayer)
        {
            if (moved)
            {
                owner.velocity *= .1f;
                float speed = owner.velocity.Length();
                if (speed > VanguardSpearRules.ChargeEndSpeed)
                    owner.velocity *= VanguardSpearRules.ChargeEndSpeed / speed;
            }
            if (owner.heldProj == Projectile.whoAmI)
            {
                owner.heldProj = -1;
                owner.itemTime = owner.itemAnimation = 0;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerControls, number: owner.whoAmI);
        }
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!VanguardSpearRules.ValidKind((int)Kind) || !float.IsFinite(Projectile.velocity.X)
            || !float.IsFinite(Projectile.velocity.Y)) { Projectile.Kill(); return; }
        if (!configured) return;
        if (!ValidOwner(owner) || movementEnded || Projectile.ai[1] > VanguardSpearRules.ChargeDuration
            || (Projectile.owner == Main.myPlayer && !owner.GetModPlayer<VanguardSpearPlayer>().ChargeHeld)
            || (started && Vector2.DistanceSquared(owner.MountedCenter, start) > 1100 * 1100))
        { Projectile.Kill(); return; }
        if (!started)
        {
            started = true; start = previousCenter = owner.MountedCenter;
            Array.Fill(history, owner.MountedCenter);
            VanguardSpearVfx.ChargeStart(owner.MountedCenter, Aim, Kind);
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { Volume = .8f, Pitch = -.35f }, owner.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                VanguardChargePulse.Spawn(Projectile, owner.MountedCenter, false);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.PlayerControls, number: owner.whoAmI);
            }
        }
        owner.GetModPlayer<VanguardSpearPlayer>().ActiveCharge = this;
        previousCenter = Projectile.Center;
        Projectile.Center = owner.MountedCenter;
        if (Projectile.ai[1] == 0) previousCenter = Projectile.Center;
        owner.ChangeDir(Aim.X < 0 ? -1 : 1); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (Aim * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Aim.ToRotation() - MathHelper.PiOver2);
        Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Aim.ToRotation() - MathHelper.PiOver2);
        previousGrip = grip; previousTip = tip;
        grip = hand + Aim * 8;
        tip = grip + Aim * VanguardSpearRules.Length(Kind) * size * .78f;
        if (Projectile.ai[1] == 0) { previousGrip = grip; previousTip = tip; }
        Array.Copy(history, 0, history, 1, history.Length - 1); history[0] = Projectile.Center;
        if (Projectile.ai[1] % 2 == 0 && Projectile.ai[1] > 0)
            VanguardSpearVfx.ChargeWake(Vector2.Lerp(previousCenter, Projectile.Center, .5f), Aim, Kind, (int)Projectile.ai[1]);
        // Let the current partial tile-limited segment resolve hits before stopping next tick.
        if (stopped) Projectile.ai[1] = VanguardSpearRules.ChargeDuration;
        Projectile.ai[1]++;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (CanDamage() == false) return false;
        Vector2 travel = Projectile.Center - previousCenter;
        if (travel.LengthSquared() > 96 * 96) return false; // discontinuous network/teleport sample
        float point = 0;
        // Player sweep catches enemies actually crossed; lance sweep uses the same tip as drawing.
        if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousCenter,
            Projectile.Center, 24, ref point)) return true;
        for (int i = 0; i <= 3; i++)
        {
            float progress = i / 3f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Vector2.Lerp(grip, previousGrip, progress), Vector2.Lerp(tip, previousTip, progress),
                12 * size, ref point)) return true;
        }
        return false;
    }
    public override bool? CanHitNPC(NPC target) => target.friendly ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.friendly || target.immortal || damageDone <= 0) return;
        Vector2 point = target.Hitbox.ClosestPointInRect(Projectile.Center);
        VanguardSpearVfx.Impact(point, Aim, Kind, pulseCount < 6);
        if (Projectile.owner != Main.myPlayer) return;
        // Preserve the old heavy-throw signature at crossed targets, with a per-charge visual cap.
        if (pulseCount++ < 6) VanguardChargePulse.Spawn(Projectile, point, true);
        if (impactSpent) return;
        impactSpent = true;
        VanguardSpearSpark.Release(Projectile, point, Kind, true);
        float radius = Kind == VanguardSpearKind.Bagpipe ? 76 : 40;
        int blast = Kind == VanguardSpearKind.Bagpipe ? Projectile.damage
            : Kind == VanguardSpearKind.Vigna ? Projectile.damage / 2 : 0;
        if (blast > 0) VanguardSpearImpact.Spawn(Projectile, point, Kind, blast, radius);
    }
    public override void OnKill(int timeLeft)
    {
        Player owner = Main.player[Projectile.owner];
        if (started) EndMovement(owner);
        var state = owner.GetModPlayer<VanguardSpearPlayer>();
        if (ReferenceEquals(state.ActiveCharge, this)) state.ActiveCharge = null;
        if (started) VanguardSpearVfx.Fizzle(Projectile.Center, Kind);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!configured || !started) return false;
        VanguardChargeTrail.Draw(Projectile.Center, history, Kind, Projectile.ai[1]);
        VanguardSpearVfx.DrawSpear(grip, tip, Kind, lightColor, accent, true, Projectile.ai[1]);
        return false;
    }
}
