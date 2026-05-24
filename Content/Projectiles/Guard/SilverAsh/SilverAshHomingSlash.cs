using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.SilverAsh
{
    public class SilverAshHomingSlash : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/SilverAsh/SilverAshWeapon2";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.extraUpdates = 1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 160;
            Projectile.penetrate = 1;
            Projectile.alpha = 255;
            Projectile.light = 0.5f;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        }
        int gg = 0;
        bool zz = false;
        bool sss = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.NewProjectile(player.GetSource_Death(), Projectile.Center
                 , Projectile.velocity.SafeNormalize(Vector2.Zero), ModContent.ProjectileType<SilverAshSlashEffect>(), 0, 0, Main.myPlayer);
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {

            Vector2 vector = Projectile.Center - Projectile.oldPos[0] + Projectile.velocity * 3f;
            Texture2D 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/ex24").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);


            Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/ex26").Value;
            VertexStrip strip = new VertexStrip();
            var rotations = Projectile.oldPos.Zip(Projectile.oldPos.Skip(1), (a, b) => a - b).Select((a) => a.ToRotation());
            strip.PrepareStrip(
                Projectile.oldPos,
                rotations.Prepend(rotations.FirstOrDefault()).ToArray(),
                (x) => new Color(151, 151, 201, 255),
                (x) => 6 * (Projectile.ai[0] + 1),
                -Main.screenPosition + new Vector2(Projectile.width, Projectile.height) / 2);
            strip.DrawTrail();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

            Main.spriteBatch.Draw(贴图, Projectile.Center - Main.screenPosition - Projectile.velocity.SafeNormalize(Vector2.Zero) * 2f,
        null, new Color(155, 155, 205, 255), Projectile.rotation, 贴图.Size() / 2f,
        new Vector2(1.5f, 3.5f) / 1.7f * (Projectile.ai[0] + 1)
        , SpriteEffects.None, 0);

            Main.spriteBatch.Draw(贴图, Projectile.Center - Main.screenPosition - Projectile.velocity.SafeNormalize(Vector2.Zero) * 2f,
             null, new Color(205, 205, 255, 205), Projectile.rotation, 贴图.Size() / 2f,
             new Vector2(1.5f, 3.5f) / 2f * (Projectile.ai[0] + 1)
             , SpriteEffects.None, 0);

            return false;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + 3.14f / 2f;

            NPC target = null;
            float distanceMax = 300f;
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly)
                {
                    float currentDistance = Vector2.Distance(npc.Center, Projectile.Center) + Vector2.Distance(npc.Size, new Vector2(0)) / 2f;
                    if (currentDistance < distanceMax)
                    {
                        distanceMax = currentDistance;
                        target = npc;
                    }
                }
            }

            if (target != null)
            {
                Vector2 targetVec = target.Center - Projectile.Center;
                targetVec.Normalize();
                targetVec *= 18f;
                Projectile.velocity = (Projectile.velocity * 10f + targetVec) / 11f;
            }
        }
        public override void OnKill(int timeLeft)
        {

        }
    }
}
