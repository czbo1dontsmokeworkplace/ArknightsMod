using ArknightsMod.Content.Projectiles.Guard.Mousse;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.ArtsFighter;

// AugerSlash movement / geometry reference, credit Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
public abstract class ArtsFighterSlash : ModProjectile
{
    public override string Texture => MousseMaterials.Root + "MousseCrescent";
    protected abstract int WeaponType { get; }
    protected abstract Color SlashColor { get; }
    protected virtual bool StarDetails => false;
    private int age;
    public override void SetDefaults()
    {
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
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.type != WeaponType) { Projectile.Kill(); return; }
        Projectile.ai[1] = MathHelper.Clamp(Projectile.ai[1], .5f, 3f);
        if (Projectile.velocity.LengthSquared() > .01f) Projectile.velocity *= .45f;
        if (age == 1) MousseVisuals.EmitSlash(Projectile, SlashColor, StarDetails);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, SlashColor.ToVector3() * (.7f * Projectile.ai[1]));
        age++;
    }
    public override bool? CanDamage() => Projectile.timeLeft > 10;
    public override bool? CanHitNPC(NPC target) => Collision.CanHitLine(Main.player[Projectile.owner].Center,
        1, 1, target.Center, 1, 1) ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0f;
        float reach = 135f * MousseVisuals.RangeMultiplier * Projectile.ai[1];
        Vector2 start = age < 2 ? Main.player[Projectile.owner].Center : Projectile.Center;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            start, start + Projectile.velocity.SafeNormalize(Vector2.UnitX) * reach, reach, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.numHits < 4) MousseVisuals.EmitHit(target.Center,
            Projectile.velocity.SafeNormalize(Vector2.UnitX), SlashColor, Projectile.numHits, StarDetails);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MousseVisuals.DrawSlash(Projectile, age, SlashColor); return false;
    }
}
