using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.SilverAsh
{
    public class SilverAshSlashEffect : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/SilverAsh/SilverAshWeapon2";
        public override void SetDefaults()
        {
            Projectile.extraUpdates = 1;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.scale = 1f;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 20;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.light = 0.5f;
            Projectile.drawLayer = ProjectileDrawLayerID.OverPlayers;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.ai[0] = Projectile.timeLeft;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + 3.14f / 2f;
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            Texture2D 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/ex24").Value;
            int d = (int)(200f / Projectile.ai[0] * Projectile.timeLeft);
            Main.spriteBatch.Draw(贴图, Projectile.Center - Main.screenPosition - Projectile.velocity.SafeNormalize(Vector2.Zero) * 2f,
        null, new Color(d + 30, d + 30, d + 55, d / 4 + 200), Projectile.rotation, 贴图.Size() / 2f,
        new Vector2(1.5f / Projectile.ai[0] * Projectile.timeLeft, 2.5f) / 1f * (Projectile.ai[1] + 1)
        , SpriteEffects.None, 0);

            Main.spriteBatch.Draw(贴图, Projectile.Center - Main.screenPosition - Projectile.velocity.SafeNormalize(Vector2.Zero) * 2f,
             null, new Color(d + 30, d + 30, d + 55, 180), Projectile.rotation, 贴图.Size() / 2f,
             new Vector2(1.5f / Projectile.ai[0] * Projectile.timeLeft, 2.5f) / 1.5f * (Projectile.ai[1] + 1)
             , SpriteEffects.None, 0);
            return true;
        }
    }
}
