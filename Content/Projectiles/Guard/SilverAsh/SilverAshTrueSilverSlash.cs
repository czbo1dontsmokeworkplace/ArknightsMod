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
    public class SilverAshTrueSilverSlash : ModProjectile
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
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.scale = 1f;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.idStaticNPCHitCooldown = 60;
            Projectile.drawLayer = ProjectileDrawLayerID.OverPlayers;
        }

        public Vector2[] oldVec = new Vector2[30];
        float t = 0;
        Vector2 csd = new Vector2(0);
        public override void OnSpawn(IEntitySource source)
        {
            csd = Projectile.Center;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 7.5f;
            Player player = Main.player[Projectile.owner];
            t = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.Zero).ToRotation();
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            player.itemTime = 2;
            player.itemAnimation = 2;
            float ndjd = 0;
            csd += Projectile.velocity;
            Projectile.Center = csd;
            Projectile.ai[1] += 2.8f;
            Projectile.ai[0] = 3.14f / 2f + 3.14f / 4f;
            if (Projectile.width < 800)
                Projectile.width =
                Projectile.height += 20;

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
            if (Projectile.ai[1] > 40) Projectile.friendly = true;
            if (Projectile.ai[1] > 140) Projectile.active = false;
        }
        public override bool PreDraw(Player player, ref Color lightColor)
        {
            Texture2D 贴图 = ModContent.Request<Texture2D>("ArknightsMod/Assets/Textures/SlashEffect/Extra_8").Value;
            StripColorFunction stripColor = (x) =>
            new Color(205 - (int)(Projectile.ai[1] * Projectile.ai[1]) / 100
            , 205 - (int)(Projectile.ai[1] * Projectile.ai[1]) / 100
            , 255 - (int)((Projectile.ai[1] + 10) * (Projectile.ai[1] + 10)) / 100
            , 170 - (int)(Projectile.ai[1] * Projectile.ai[1]) / 100);

            StripColorFunction stripColor2 = (x) =>
            new Color(205 - (int)((Projectile.ai[1] + 10) * (Projectile.ai[1] + 10)) / 100
            , 205 - (int)(Projectile.ai[1] * Projectile.ai[1]) / 100
            , 255 - (int)(Projectile.ai[1] * Projectile.ai[1]) / 100
          , 170 - (int)(Projectile.ai[1] * Projectile.ai[1]) / 100);

            float Cd = 80;
            VertexStrip strip = new VertexStrip();
            VertexStrip strip2 = new VertexStrip();
            VertexStrip strip3 = new VertexStrip();
            Vector2 wz = Projectile.Center;

            var rotations = oldVec.Zip(oldVec.Skip(1), (a, b) => a - b).Select((a) => a.ToRotation());
            strip.PrepareStrip(
                oldVec,
                rotations.Prepend(rotations.FirstOrDefault()).ToArray(), stripColor, (x) => Projectile.ai[1] * 2,
                -Main.screenPosition + Projectile.Center - Projectile.velocity * 10);

            strip2.PrepareStrip(
               oldVec,
               rotations.Prepend(rotations.FirstOrDefault()).ToArray(), stripColor2, (x) => Projectile.ai[1] * 1.8f,
               -Main.screenPosition + Projectile.Center - Projectile.velocity * 10);
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
            Main.graphics.GraphicsDevice.Textures[0] = 贴图;
            Main.graphics.GraphicsDevice.BlendState = blendStatef2;
            strip2.DrawTrail();
            strip2.DrawTrail();
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            strip.DrawTrail();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
