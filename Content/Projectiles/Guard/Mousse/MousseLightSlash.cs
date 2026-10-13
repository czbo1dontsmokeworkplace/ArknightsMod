using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Items.Weapons.Guard.Mousse;
using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

// AugerSlash collision/movement reference, credit Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
public sealed class MousseLightSlash : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Mousse/MousseGlove";
    internal const float Reach = 135f * MousseVisuals.RangeMultiplier;
    private bool emitted;

    public override void SetDefaults() {
        Projectile.width = Projectile.height = 150;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 25;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override void AI() {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<MousseGlove>()) {
            Projectile.Kill(); return;
        }
        Projectile.ai[1] = MathHelper.Clamp(Projectile.ai[1], .5f, 3f);
        if (Projectile.velocity.LengthSquared() > .01f) Projectile.velocity *= .45f;
        if (!emitted) {
            emitted = true;
            MousseVisuals.EmitSlash(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX),
                Projectile.ai[1], Projectile.ai[0]);
        }
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, new Vector3(.35f, .32f, .16f) * (Projectile.timeLeft / 25f));
    }
    public override bool? CanDamage() => Projectile.timeLeft > 10;
    public override bool? CanHitNPC(NPC target) => Collision.CanHitLine(Main.player[Projectile.owner].Center,
        1, 1, target.Center, 1, 1) ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        float collisionPoint = 0f;
        float size = Reach * Projectile.ai[1];
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center, Projectile.Center + direction * size, size, ref collisionPoint);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Projectile.ai[2] == 1f) {
            target.AddBuff(ModContent.BuffType<MousseScratch>(), 300);
        }
        if (Main.dedServ || Projectile.numHits >= 4) return;
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 0; i < 8 && ParticleManager.activeParticles.Count < 1500; i++)
            new MousseLightShard(target.Center, direction.RotatedByRandom(.6f) * Main.rand.NextFloat(3f, 7f),
                16, Main.rand.NextFloat(5f, 12f), MousseVisuals.Pale).Spawn();
        for (int i = 0; i < 3; i++) {
            Dust dust = Dust.NewDustPerfect(target.Center, DustID.GoldFlame, direction.RotatedByRandom(.5f) * Main.rand.NextFloat(2f, 4f));
            dust.noGravity = true; dust.scale = .7f;
        }
    }
    public override bool PreDraw(ref Color lightColor) { MousseVisuals.DrawSlash(Projectile); return false; }
}
