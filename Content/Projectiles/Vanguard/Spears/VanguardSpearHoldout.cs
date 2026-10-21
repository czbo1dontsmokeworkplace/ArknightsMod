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

// Left-click only. Right-click creates a player-bound lance charge.
public sealed class VanguardSpearHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public VanguardSpearKind Kind => (VanguardSpearKind)(int)Projectile.ai[0];
    private int duration = 28, skill, slot, echoTargets;
    private float size = 1, poseProgress;
    private bool accent, configured, soundPlayed;
    private Item boundItem;
    private Vector2 grip, tip;
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
        size = Math.Clamp(player.GetAdjustedItemScale(item), .65f, 1.6f);
        if (Kind == VanguardSpearKind.Grani && skill == 2) size *= .85f;
        if (Kind == VanguardSpearKind.WildMane && skill == 2) size *= 1.2f;
        duration = VanguardSpearRules.ThrustDuration(Kind, skill,
            player.GetTotalAttackSpeed(DamageClass.Melee) * 28f / Math.Max(1, item.useTime));
        configured = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(configured); writer.Write((byte)skill); writer.Write((short)duration);
        writer.Write((short)slot); writer.Write(size); writer.Write(accent);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        configured = reader.ReadBoolean(); skill = Math.Clamp((int)reader.ReadByte(), 0, 3);
        duration = Math.Clamp((int)reader.ReadInt16(), 11, 55); slot = reader.ReadInt16();
        float received = reader.ReadSingle(); size = float.IsFinite(received) ? Math.Clamp(received, .5f, 1.92f) : 1;
        accent = reader.ReadBoolean();
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => configured && VanguardSpearRules.Active(poseProgress) ? null : false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!VanguardSpearRules.ValidKind((int)Kind) || !owner.active || owner.dead || owner.CCed || owner.noItems
            || owner.HeldItem.ModItem is not VanguardSpearWeapon weapon || weapon.Kind != Kind)
        { Projectile.Kill(); return; }
        if (!configured) return;
        if (Projectile.owner == Main.myPlayer && (owner.selectedItem != slot || boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)))
        { Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.Center = owner.MountedCenter; Projectile.direction = direction.X < 0 ? -1 : 1;
        owner.ChangeDir(Projectile.direction); owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.itemRotation = (direction * owner.direction).ToRotation();
        float arm = direction.ToRotation() - MathHelper.PiOver2;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, arm);
        poseProgress = Projectile.ai[2] / duration;
        MeleePose(owner, poseProgress, out grip, out tip);
        if (VanguardSpearRules.Active(poseProgress) && !soundPlayed)
        {
            soundPlayed = true;
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item1 with { Volume = .45f, Pitch = -.1f }, grip);
        }
        if (++Projectile.ai[2] >= duration) Projectile.Kill();
    }
    private void MeleePose(Player owner, float progress, out Vector2 hand, out Vector2 head)
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 anchor = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
        VanguardSpearRules.ThrustPose(anchor, direction, progress, VanguardSpearRules.Length(Kind) * size, out hand, out head, duration);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (CanDamage() == false) return false;
        Player owner = Main.player[Projectile.owner];
        for (int i = 0; i <= 4; i++)
        {
            float p = Math.Max(.2f, poseProgress - i / (4f * duration));
            MeleePose(owner, p, out Vector2 a, out Vector2 b);
            float point = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, b, 12 * size, ref point)) return true;
        }
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.friendly || target.immortal || damageDone <= 0) return;
        Vector2 impact = target.Hitbox.ClosestPointInRect(tip);
        VanguardSpearVfx.Impact(impact, (tip - grip).SafeNormalize(Vector2.UnitX), Kind, false);
        if (Projectile.owner != Main.myPlayer) return;
        VanguardSpearSpark.Release(Projectile, impact, Kind, false);
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
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (configured) VanguardSpearVfx.DrawSpear(grip, tip, Kind, lightColor, accent);
        return false;
    }
}
