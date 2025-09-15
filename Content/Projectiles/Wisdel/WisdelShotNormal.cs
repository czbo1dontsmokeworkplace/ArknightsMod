using System;
using System.Collections.Generic;
using System.Linq;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.Saki;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Wisdel
{
	public class WisdelShotNormal : ModProjectile
	{
		public override void SetStaticDefaults()
		{
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 14;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
		public override void SetDefaults()
		{
            Projectile.width = 18;
			Projectile.height = 18;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
            Projectile.extraUpdates = 2;

		}
		public int timer;
		public override void AI()
		{
			if (++timer > 30)
			{
				Projectile.tileCollide = true;
			}
            Projectile.rotation = Projectile.velocity.ToRotation() + 1.57079637f;
            if (Projectile.timeLeft < 170 && Main.rand.NextBool(12))
            {
                /*for (int i = 0; i < 3; i++)
                {
                    Vector2 newVelocity = Vector2.One.RotatedByRandom(MathHelper.ToRadians(180));
                    newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 8;
                    DefaultParticleNonPre p = new DefaultParticleNonPre(Projectile.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 4f,
                    newVelocity, 90, Main.rand.NextFloat(0.2f, 0.5f) * 2f, Color.Black, true);
                    p.Deformation = new Vector2(0.3f, 0.4f) * Main.rand.NextFloat(1, 2);
                    p.Spawn();
                }*/
            }
			if (hasHit) {
				Projectile.velocity *= 0.5f;
				Projectile.alpha += 10;
				if (Projectile.alpha > 255) {
					Projectile.Kill();
				}
			}
		}
        public override void OnKill(int timeLeft)
        {
			if (!hasHit)
			{
				SoundEngine.PlaySound(Wisdel_Probe.ShootBlast.WithVolumeScale(1.5f), Projectile.position);
				Projectile.NewProjectile
				(new EntitySource_Parent(Projectile), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<WisdelHitNormal>(), 0, 0, Projectile.owner);

				for (int i = 0; i < 6; i++) {
					Vector2 newVelocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30));
					newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 3f;
					DefaultParticleNonPre p = new DefaultParticleNonPre(Projectile.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.35f, 0.5f) * 4f, Color.Black, true);
					p.Deformation = new Vector2(0.1f, 0.6f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
				for (int i = 0; i < 6; i++) {
					Vector2 newVelocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30));
					newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 3f;
					DefaultParticle p = new DefaultParticle(Projectile.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.35f, 0.5f) * 4f, new Color(249, 90, 100), true);
					p.Deformation = new Vector2(0.1f, 0.6f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
				for (int i = 0; i < Main.rand.Next(6, 12); i++) {
					Vector2 newVelocity = Vector2.One.RotatedByRandom(MathHelper.ToRadians(360));
					newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 10;
					DefaultParticle p = new DefaultParticle(Projectile.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.2f, 0.5f) * 3.5f, new Color(249, 90, 100), true);
					p.Deformation = new Vector2(0.35f, 0.4f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
			}
		}
		public override bool? CanDamage()
		{
			return !hasHit;
		}
		public bool hasHit;
		public bool hasCollide;
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
			if (!hasHit)
			{
				SoundEngine.PlaySound(Wisdel_Probe.ShootBlast.WithVolumeScale(1.5f), Projectile.position);

				Projectile.NewProjectile
				(new EntitySource_Parent(Projectile), target.Center, Vector2.Zero,
				ModContent.ProjectileType<WisdelHitNormal>(), 0, 0, Projectile.owner);

				for (int i = 0; i < 6; i++) {
					Vector2 newVelocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30));
					newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 1.5f;
					DefaultParticleNonPre p = new DefaultParticleNonPre(target.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.35f, 0.5f) * 4f, Color.Black, true);
					p.Deformation = new Vector2(0.1f, 0.6f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
				for (int i = 0; i < 6; i++) {
					Vector2 newVelocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30));
					newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 1.5f;
					DefaultParticle p = new DefaultParticle(target.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.35f, 0.5f) * 4f, new Color(249, 90, 100), true);
					p.Deformation = new Vector2(0.1f, 0.6f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
				hasHit = true;
			}
			
            for (int i = 0; i < Main.rand.Next(6, 12); i++)
            {
                Vector2 newVelocity = Vector2.One.RotatedByRandom(MathHelper.ToRadians(360));
                newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 10;
				if (!Main.rand.NextBool(4)) {
					DefaultParticle p = new DefaultParticle(target.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.2f, 0.5f) * 3.5f, new Color(249, 90, 100), true);
					p.Deformation = new Vector2(0.35f, 0.4f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
				else {
					DefaultParticleNonPre p = new DefaultParticleNonPre(target.Center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
				newVelocity, 90, Main.rand.NextFloat(0.2f, 0.5f) * 3.5f, Color.Black, true);
					p.Deformation = new Vector2(0.35f, 0.4f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
            }
        }
		public override bool PreDraw(ref Color lightColor) {
			Player player = Main.player[Projectile.owner];
			Color color = Color.HotPink;
			Texture2D tex = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Wisdel/star_02").Value;
			if (!hasCollide) { 
				Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null,
				new Color(color.R, color.G, color.B, Projectile.alpha),
				Projectile.rotation, tex.Size() / 2, Projectile.scale * new Vector2(0.2f, 0.4f), SpriteEffects.None, 0f);
			}
			Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            
            return false;
        }
        public override void PostDraw(Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			VertexStrip.StripColorFunction colorFunction = (prog) =>
            {
				float lerpValue = Utils.GetLerpValue(0f - 0.1f * this.transitToDark, 0.7f - 0.2f * this.transitToDark, prog, true);
				Color result = Color.Lerp(Color.Lerp(Color.HotPink, Color.HotPink, this.transitToDark * 0.5f), Color.Red, lerpValue) * (1f - Utils.GetLerpValue(0f, 0.98f, prog, false));
				result.A /= 8;
				return result * Projectile.Opacity;
			};

            VertexStrip.StripHalfWidthFunction widthFunction = (prog) =>
            {
				return 10f * MathHelper.Lerp(1f, 0.5f, prog / 1.5f);
			};

            VertexStrip strip = new VertexStrip();


			Main.graphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
			this.transitToDark = Utils.GetLerpValue(0f, 6f, 0.1f, true);
			MiscShaderData miscShaderData = GameShaders.Misc["FlameLash"];
			miscShaderData.UseSaturation(-2f);
			miscShaderData.UseOpacity(50f);
			miscShaderData.Apply(null);
			miscShaderData.UseImage0("Images/Extra_194");
			var rotations = Projectile.oldPos.Zip(Projectile.oldPos.Skip(1), (a, b) => a - b).Select((a) => a.ToRotation());
			strip.PrepareStrip(Projectile.oldPos, rotations.Prepend(rotations.FirstOrDefault()).ToArray(),
				colorFunction, widthFunction, -Main.screenPosition + new Vector2(Projectile.width, Projectile.height) / 2);
			strip.DrawTrail();
			Main.pixelShader.CurrentTechnique.Passes[0].Apply();

			if (hasCollide)
				return;
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null,
				Color.White * Projectile.Opacity, Projectile.rotation, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0f);

		}
		private float transitToDark;
	}
}
