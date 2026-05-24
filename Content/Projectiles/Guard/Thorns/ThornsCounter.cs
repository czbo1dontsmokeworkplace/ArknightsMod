using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;
using Color = Microsoft.Xna.Framework.Color;

namespace ArknightsMod.Content.Projectiles.Guard.Thorns
{
    public class ThornsCounter : ModProjectile
    {
        public override string Texture => "ArknightsMod/Content/Projectiles/Guard/Thorns/ThornsSword";
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<sjds2>(), 180);
        }
        public override void SetDefaults()
        {
            Projectile.extraUpdates = 1;
            Projectile.width = 300;
            Projectile.height = 300;
            Projectile.scale = 1f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.idStaticNPCHitCooldown = 60;
            Projectile.drawLayer = ProjectileDrawLayerID.OverPlayers;
        }

        public Vector2[] oldVec = new Vector2[40];
        float t = 0;
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 4.5f;
            Player player = Main.player[Projectile.owner];
            t = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero).ToRotation();
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float ndjd = .25f;
            Projectile.ai[1] += 2.5f;
            Projectile.ai[0] = 3.14f / 2f;
            for (int gg = 0; gg < oldVec.Length; gg++)
            {
                for (int i = oldVec.Length - 1; i > 0; i--)
                {
                    oldVec[i] = oldVec[i - 1];
                }
                oldVec[0] = (new Vector2((float)Math.Sin(Projectile.ai[0] + ndjd),
                    (float)Math.Cos(Projectile.ai[0] - ndjd)) * Projectile.ai[1])
                   .RotatedBy(t + 3.14 * .75f);
                Projectile.ai[0] += 0.12f;
            }
            if (Projectile.ai[1] > 100) Projectile.active = false;

        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            Texture2D 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_8").Value;
            StripColorFunction stripColor = (x) => new Color(255 - (int)(Projectile.ai[1] * 1.275f * 2), 200 - (int)(Projectile.ai[1] * 2), 0, 255 - (int)(Projectile.ai[1] * 1.275f * 2));
            StripColorFunction stripColor2 = (x) => new Color(0, 55, 255, 255 - (int)(Projectile.ai[1] * 1.275f * 2));
            float Cd = 80;
            VertexStrip strip = new VertexStrip();
            VertexStrip strip2 = new VertexStrip();
            VertexStrip strip3 = new VertexStrip();
            Vector2 wz = Projectile.Center;

            var rotations = oldVec.Zip(oldVec.Skip(1), (a, b) => a - b).Select((a) => a.ToRotation());
            strip.PrepareStrip(
                oldVec,
                rotations.Prepend(rotations.FirstOrDefault()).ToArray(), stripColor, (x) => Projectile.ai[1] * 2 < 80 ? Projectile.ai[1] * 2 : 80,
                -Main.screenPosition + Projectile.Center - Projectile.velocity * 20);

            strip2.PrepareStrip(
               oldVec,
               rotations.Prepend(rotations.FirstOrDefault()).ToArray(), stripColor2, (x) => Projectile.ai[1] * 2 < 80 ? Projectile.ai[1] * 2 : 80,
               -Main.screenPosition + Projectile.Center - Projectile.velocity * 20);
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

            Color color = new Color(255 - (int)(Projectile.ai[1] * 1.275f * 1.4f), 200 - (int)(Projectile.ai[1] * 1.4f), 0, 255 - (int)(Projectile.ai[1] * 1.275f * 1.4f));
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.graphics.GraphicsDevice.BlendState = blendStatef2;
            Main.graphics.GraphicsDevice.Textures[0] = 贴图;
            strip2.DrawTrail();
            strip2.DrawTrail();
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            strip.DrawTrail();
            strip.DrawTrail();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int ff = 0; ff < 5; ff++)
            {
				Texture2D texture = TextureAssets.Projectile[Type].Value;
				Main.spriteBatch.Draw(texture, oldVec[oldVec.Length / 7 * (ff + 2)] * 2 - Main.screenPosition + Projectile.Center - Projectile.velocity * 20
                    , null, color, oldVec[oldVec.Length / 7 * (ff + 2)].ToRotation() + 3.14f
                , texture.Size() / 2f, Projectile.ai[1] / 70f, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
