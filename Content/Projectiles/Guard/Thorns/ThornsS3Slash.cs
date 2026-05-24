using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;
using Color = Microsoft.Xna.Framework.Color;

namespace ArknightsMod.Content.Projectiles.Guard.Thorns
{
    public class ThornsS3Slash : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/Thorns/ThornsSword";
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<sjds>(), 180);
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
            if (player.Center.X - Main.MouseWorld.X > 0) sx = false; else sx = true;
            if (sx)
            {
                Projectile.ai[0] = 3.1415f * 1.65f;
                t = -(Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero).ToRotation()
                  + Main.rand.Next(-4, 4 + 1) * .1f;
                t2 = +Main.rand.Next(-3, 3 + 1) * .1f;
            }
            else
            {
                Projectile.ai[0] = 3.1415f * 1.15f;
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
            if (sx)
            {
                for (int i = oldVec.Length - 1; i > 0; i--)
                {
                    oldVec[i] = oldVec[i - 1];
                }
                oldVec[0] = new Vector2((float)Math.Sin(Projectile.ai[0] + t + t2), (float)Math.Cos(Projectile.ai[0] + t - t2)) * 60f;
                Projectile.Center = player.Center + new Vector2((float)Math.Sin(Projectile.ai[0] + t + t2), (float)Math.Cos(Projectile.ai[0] + t - t2)) * 60f;
                if (Projectile.ai[0] <= 3.1415f * (2.5f + .5f))
                {
                    Projectile.ai[0] += 0.12f * 30f / player.itemAnimationMax;
                }
                else
                {
                    Projectile.ai[0] += 0.03f * 30f / player.itemAnimationMax;
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
                oldVec[0] = new Vector2((float)Math.Cos(Projectile.ai[0] + t + t2), (float)Math.Sin(Projectile.ai[0] + t - t2)) * 60f;
                Projectile.Center = player.Center + new Vector2((float)Math.Cos(Projectile.ai[0] + t + t2), (float)Math.Sin(Projectile.ai[0] + t - t2)) * 60f;
                if (Projectile.ai[0] <= 3.1415f * 2.5f)
                {
                    Projectile.ai[0] += 0.12f * 30f / player.itemAnimationMax;
                }
                else
                {
                    Projectile.ai[0] += 0.03f * 30f / player.itemAnimationMax;
                    Projectile.ai[2]++;
                    if (Projectile.ai[2] > 11) Projectile.active = false;
                }
            }
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            Texture2D 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_2").Value;
            if (sx) 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_1").Value;
            StripColorFunction stripColor = (x) => new Color(0, 50, 255, 255);
            StripColorFunction stripColor2 = (x) => new Color(255, 200, 0, 255);
            float Cd = 35;
            VertexStrip strip = new VertexStrip();
            VertexStrip strip2 = new VertexStrip();
            VertexStrip strip3 = new VertexStrip();
            VertexStrip strip4 = new VertexStrip();
            VertexStrip strip5 = new VertexStrip();
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
            strip3.PrepareStrip(oldVec,
            rotations.Prepend(rotations.FirstOrDefault()).ToArray(), (x) => new Color(255, 200, 140, 255)
            , (x) => Cd + 19,
            -Main.screenPosition + player.Center);

            strip4.PrepareStrip(oldVec,
            rotations.Prepend(rotations.FirstOrDefault()).ToArray(), (x) => new Color(175, 255, 220, 200)
            , (x) => Cd + 29,
             -Main.screenPosition + player.Center);

            strip5.PrepareStrip(oldVec,
            rotations.Prepend(rotations.FirstOrDefault()).ToArray(), (x) => new Color(75, 155, 120, 100)
            , (x) => Cd + 22,
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.graphics.GraphicsDevice.BlendState = blendStatef2;

            if (oldVec[oldVec.Length - 1] != oldVec[oldVec.Length - 2])
            {
                Main.graphics.GraphicsDevice.Textures[0] = 贴图;
                strip.DrawTrail();
                strip.DrawTrail();
                strip3.DrawTrail();
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                strip2.DrawTrail();
                Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_4").Value;
                if (sx) Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_3").Value;
                strip3.DrawTrail();
                Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_6").Value;
                if (sx) Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_5").Value;
                strip4.DrawTrail();
                strip5.DrawTrail();
				Texture2D texture = TextureAssets.Projectile[Type].Value;
				Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                float jd = (oldVec[0]).ToRotation();
                if (sx)
                {
                    Main.spriteBatch.Draw(texture, player.Center - Main.screenPosition + new Vector2(0, 5),
                        null, Color.AliceBlue, jd + 3.14f / 3.5f, new Vector2(10, texture.Size().Y - 10), Vector2.Distance(new Vector2(0), oldVec[0]) / 55, SpriteEffects.None, 0);
                }
                else
                {
                    Main.spriteBatch.Draw(texture, player.Center - Main.screenPosition + new Vector2(0, 5),
                        null, Color.AliceBlue, jd - 3.14f - 3.14f / 3.5f, texture.Size() + new Vector2(-10), Vector2.Distance(new Vector2(0), oldVec[0]) / 55, SpriteEffects.FlipHorizontally, 0);
                }
            }
            return false;
        }
    }
}
