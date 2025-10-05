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
using Terraria.WorldBuilding;

namespace ArknightsMod.Content.Projectiles.Wisdel
{
	public class WisdelShotLarge : ModProjectile
	{
		public override void SetStaticDefaults()
		{
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 14;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
		public override void SetDefaults()
		{
            Projectile.width = 30;
			Projectile.height = 30;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.scale = 1.2f;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
            Projectile.extraUpdates = 2;

		}
		public int timer;
		public override void AI()
		{
			if (++timer > 10)
			{
				Projectile.tileCollide = true;
			}
            Projectile.rotation = Projectile.velocity.ToRotation() + 1.57079637f;
            
			if (hasHit) {
				Projectile.velocity *= 0.5f;
				Projectile.alpha += 8;
				if (Projectile.alpha > 255) {
					Projectile.Kill();
				}
			}
		}
        public override void OnKill(int timeLeft)
        {
			Player player = Main.player[Projectile.owner];
			Projectile.NewProjectile
				(new EntitySource_Parent(Projectile), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<WisdelHitNormal>(), 0, 0, Projectile.owner);

			Projectile.NewProjectile
			(new EntitySource_Parent(Projectile), Projectile.Center, Vector2.Zero,
			ModContent.ProjectileType<WisdelExplode>(), 0, 0, Projectile.owner, hasHit ? 0 : 10);

			Explode(player);
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
			HitEffect(target.Center);
			Explode(player, target);
			hasHit = true;
        }
		public void Explode(Player player, NPC ignoreNPC = null)
		{
			HitEffect(Projectile.Center);
			foreach (NPC npc in Main.npc)
			{
				bool immortal = npc.dontTakeDamage || npc.townNPC;
				bool noKnockback = (npc.type == NPCID.TargetDummy || npc.knockBackResist == 0 || npc.immortal);
				if (npc != ignoreNPC && npc.active && !immortal && Vector2.Distance(npc.Center, Projectile.Center) < 16 * 25)
				{
					NPC.HitInfo info = new();
					bool crit = Main.rand.Next(100) < Projectile.CritChance;
					info.Damage = (int)(Projectile.damage * (crit ? 2f : 1f) * Main.rand.NextFloat(0.95f, 1.051f));
					info.Knockback = noKnockback ? 0f : Projectile.knockBack;
					info.HitDirection = (npc.position.X - player.position.X > 0 ? 1 : -1);
					info.Crit = crit;
					info.DamageType = Projectile.DamageType;
					npc.StrikeNPC(info);
					HitEffect(npc.Center);
				}
			}
		}
		public void HitEffect(Vector2 center) {

			Projectile.NewProjectile
				(new EntitySource_Parent(Projectile), center, Vector2.Zero,
				ModContent.ProjectileType<WisdelHitNormal>(), 0, 0, Projectile.owner);


			for (int i = 0; i < 12; i++) {
				Vector2 newVelocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(360));
				newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 1.5f;
				DefaultParticleNonPre p = new DefaultParticleNonPre(center,
				newVelocity, 90, Main.rand.NextFloat(0.35f, 0.5f) * 4f, Color.Black, true);
				p.Deformation = new Vector2(0.1f, 0.6f) * Main.rand.NextFloat(1, 2);
				p.Spawn();
			}
			for (int i = 0; i < 12; i++) {
				Vector2 newVelocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(360));
				newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 1.5f;
				DefaultParticle p = new DefaultParticle(center,
				newVelocity, 90, Main.rand.NextFloat(0.35f, 0.5f) * 4f, new Color(249, 90, 100), true);
				p.Deformation = new Vector2(0.1f, 0.6f) * Main.rand.NextFloat(1, 2);
				p.Spawn();
			}

			for (int i = 0; i < Main.rand.Next(6, 12); i++) {
				Vector2 newVelocity = Vector2.One.RotatedByRandom(MathHelper.ToRadians(360));
				newVelocity *= Main.rand.NextFloat(0.1f, 0.35f) * 10;
				if (!Main.rand.NextBool(4)) {
					DefaultParticle p = new DefaultParticle(center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
					newVelocity, 90, Main.rand.NextFloat(0.2f, 0.5f) * 3.5f, new Color(249, 90, 100), true);
					p.Deformation = new Vector2(0.35f, 0.4f) * Main.rand.NextFloat(1, 2);
					p.Spawn();
				}
				else {
					DefaultParticleNonPre p = new DefaultParticleNonPre(center + new Vector2(Main.rand.Next(-Projectile.width, Projectile.width), Main.rand.Next(-Projectile.height, Projectile.height)) / 2f,
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
				Projectile.rotation, tex.Size() / 2, Projectile.scale * new Vector2(0.2f, 0.4f)*1.5f, SpriteEffects.None, 0f);
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
				return 28f * MathHelper.Lerp(1f, 0.5f, prog / 1.5f);
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
				colorFunction, widthFunction, -Main.screenPosition
				+ new Vector2(Projectile.width, Projectile.height) / 2
				+ new Vector2(-2, 0).RotatedBy(Projectile.rotation));
			strip.DrawTrail();
			Main.pixelShader.CurrentTechnique.Passes[0].Apply();

			if (hasCollide)
				return;
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null,
				Color.White * Projectile.Opacity, Projectile.rotation, tex.Size() / 2, Projectile.scale*new Vector2(0.8f,1.5f), SpriteEffects.None, 0f);

		}
		private float transitToDark;
	}
}
