using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;
using Color = Microsoft.Xna.Framework.Color;

namespace ArknightsMod.Content.Projectiles.Guard.SilverAsh
{
    public class SilverAshS3Blade : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/SilverAsh/SilverAshWeapon2";
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.NewProjectile(player.GetSource_Death(), target.Center
                 , Projectile.velocity.SafeNormalize(Vector2.Zero), ModContent.ProjectileType<SilverAshSlashEffect>(), 0, 0, Main.myPlayer);
        }
        public override void SetDefaults()
        {
            Projectile.extraUpdates = 1;
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.scale = 1f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 6000;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.idStaticNPCHitCooldown = 60;
            Projectile.drawLayer = ProjectileDrawLayerID.OverPlayers;
        }

        public Vector2[] oldVec = new Vector2[14];
        float t = 0;
        float t2 = 0;
        bool sx = true;
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            if (player.Center.X - Main.MouseWorld.X < 0) sx = false; else sx = true;
            if (sx)
            {
                Projectile.ai[0] = 3.1415f * 1.75f;
                t = -(Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero).ToRotation()
                  + Main.rand.Next(-4, 4 + 1) * .1f;
                t2 = +Main.rand.Next(-3, 3 + 1) * .1f;
            }
            else
            {
                Projectile.ai[0] = 3.1415f * 1.25f;
                t = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero).ToRotation()
                  + Main.rand.Next(-4, 4 + 1) * .1f;
                t2 = +Main.rand.Next(-3, 3 + 1) * .1f;
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            float rotaion = (oldVec[0]).ToRotation() - (Main.MouseWorld.X < player.Center.X ? 3.1415f : 0);
            player.itemRotation = rotaion;
            player.direction = Main.MouseWorld.X < player.Center.X ? -1 : 1;
            Projectile.timeLeft = 2;
            player.itemTime = 2;
            player.itemAnimation = 2;
            if (sx)
            {
                for (int i = oldVec.Length - 1; i > 0; i--)
                {
                    oldVec[i] = oldVec[i - 1];
                }
                oldVec[0] = new Vector2((float)Math.Sin(Projectile.ai[0] + t + t2), (float)Math.Cos(Projectile.ai[0] + t - t2)) * 20f;
                Projectile.Center = player.Center + new Vector2((float)Math.Sin(Projectile.ai[0] + t + t2), (float)Math.Cos(Projectile.ai[0] + t - t2)) * 20f;
                if (Projectile.ai[0] <= 3.1415f * (2.5f + .5f))
                {
                    Projectile.ai[0] += 0.12f;
                }
                else
                {
                    Projectile.ai[0] += 0.03f;
                    Projectile.ai[2]++;
                    if (Projectile.ai[2] > 11) Projectile.active = false;
                }
            }
            else
            {
                for (int i = oldVec.Length - 1; i > 0; i--)
                {
                    oldVec[i] = oldVec[i - 1];
                }
                oldVec[0] = new Vector2((float)Math.Cos(Projectile.ai[0] + t + t2), (float)Math.Sin(Projectile.ai[0] + t - t2)) * 20f;
                Projectile.Center = player.Center + new Vector2((float)Math.Cos(Projectile.ai[0] + t + t2), (float)Math.Sin(Projectile.ai[0] + t - t2)) * 20f;
                if (Projectile.ai[0] <= 3.1415f * 2.5f)
                {
                    Projectile.ai[0] += 0.12f;
                }
                else
                {
                    Projectile.ai[0] += 0.03f;
                    Projectile.ai[2]++;
                    if (Projectile.ai[2] > 11) Projectile.active = false;
                }
            }
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            Texture2D 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_B").Value;
            if (sx) 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_A").Value;
            StripColorFunction stripColor = (x) => new Color(151, 151, 151, 105);
            StripColorFunction stripColor2 = (x) => new Color(151, 151, 151, 125);
            float Cd = 15;
            VertexStrip strip = new VertexStrip();
            VertexStrip strip2 = new VertexStrip();
            Vector2 wz = Projectile.Center;

            var rotations = oldVec.Zip(oldVec.Skip(1), (a, b) => a - b).Select((a) => a.ToRotation());
            strip.PrepareStrip(
                oldVec,
                rotations.Prepend(rotations.FirstOrDefault()).ToArray(), stripColor
                ,
                (x) => Cd + 18,
                -Main.screenPosition + player.Center
                );
            strip2.PrepareStrip(oldVec,
               rotations.Prepend(rotations.FirstOrDefault()).ToArray(), stripColor2
               , (x) => Cd + 26,
               -Main.screenPosition + player.Center);
            BlendState blendStatef = new BlendState()
            {
                AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction,
                AlphaDestinationBlend = BlendState.AlphaBlend.AlphaDestinationBlend,
                AlphaSourceBlend = BlendState.AlphaBlend.AlphaSourceBlend,
                ColorBlendFunction = (BlendFunction)0,
                ColorDestinationBlend = (Blend)5,
                ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
                ColorWriteChannels = ColorWriteChannels.All,
                ColorWriteChannels1 = ColorWriteChannels.All,
                ColorWriteChannels2 = ColorWriteChannels.All,
                ColorWriteChannels3 = ColorWriteChannels.All,
                BlendFactor = Color.White,
                MultiSampleMask = -1
            };
            BlendState blendStatef2 = new BlendState()
            {
                AlphaBlendFunction = BlendState.Additive.AlphaBlendFunction,
                AlphaDestinationBlend = BlendState.Additive.AlphaDestinationBlend,
                AlphaSourceBlend = BlendState.Additive.AlphaSourceBlend,
                ColorBlendFunction = BlendFunction.ReverseSubtract,
                ColorDestinationBlend = BlendState.Additive.ColorDestinationBlend,
                ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
                ColorWriteChannels = ColorWriteChannels.All,
                ColorWriteChannels1 = ColorWriteChannels.All,
                ColorWriteChannels2 = ColorWriteChannels.All,
                ColorWriteChannels3 = ColorWriteChannels.All,
                BlendFactor = Color.White,
                MultiSampleMask = -1
            };


            if (oldVec[oldVec.Length - 1] != oldVec[oldVec.Length - 2])
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                Main.graphics.GraphicsDevice.BlendState = blendStatef2;

                Main.graphics.GraphicsDevice.Textures[0] = 贴图;
                strip.DrawTrail();
                strip.DrawTrail();
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                strip2.DrawTrail();
                Texture2D 贴图1 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Guard/SilverAsh/SilverAshWeapon3").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                float jd = (oldVec[0]).ToRotation();
                if (sx)
                {
                    Main.spriteBatch.Draw(贴图1, player.Center - Main.screenPosition + new Vector2(0, 5),
                        null, Color.AliceBlue, jd + 3.14f / 3.5f, new Vector2(3, 贴图1.Size().Y - 3), 1f, SpriteEffects.None, 0);
                }
                else
                {
                    Main.spriteBatch.Draw(贴图1, player.Center - Main.screenPosition + new Vector2(0, 5),
                        null, Color.AliceBlue, jd - 3.14f - 3.14f / 3.5f, 贴图1.Size() + new Vector2(-3), 1f, SpriteEffects.FlipHorizontally, 0);
                }
            }
            return false;
        }
    }
}
