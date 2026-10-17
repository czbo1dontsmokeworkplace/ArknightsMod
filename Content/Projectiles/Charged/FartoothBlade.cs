using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

// Only the vanilla EmpressBlade art is reused; straight flight has no summon AI or homing.
public sealed class FartoothBlade : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EmpressBlade;
    private float distance;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.extraUpdates = 4;
        Projectile.timeLeft = 300; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        distance += Projectile.velocity.Length();
        if (distance >= ChargedOperatorRules.ShotRange(ChargedOperatorKind.Fartooth, (int)Projectile.ai[0])) Projectile.Kill();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center, 18, ref point);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if ((int)Projectile.ai[0] != 3) return;
        modifiers.ArmorPenetration += 35;
        if (distance > 800) modifiers.SourceDamage *= 1.4f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => ChargedSniperVisuals.HitBurst(target.Hitbox.ClosestPointInRect(Projectile.Center), Projectile.velocity.SafeNormalize(Vector2.UnitX),
            ChargedOperatorKind.Fartooth, true, hit.Crit);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        DrawBlade(texture, Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), Color.White);
        return false;
    }
    public static void DrawBlade(Texture2D texture, Vector2 center, Vector2 aim, Color tint)
    {
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, tint, aim.ToRotation(),
            texture.Size() / 2, .65f, SpriteEffects.None);
    }
}
