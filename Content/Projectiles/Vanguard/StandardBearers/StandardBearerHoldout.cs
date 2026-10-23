using System;
using ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

public sealed class StandardBearerHoldout : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Vanguard/Fang/FangSpear";
    public bool Idle => Projectile.ai[1] < 0;
    private Vector2 grip, tip;
    public static StandardBearerHoldout Find(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is StandardBearerHoldout held) return held;
        return null;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.ownerHitCheck = true; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public void Thrust(float angle)
    {
        if (!Idle) return;
        Projectile.ai[1] = 0; Projectile.ai[2] = angle;
        Array.Clear(Projectile.localNPCImmunity);
        Projectile.netUpdate = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => !Idle && StandardBearerRules.CanStrike(Projectile.ai[1] / StandardBearerRules.AttackFrames)
        && !StandardBearerPlayer.IsPhased(Main.player[Projectile.owner]) ? null : false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems
            || owner.HeldItem.ModItem is not StandardBearerWeapon weapon || (int)weapon.Kind != (int)Projectile.ai[0])
        { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (StandardBearerPlayer.IsPhased(owner)) Projectile.ai[1] = -1;
        float progress = Idle ? 0 : Projectile.ai[1] / StandardBearerRules.AttackFrames;
        StandardBearerRules.Pose(progress, Projectile.ai[2], owner.gravDir, out float angle, out float extension);
        Vector2 aim = angle.ToRotationVector2();
        grip = owner.RotatedRelativePoint(owner.MountedCenter) + new Vector2(10 * owner.direction, -8 * owner.gravDir) + aim * extension;
        tip = grip + aim * StandardBearerRules.Reach;
        Projectile.Center = grip; Projectile.rotation = angle;
        Projectile.damage = owner.GetWeaponDamage(owner.HeldItem);
        owner.heldProj = Projectile.whoAmI;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        if (!Idle)
        {
            if (Projectile.ai[1] == 16 && !Main.dedServ) SoundEngine.PlaySound(SoundID.Item1 with { Volume = .3f }, grip);
            if (++Projectile.ai[1] >= StandardBearerRules.AttackFrames)
            { Projectile.ai[1] = -1; if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true; }
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (CanDamage() == false) return false;
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Vector2.Lerp(grip, tip, .55f), tip, 12, ref point);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        // Existing Fang sprite has a bottom-left shaft and top-right point.
        Vector2 origin = new(12, 43), sourceTip = new(55, 0);
        Vector2 blade = sourceTip - origin;
        Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null,
            lightColor * (StandardBearerPlayer.IsPhased(Main.player[Projectile.owner]) ? .45f : 1),
            Projectile.rotation - blade.ToRotation(), origin, StandardBearerRules.Reach / blade.Length(), SpriteEffects.None);
        return false;
    }
}
