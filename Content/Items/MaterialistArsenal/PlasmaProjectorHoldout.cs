using System;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

// Reuses the former Typhon right-click plasma rendering. Both visible beams
// belong to one damage projectile and stop at the same nearest target.
public sealed class PlasmaProjectorHoldout : ModProjectile
{
    internal const float Range = 1200, Width = 28;
    private const float BeamOffset = 11.3f;
    private readonly float[] scan = new float[3];
    private readonly PhoenixBeamFx[] beams = new PhoenixBeamFx[2];
    private readonly ReaperSwarmVisualNode[] muzzles = new ReaperSwarmVisualNode[2];
    private ReaperSwarmVisualNode contact;
    private Item boundItem;
    private Vector2 start, end;
    private bool blocked, started;
    private int targetIndex = -1;
    private Vector2 Axis => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    private Vector2 Offset(int side) => new Vector2(-Axis.Y, Axis.X) * ((side * 2 - 1) * BeamOffset);
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.LaserRifle}";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.timeLeft = 2;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = PlasmaProjector.ManaInterval;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => targetIndex >= 0 ? null : false;
    public override bool? CanHitNPC(NPC target) => target.whoAmI == targetIndex ? null : false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.silence
            || owner.HeldItem.ModItem is not PlasmaProjector)
        { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && (!owner.channel || boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)))
        { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        // Native item use pays the first interval; only subsequent intervals
        // debit mana here, once on the owning client.
        if (local && ++Projectile.ai[0] >= PlasmaProjector.ManaInterval)
        {
            if (!owner.CheckMana(owner.HeldItem, -1, true)) { Projectile.Kill(); return; }
            Projectile.ai[0] = 0;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerMana, -1, -1, null, owner.whoAmI);
        }
        if (local)
        {
            Vector2 next = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Axis);
            if (Vector2.DistanceSquared(next, Projectile.velocity) > .0001f)
            { Projectile.velocity = next; Projectile.netUpdate = true; }
        }
        Vector2 axis = Axis;
        Projectile.rotation = axis.ToRotation();
        owner.ChangeDir(axis.X < 0 ? -1 : 1);
        Projectile.Center = owner.MountedCenter + axis * 18;
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (axis * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Projectile.timeLeft = 2; owner.manaRegenDelay = owner.maxRegenDelay;
        Trace(owner);
        if (Main.dedServ) return;
        if (!started)
        {
            started = true;
            SoundEngine.PlaySound(SoundID.Item15 with { Volume = .45f, MaxInstances = 2 }, Projectile.Center);
        }
        for (int i = 0; i < 2; i++)
        {
            int side = i;
            if (beams[i] == null)
            {
                var style = PhoenixFx.Original();
                beams[i] = new PhoenixBeamFx("projectile.player.75", () => start + Offset(side),
                    () => end + Offset(side), () => Projectile.active, Range, style);
                muzzles[i] = ReaperSwarmVisuals.Spawn("actor.player.weapon.75.muzzle", () => start + Offset(side),
                    () => Axis, active: () => Projectile.active, style: style);
            }
            beams[i].Extend();
        }
        if (blocked && contact == null)
            contact = ReaperSwarmVisuals.Spawn("actor.player.weapon.75.impact", () => end, () => -Axis,
                active: () => Projectile.active && blocked, style: PhoenixFx.Original());
        else if (!blocked) { contact?.Stop(); contact = null; }
    }
    private void Trace(Player owner)
    {
        Vector2 axis = Axis;
        // Trace from the player's hand, so a barrel beyond a thin wall cannot
        // create a laser on the far side. Both beam edges use the shortest scan.
        Collision.LaserScan(owner.MountedCenter, axis, Width, Range + 48, scan);
        float handLength = MathF.Min(scan[0], MathF.Min(scan[1], scan[2]));
        float muzzleDistance = MathF.Min(48, handLength);
        start = owner.MountedCenter + axis * muzzleDistance;
        float length = MathF.Min(Range, MathF.Max(0, handLength - muzzleDistance));
        blocked = handLength < Range + 47; targetIndex = -1;
        float closest = length;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile)) continue;
            float distance = 0;
            if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size, start,
                start + axis * length, Width, ref distance)) continue;
            if (distance <= closest && (targetIndex < 0 || distance < closest))
            { closest = MathF.Max(0, distance); targetIndex = npc.whoAmI; }
        }
        if (targetIndex >= 0) { length = closest; blocked = true; }
        end = start + axis * length;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Width, ref distance);
    }
    public override void OnKill(int timeLeft)
    {
        foreach (var beam in beams) beam?.Stop();
        foreach (var muzzle in muzzles) muzzle?.Stop();
        contact?.Stop();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation, texture.Size() * .5f, 1,
            Axis.X < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
}
