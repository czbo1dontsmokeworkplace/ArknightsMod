using ArknightsMod.Content.Items.Weapons.ChenSword;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Chen
{
	public class sw_Proj_1 : ModProjectile
	{
		public static Texture2D SwordLightTex;
		public Texture2D LightCircle_1 => sw_Proj_2.LightCircle_1;
		private static Texture2D Skill_1_Release_Effect_1;
		private static Texture2D Skill_1_Release_Effect_2;

		private Player player => Main.player[Projectile.owner];
		public override void Load() {
			SwordLightTex = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/SwordLightTail_7", AssetRequestMode.ImmediateLoad).Value;
			Skill_1_Release_Effect_1 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/Skill_1_Release_Effect_1", AssetRequestMode.ImmediateLoad).Value;
			Skill_1_Release_Effect_2 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/Skill_1_Release_Effect_2", AssetRequestMode.ImmediateLoad).Value;
			base.Load();
		}
		public override void SetDefaults() {
			Projectile.width = 32;
			Projectile.height = 40;
			Projectile.scale = 1f;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.friendly = true; // 友方弹幕
			Projectile.aiStyle = -1; // 不使用默认的 AI 样式，自定义弹幕行为
			Projectile.tileCollide = false;//false就能让他穿墙
			Projectile.penetrate = -1;//表示能穿透几次
			Projectile.ignoreWater = true;//无视液体
										  //Swing_AI
										  //Projectile.timeLeft = 80;

			//Skill_1_AI
			Projectile.timeLeft = 80;
		}
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 14;
			base.SetStaticDefaults();
		}
		private Vector2 Swing_DrawScale = new Vector2(0);

		#region 普攻
		private void Swing_AI() {
			Projectile.ai[0]++;
			if (Projectile.ai[0] >= 76) {
				player.heldProj = Projectile.whoAmI;
				player.itemAnimation = player.itemTime = 6;
				Projectile.velocity = new Vector2(0, -2).RotatedBy(Projectile.rotation);
				var time = Projectile.ai[0] - 76;
				var extra_Ro = 2f;
				//Dust.NewDustPerfect(Projectile.Center, 6).noGravity = true;
				player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - extra_Ro * player.direction + MathHelper.Pi);
				if (time > 30) {

					Projectile.Center = player.MountedCenter + new Vector2(-7 * player.direction, -15).RotatedBy(Projectile.rotation - extra_Ro * player.direction);

				}
				if (time == 0) {
					if (player.controlUseItem)
						Projectile.timeLeft = 90;

					if (Main.MouseWorld.X > player.Center.X)
						player.direction = 1;
					else
						player.direction = -1;
					Projectile.ai[1] = (Main.MouseWorld - player.Center).ToRotation() + MathHelper.PiOver2;
				}
				if (time < 30) {
					float ro = time / 30f * 0.2f;
					Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.WrapAngle(Projectile.ai[1] + (4 + ro) * player.direction), time / 30f);
					Projectile.Center = Vector2.Lerp(Projectile.Center, player.MountedCenter + new Vector2(-7 * player.direction, -15).RotatedBy(Projectile.rotation - extra_Ro * player.direction), time / 30f);
				}
				else if (time < 35) {
					Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.WrapAngle(Projectile.ai[1] + (4 + 0.4f) * player.direction), 0.03f);

				}
				else if (time < 90) {
					var step = Math.Clamp((time - 35f) * 0.04f, 0, 1);
					Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.WrapAngle(Projectile.ai[1] + (4 + 0.4f) * player.direction) - 4.7f * player.direction, step);

				}
				Swing_DrawScale = Vector2.Lerp(Swing_DrawScale, new Vector2(1), 0.1f);
			}
			else {
				Projectile.rotation = (-MathHelper.Pi + 0.9f) * player.direction;
				Projectile.Center = player.MountedCenter + new Vector2(21 * player.direction, -20);
				Swing_DrawScale = new Vector2(0.9f);
			}
		}
		private void Swing_Draw(SpriteBatch sb, GraphicsDevice gd) {
			if (Projectile.ai[0] > 106) {
				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
				{
					var vertices = new List<Vertex>();
					var count = Math.Clamp(Projectile.ai[0] - 26, 1, Projectile.oldRot.Length - 7);
					var Vertex_Num = 0.1f;

					for (float i = 0; i < count; i++) {
						for (float j = 0.0f; j <= 1f; j += Vertex_Num) {
							Color coordColor = Color.OrangeRed;
							coordColor.A = 0;
							float ro = Projectile.oldRot[(int)i].AngleLerp(Projectile.oldRot[(int)i + 1], j);



							vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 90f,
							new Vector3((float)(i + j) / count, 1, 1),
							coordColor));
							vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * (80f - 60 * (1 - (i + j) / count)),
													new Vector3((float)(i + j + 1) / count, 0, 1),
													coordColor));

							//Main.NewText(a);
						}


					}
					if (vertices.Count >= 3) {
						//Main.NewText(Color.Red.ToVector3());
						gd.Textures[0] = LightCircle_1;
						gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

					}
				}
				//if(false)
				{
					var vertices = new List<Vertex>();
					var count = Math.Clamp(Projectile.ai[0] - 26, 1, Projectile.oldRot.Length - 5);
					var Vertex_Num = 0.1f;

					for (float i = 0; i < count; i++) {
						for (float j = 0.0f; j < 1f; j += Vertex_Num) {
							var coordColor = Color.Lerp(Color.Red, Color.Silver, (i + j) / count);

							float ro = Projectile.oldRot[(int)i].AngleLerp(Projectile.oldRot[(int)i + 1], j);



							vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 80f,
							new Vector3((float)(i + j) / count, 1, 1),
							coordColor));
							vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 30f,
													new Vector3((float)(i + j) / count, 0, 1),
													coordColor));

							//Main.NewText(a);
						}


					}
					if (vertices.Count >= 3) {
						//Main.NewText(Color.Red.ToVector3());
						gd.Textures[0] = SwordLightTex;
						gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

					}
				}
				// if (false)

				{
					var vertices = new List<Vertex>();
					var count = Math.Clamp(Projectile.ai[0] - 26, 1, Projectile.oldRot.Length - 8);
					var Vertex_Num = 0.1f;

					for (float i = 0; i < count; i++) {
						for (float j = 0.0f; j < 1f; j += Vertex_Num) {
							var coordColor = Color.Lerp(Color.SkyBlue, Color.White, (i + j) / count);

							float ro = Projectile.oldRot[(int)i].AngleLerp(Projectile.oldRot[(int)i + 1], j);



							vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 80f,
							new Vector3((float)(i + j) / count, 1, 1),
							coordColor));
							vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 70f,
													new Vector3((float)(i + j) / count, 0, 1),
													coordColor));

							//Main.NewText(a);
						}


					}
					if (vertices.Count >= 3) {
						//Main.NewText(Color.Red.ToVector3());
						gd.Textures[0] = SwordLightTex;
						gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

					}
				}

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			}



			Projectile.KZ_QuicklyDraw_Proj(Swing_DrawScale, spE: player.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);

		}
		private bool Swing_Coll(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Projectile.ai[0] > 107) {
				float point = 0f;
				Vector2 startPoint = Projectile.Center;
				Vector2 endPoint = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 80 * Swing_DrawScale.Length();
				bool K =
					Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
					targetHitbox.Size(),
					startPoint,
					endPoint,
					1
					, ref point);
				if (K && Collision.CanHit(player.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height))
					return true;
			}
			return false;

		}
		#endregion

		#region 技能1
		private bool Skill_1_Coll(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Projectile.ai[1] > 0) {
				float point = 0f;
				Vector2 startPoint = Projectile.Center;
				Vector2 endPoint = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 16 * 15;
				bool K =
					Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
					targetHitbox.Size(),
					startPoint,
					endPoint,
					30
					, ref point);
				if (K && Collision.CanHit(player.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height))
					return true;

			}
			return false;
		}
		private class Skill_1_Release_Dust_1 : ModDust
		{
			public override void OnSpawn(Dust dust) {
				dust.color = Color.White;
				dust.alpha = 220;
				dust.fadeIn = 0.3f;
				dust.scale = 1f;
				dust.velocity = Vector2.Zero;
				dust.rotation = 1;
				base.OnSpawn(dust);
			}
			public override bool Update(Dust dust) {
				dust.fadeIn = MathHelper.Lerp(dust.fadeIn, 1, 0.3f);
				if (dust.alpha < 0)
					dust.active = false;
				else
					dust.alpha -= 10;
				return false;
			}
			static Texture2D t;
			public override void Load() {
				t = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
				base.Load();
			}
			public override bool PreDraw(Dust dust) {
				for (int i = 0; i < 3; i++)
					Main.spriteBatch.Draw(t, dust.position - Main.screenPosition, null, dust.color * (float)(dust.alpha / 130f) * dust.fadeIn, dust.rotation == 1 ? 0 : MathHelper.Pi, t.Size() / 2f + new Vector2(25, 0), dust.scale, dust.rotation == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
				return false;
			}
		}
		private void Skill_1_AI() {
			var rand = Main.rand;
			if (Projectile.ai[0] == 0) {
				Projectile.rotation = (MathHelper.Pi + 0.8f) * player.direction;
			}
			player.heldProj = Projectile.whoAmI;
			player.itemAnimation = player.itemTime = 3;
			Swing_DrawScale = new Vector2(1);
			Projectile.velocity = new Vector2(player.direction, 0);
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + MathHelper.PiOver2 * player.direction);
			//if (Projectile.ai[0] < 40)
			{
				Projectile.rotation = Projectile.rotation.AngleLerp((MathHelper.Pi + MathHelper.PiOver2 * 0.9f) * player.direction, 0.2f);
			}
			if (Projectile.ai[0] > 30 && Projectile.ai[1] == 0) {
				if (rand.NextBool(2) && player.controlUseItem) {
					var d = Dust.NewDustPerfect(Projectile.Center + new Vector2(player.direction * -15, 1), 222);
					d.noGravity = true;
					d.velocity = new Vector2(player.direction, -1).RotatedByRandom(1.2) * 2 * rand.NextFloat(0.5f, 1.5f);
					d.fadeIn = 0.4f;
					d.scale = 0.2f;
				}
			}
			if (player.controlUseItem && Projectile.ai[1] == 0) {
				Projectile.timeLeft = 10;
			}
			else if (Projectile.ai[0] > 40) {
				if (Projectile.ai[1] == 0) {

					Projectile.timeLeft = 35;
					Dust.NewDustPerfect(player.Center, ModContent.DustType<Skill_1_Release_Dust_1>()).rotation = player.direction;
					//222黄
					//219红
					//for (int ii = 0; ii < 3; ii++)
					{
						for (int i = 0; i < 14; i++) {
							var d = Dust.NewDustPerfect(player.Center, 222);
							d.noGravity = true;
							d.velocity = new Vector2(player.direction * 20, 0) * rand.NextFloat(0.4f, 1.5f);
							d.scale = 0.8f;


						}
						for (int i = -10; i <= 10; i++) {
							var d = Dust.NewDustPerfect(player.Center, 222);
							d.noGravity = true;
							d.velocity = new Vector2(player.direction * 13, 0).RotatedByRandom(0.7) * rand.NextFloat(0.4f, 1.3f);
							d.scale = 0.6f;


						}
						for (int i = -10; i <= 10; i++) {
							var d = Dust.NewDustDirect(player.Center - new Vector2(0, 20), 0, 40, 219);
							d.noGravity = true;
							d.velocity = new Vector2(player.direction * 23, 0) * rand.NextFloat(0.0f, 1.4f);
							d.scale = 0.6f;
						}

					}
				}
				if (Projectile.ai[1] == 6) {
					for (float i = 0.1f; i < 1; i += 0.02f) {
						var d = Dust.NewDustPerfect(player.Center + Projectile.velocity * i * 16 * 15, 76);
						d.noGravity = true;
						d.velocity = new Vector2(0, 4 * (rand.NextBool() ? -1 : 1)) * rand.NextFloat(0.3f, 1.4f) * (1.2f - Math.Abs(i - 0.5f) * 1.8f);
						var c = Color.Blue;
						if (rand.NextBool())
							c = Color.Red;
						c.A = 0;
						d.color = c;
						//d.fadeIn = 1.3f;
						//d.scale = 0.6f;
					}

				}
				Projectile.ai[1]++;
			}
			var x_ = Math.Clamp(Projectile.ai[0] * 0.4f, 0, 15) * player.direction;
			Projectile.Center = player.MountedCenter + new Vector2(player.direction * 30 - x_, 3 + x_ * player.direction * 0.3f);
			Projectile.ai[0]++;
		}
		private void Skill_1_Draw(SpriteBatch sb) {
			Projectile.KZ_QuicklyDraw_Proj(Swing_DrawScale);
			if (Projectile.ai[1] > 0) {
				var ControlVal = 13f;
				var val = Math.Clamp(Projectile.ai[1], 0, ControlVal * 2);
				var posx = Math.Clamp(Projectile.ai[1], 0, ControlVal) / ControlVal;
				var invert_posx = 1f - posx;
				val = Math.Abs(val - ControlVal);
				val = ControlVal - val;
				val /= ControlVal;
				var col = Color.Red * val;
				col.A = 0;
				sb.Draw(Skill_1_Release_Effect_1, Projectile.Center - Main.screenPosition, null, col, Projectile.rotation + MathHelper.PiOver2, Skill_1_Release_Effect_1.Size() / 2f, new Vector2(1, 0.5f), default, 0);

				float Skill_1_Release_Effect_2_Scale = 2;
				{
					sb.Draw(Skill_1_Release_Effect_2, Projectile.Center + new Vector2(300 * posx, 0) * player.direction - Main.screenPosition, null, col, player.direction == 1 ? 0 : MathHelper.Pi, Skill_1_Release_Effect_2.Size() / 2f, new Vector2(1.4f, 0.7f * invert_posx) * Skill_1_Release_Effect_2_Scale, default, 0);
					sb.Draw(Skill_1_Release_Effect_2, Projectile.Center + new Vector2(300 * posx, 0) * player.direction - Main.screenPosition, null, col, player.direction == 1 ? MathHelper.Pi : 0, Skill_1_Release_Effect_2.Size() / 2f, new Vector2(1, 0.7f * invert_posx) * Skill_1_Release_Effect_2_Scale, SpriteEffects.FlipHorizontally, 0);

				}
				sb.Draw(Skill_1_Release_Effect_1, Projectile.Center + new Vector2(100, 0) * player.direction - Main.screenPosition, null, col, 0, Skill_1_Release_Effect_1.Size() / 2f, new Vector2(2, 1f * invert_posx), default, 0);

				col = Color.White * val;
				col.A = 0;
				sb.Draw(Skill_1_Release_Effect_1, Projectile.Center - Main.screenPosition, null, col, Projectile.rotation + MathHelper.PiOver2, Skill_1_Release_Effect_1.Size() / 2f, new Vector2(1, 0.3f), default, 0);
				sb.Draw(Skill_1_Release_Effect_1, Projectile.Center + new Vector2(100, 0) * player.direction - Main.screenPosition, null, col, 0, Skill_1_Release_Effect_1.Size() / 2f, new Vector2(2, 1f * invert_posx) * 0.95f, default, 0);

				{
					sb.Draw(Skill_1_Release_Effect_2, Projectile.Center + new Vector2(300 * posx, 0) * player.direction - Main.screenPosition, null, col, player.direction == 1 ? 0 : MathHelper.Pi, Skill_1_Release_Effect_2.Size() / 2f, new Vector2(1.4f, 0.7f * invert_posx) * 0.94f * Skill_1_Release_Effect_2_Scale, default, 0);
					sb.Draw(Skill_1_Release_Effect_2, Projectile.Center + new Vector2(300 * posx, 0) * player.direction - Main.screenPosition, null, col, player.direction == 1 ? MathHelper.Pi : 0, Skill_1_Release_Effect_2.Size() / 2f, new Vector2(1, 0.7f * invert_posx) * 0.94f * Skill_1_Release_Effect_2_Scale, SpriteEffects.FlipHorizontally, 0);

				}

			}
		}
		#endregion

		#region 技能2
		private void Skill_2_AI() {
			Projectile.rotation = (-MathHelper.Pi + 0.9f) * player.direction;
			Projectile.Center = player.MountedCenter + new Vector2(21 * player.direction, -20);

			if (!player.controlUseItem)
				Projectile.ai[0]++;
			if (Projectile.ai[0] == 0) {
				Swing_DrawScale = new Vector2(0.9f);
			}
			else
				Swing_DrawScale = Vector2.Zero;

			Projectile.timeLeft = 3;

			foreach (var p in Main.projectile) {
				if (p.active && p != null)
					if (p.type == ModContent.ProjectileType<sw_Proj_2>()) {
						return;
					}
			}
			Projectile.Kill();

		}
		private void Skill_2_Draw() {
			Projectile.KZ_QuicklyDraw_Proj(Swing_DrawScale);
		}

		#endregion

		public override void AI() {
			//Swing_AI();
			//Skill_1_AI();
			Skill_2_AI();
			base.AI();
		}
		public override bool PreDraw(ref Color lightColor) {
			var sb = Main.spriteBatch;
			var gd = Main.graphics.GraphicsDevice;

			//Swing_Draw(sb, gd);
			//Skill_1_Draw(sb);
			Skill_2_Draw();
			return false;
		}
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			//return Skill_1_Coll(projHitbox, targetHitbox);
			//return Swing_Coll(projHitbox, targetHitbox);
			return false;
		}
		public override bool ShouldUpdatePosition() {
			return false;
		}
		private Queue<NPC> Target = new Queue<NPC>();
		public override bool? CanHitNPC(NPC target) {
			if (Target.Contains(target))
				return false;
			return base.CanHitNPC(target);
		}
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Target.Enqueue(target);
			Dust.NewDustDirect(target.position, target.Hitbox.Width, target.Hitbox.Height, ModContent.DustType<OnHit_Dust_1>());
			base.OnHitNPC(target, hit, damageDone);
		}
	}
}