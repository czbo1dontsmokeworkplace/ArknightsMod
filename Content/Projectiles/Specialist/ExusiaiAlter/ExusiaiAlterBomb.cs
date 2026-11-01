using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.ExusiaiAlter;

// One immediate physical splash, not a homing projectile repeatedly dealing contact damage.
public class ExusiaiAlterBomb : ModProjectile
{
    public const float Radius = 96f;
    public override string Texture => "ArknightsMod/Assets/null";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = (int)Radius * 2;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft == 2;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        targetHitbox.Distance(Projectile.Center) <= Radius;
    public override bool? CanHitNPC(NPC target) => target.friendly ? false : null;
    public override bool CanHitPvp(Player target) => false;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DisableCrit();
    public override void AI() {
        // A caster held by the wearer must not convert this physical armor proc into Arts damage.
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = false;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
        if (Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        for (int i = 0; i < 40; i++) {
            Vector2 direction = (MathHelper.TwoPi * i / 40f).ToRotationVector2();
            Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * Radius, DustID.GoldFlame,
                direction * 3f, 80, default, 1.3f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
