using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

// Skill fields are ranged damage carriers, deliberately separate from ReaperSpin.
public sealed class ReaperBloodOrbit : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private int age, fading;
    private float Radius => Projectile.ai[0] == 0 ? 110 : 76;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 240; Projectile.friendly = true; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.timeLeft = 2; Projectile.penetrate = -1; Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 30; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner]; age++;
        bool holding = owner.active && !owner.dead && owner.selectedItem == (int)Projectile.ai[1] && owner.HeldItem.ModItem is ReaperWeapon w && w.Kind == ReaperKind.Entelechia;
        if (owner.whoAmI == Main.myPlayer && !owner.GetModPlayer<ReaperPlayer>().SuppressAttacks) holding = false;
        if (!holding || fading > 0) { if (++fading >= 18) { Projectile.Kill(); return; } }
        Projectile.timeLeft = 2;
        if (Projectile.ai[0] == 0) Projectile.Center = owner.MountedCenter;
        else if (owner.whoAmI == Main.myPlayer && holding) {
            Vector2 offset = Main.MouseWorld - owner.MountedCenter;
            if (offset.LengthSquared() > 600 * 600) offset = offset.SafeNormalize(Vector2.UnitX) * 600;
            Vector2 next = owner.MountedCenter + offset;
            if (Vector2.DistanceSquared(next, Projectile.Center) > 6 * 6) Projectile.netUpdate = true;
            Projectile.Center = next;
        }
        Projectile.Opacity = Math.Min(1, age / 12f) * (1 - fading / 18f);
    }
    public override bool? CanDamage() => fading == 0 && age > 12 && age % 30 == 0 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float closest = Vector2.Distance(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center));
        float farthest = 0;
        foreach (Vector2 corner in new[]{targetHitbox.TopLeft(),targetHitbox.TopRight(),targetHitbox.BottomLeft(),targetHitbox.BottomRight()}) farthest = Math.Max(farthest, Vector2.Distance(Projectile.Center,corner));
        return closest <= Radius + 16 && farthest >= Radius - 23;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        ReaperMaterial.Crescent(Projectile.Center, age * .085f, Radius, ReaperKind.Entelechia, Projectile.Opacity);
        ReaperMaterial.Crescent(Projectile.Center, age * .085f + MathHelper.Pi, Radius * .94f, ReaperKind.Entelechia, Projectile.Opacity * .65f);
        return false;
    }
}

