using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Items.Weapons.Guard.Chen;

// Independent hit and visual carrier: never changes item timers, arms or heldProj.
public sealed class ChenSkillTwoOverlay : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Chen/Skill_1_Release_Effect_2";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.timeLeft = 18;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 13;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/ChenSwordSkill2") { MaxInstances = 2 }, Projectile.Center);
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool? Colliding(Rectangle a, Rectangle b)
    {
        float at = 0;
        return Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 580, 100, ref at);
    }
    public override void OnKill(int timeLeft) => ChenSwordRules.EndSkill(Main.player[Projectile.owner], 1);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        float t = 1 - Projectile.timeLeft / 18f;
        Main.EntitySpriteDraw(texture, Projectile.Center + Projectile.velocity * (110 + 160 * t) - Main.screenPosition,
            null, new Color(255, 110, 80, 0) * (1 - t), Projectile.rotation,
            texture.Size() * .5f, new Vector2(2, .8f), SpriteEffects.None);
        return false;
    }
}
