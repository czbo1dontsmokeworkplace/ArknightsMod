using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Players;

namespace ArknightsMod.Content.Projectiles.Sniper.Schwarz
{
    public class SchwarzArrow : ModProjectile
    {
        Player player => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.friendly = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
            if (Main.myPlayer == player.whoAmI) {
                if (modPlayer.SkillActive == true){
                    if (modPlayer.Skill == 0 && Main.rand.NextFloat() < 0.8f) {
                        target.AddBuff(36, 300);
                    }
                    else if (modPlayer.Skill == 1 && Main.rand.NextFloat() < 0.5f) {
                        target.AddBuff(36, 300);
                    }
                    else if (modPlayer.Skill == 2 ) {
                        target.AddBuff(36, 300);
                    }
                    // 这里要黑命中时的纹理图暂时没有
                }
                else {
                    if (Main.rand.NextFloat() < 0.2f)
					    target.AddBuff(36, 300);
                }
			}

        }
        

        public override void AI()
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(5))
            {
                Vector2 spawnPos = Projectile.Center
                    - Projectile.velocity * Main.rand.NextFloat(0.2f, 0.6f);

                int stripe = Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    spawnPos,
                    Projectile.velocity * 0.5f,
                    ModContent.ProjectileType<TrailStripe>(),
                    0,
                    0,
                    Projectile.owner
                );

                Projectile p = Main.projectile[stripe];
                p.rotation = Projectile.velocity.ToRotation();
                p.ai[0] = Main.rand.NextBool() ? 0 : 1; // 0=白 1=黑
            }
            base.AI();
        }
    }

    public class TrailStripe : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 2;
            Projectile.timeLeft = 60;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.95f;
            Projectile.alpha += 3;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.MagicPixel.Value;

            Color color = Projectile.ai[0] == 0
                ? Color.White
                : Color.Black;

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                new Rectangle(0,0,2,2),
                color * (1f - Projectile.alpha / 255f),
                Projectile.rotation,
                new Vector2(0f),
                new Vector2(6f, 2f),
                SpriteEffects.None,
                0f
            );  

            return false;
        }
    }
}
