using ArknightsMod.Content.Items.Weapons.ChenSword;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Chen
{
	public class sw_Proj_2 : ModProjectile
	{
		Texture2D SwordLightTex => sw_Proj_1.SwordLightTex;
		public static Texture2D LightCircle_1;
		private static Asset<Texture2D> sw_Proj_2_old;
		private static Asset<Texture2D> sw_Proj_2_t2;
		private static Texture2D SwordLightTex_8;
		private Player player => Main.player[Projectile.owner];
		public override void Load() {
			LightCircle_1 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/LightCircle_1", AssetRequestMode.ImmediateLoad).Value;
			SwordLightTex_8 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/SwordLightTail_8", AssetRequestMode.ImmediateLoad).Value;
			sw_Proj_2_old = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);
			sw_Proj_2_t2 = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Chen/sw_Proj_2_t2", AssetRequestMode.ImmediateLoad);
			base.Load();
		}
		public override void SetDefaults() {
			Projectile.width = 58;
			Projectile.height = 60;
			Projectile.scale = 1f;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.friendly = true; // 友方弹幕
			Projectile.aiStyle = -1; // 不使用默认的 AI 样式，自定义弹幕行为
			Projectile.tileCollide = false;//false就能让他穿墙
			Projectile.penetrate = -1;//表示能穿透几次
			Projectile.ignoreWater = true;//无视液体
			Projectile.timeLeft = 95;
		}
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 14;
			base.SetStaticDefaults();
		}
		private Vector2 Swing_DrawScale = new Vector2(0);
		public override bool ShouldUpdatePosition() {
			return false;
		}
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			//return Swing_Coll(projHitbox, targetHitbox);
			//return false;
			return false;
		}
		private Queue<NPC> Target = new Queue<NPC>();
		public override bool? CanHitNPC(NPC target) {
			if (Target.Contains(target))
				return false;
			return base.CanHitNPC(target);
		}
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Dust.NewDustDirect(target.position, target.Hitbox.Width, target.Hitbox.Height, ModContent.DustType<OnHit_Dust_1>());

			Target.Enqueue(target);
			base.OnHitNPC(target, hit, damageDone);
		}

		#region 普攻
		private void Swing_AI() {
			if (Projectile.ai[0] < 76) {
				if (Projectile.ai[0] > 12)
					player.heldProj = Projectile.whoAmI;
				Projectile.Center = player.MountedCenter + new Vector2(5).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
				player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + MathHelper.Pi);
				player.itemAnimation = player.itemTime = 6;

			}
			Projectile.velocity = new Vector2(0, -3).RotatedBy(Projectile.rotation);
			if (Projectile.ai[0] == 0) {
				Projectile.ai[1] = (Main.MouseWorld - player.Center).ToRotation() + MathHelper.PiOver2;
				Projectile.rotation = Projectile.ai[1];
				Swing_DrawScale = new Vector2(0);
			}
			else if (Projectile.ai[0] <= 12) {
				Swing_DrawScale = new Vector2((Projectile.ai[0] - 6f) / 6f);
			}
			else if (Projectile.ai[0] < 26) {
				Swing_DrawScale = new Vector2(1);
			}
			else if (Projectile.ai[0] == 26) {
				if (Main.MouseWorld.X > player.Center.X)
					player.direction = 1;
				else
					player.direction = -1;
				Target.Clear();
				Projectile.ai[1] = (Main.MouseWorld - player.Center).ToRotation() + MathHelper.PiOver2;
				Projectile.rotation = Projectile.ai[1] - 2 * player.direction;
			}
			else if (Projectile.ai[0] < 76) {
				Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.ai[1] + 2.3f * player.direction, 0.22f);
			}
			else {
				if (Projectile.ai[0] == 78) {
					if (player.controlUseItem)
						Projectile.timeLeft = 94;

				}
				if (Projectile.ai[0] < 86) {
					Projectile.rotation = Projectile.rotation.AngleLerp(MathHelper.Pi + 0.5f * player.direction, 0.1f);

					Projectile.Center = Vector2.Lerp(Projectile.Center, player.MountedCenter + new Vector2(19 * player.direction, -30), 0.1f);
				}
				else {
					Projectile.rotation = MathHelper.Pi + 0.5f * player.direction;

					Projectile.Center = player.MountedCenter + new Vector2(19 * player.direction, -30);
				}
				Swing_DrawScale = Vector2.Lerp(Swing_DrawScale, new Vector2(0.8f), 0.1f);
			}
			Projectile.ai[0]++;
		}
		private void Swing_Draw(SpriteBatch sb, GraphicsDevice gd) {
			if (Projectile.ai[0] > 26 && Projectile.ai[0] < 76) {
				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
				{
					var vertices = new List<Vertex>();
					var count = Math.Clamp(Projectile.ai[0] - 26, 1, Projectile.oldRot.Length - 7);
					var Vertex_Num = 0.1f;

					for (float i = 0; i < count; i++) {
						for (float j = 0.0f; j <= 1f; j += Vertex_Num) {
							Color coordColor = Color.SkyBlue;
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

				{
					var vertices = new List<Vertex>();
					var count = Math.Clamp(Projectile.ai[0] - 26, 1, Projectile.oldRot.Length - 5);
					var Vertex_Num = 0.1f;

					for (float i = 0; i < count; i++) {
						for (float j = 0.0f; j < 1f; j += Vertex_Num) {
							var coordColor = Color.Lerp(Color.DarkRed, Color.Silver, (float)Math.Clamp(i * 0.07, 0, 1));

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
				{
					var vertices = new List<Vertex>();
					var Vertex_Num = 0.1f;

					for (float j = 0.0f; j <= 1.1f; j += Vertex_Num) {
						var coordColor = Color.Lerp(Color.IndianRed, Color.White, j);
						coordColor.A = 0;
						float ro = Projectile.rotation.AngleLerp(Projectile.oldRot[4], j);



						vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 83f,
						new Vector3((float)j, 1, 1),
						coordColor));
						vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 70f,
												new Vector3((float)j, 0, 1),
												coordColor));

						//Main.NewText(a);
					}
					if (vertices.Count >= 3) {
						//Main.NewText(Color.Red.ToVector3());
						gd.Textures[0] = LightCircle_1;
						//for(int i = 0; i < 2; i ++)
						gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

					}
				}

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			}

			Projectile.KZ_QuicklyDraw_Proj(Swing_DrawScale);
			//第一段刺出去的效果
			if (Projectile.ai[0] <= 12) {
				var ex = LightCircle_1;
				var time = (Projectile.ai[0] - 6f) / 6f;
				var scale = new Vector2(0.6f, 1 + time);
				if (time > 0)
					scale = new Vector2(0.6f * (1f - time), 1);

				scale *= new Vector2(0.1f, 1);
				scale *= 0.9f;
				var c = Color.DarkRed;
				c.A = 0;
				c *= 0.7f;
				sb.Draw(ex, player.Center - Main.screenPosition, null, c, Projectile.ai[1], ex.Size() / 2f, scale, default, 0);
				c = Color.White;
				c.A = 0;
				scale *= 0.5f;
				sb.Draw(ex, player.Center - Main.screenPosition, null, c, Projectile.ai[1], ex.Size() / 2f, scale, default, 0);

			}
		}
		private bool Swing_Coll(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Projectile.ai[0] < 72) {
				float point = 0f;
				Vector2 startPoint = Projectile.Center;
				Vector2 endPoint = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 80 * Swing_DrawScale.Length() / 1.3f;
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
		private void Skill_1_AI() {
			Projectile.rotation = MathHelper.Pi + 0.5f * player.direction;

			Projectile.Center = player.MountedCenter + new Vector2(19 * player.direction, -30);
			Swing_DrawScale = new Vector2(0.88f);
			Projectile.timeLeft = 3;

			foreach (var p in Main.projectile) {
				if (p.active && p != null)
					if (p.type == ModContent.ProjectileType<sw_Proj_1>()) {
						return;
					}
			}
			Projectile.Kill();
		}
		private void Skill_1_Draw() {
			Projectile.KZ_QuicklyDraw_Proj(Swing_DrawScale);
		}
		#endregion

		#region 技能2
		private class Skill_2_Dust_1 : ModDust
		{

			static Texture2D t1;
			public override void Load() {
				t1 = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

				base.Load();
			}
			public override void OnSpawn(Dust d) {
				d.color = Color.Red;
				d.color.A = 200;
				d.scale = 0;
				d.alpha = 255;
				base.OnSpawn(d);
			}
			public override bool Update(Dust dust) {
				dust.scale = MathHelper.Lerp(dust.scale, 1f, 0.1f);
				dust.alpha -= 10;
				if (dust.alpha < 0)
					dust.active = false;
				return false;
			}
			public override bool PreDraw(Dust dust) {
				var sb = Main.spriteBatch;
				var c = dust.color;
				c.A = 0;
				sb.Draw(LightCircle_1, dust.position - Main.screenPosition, null, c * (dust.alpha / 255f), MathHelper.PiOver4, LightCircle_1.Size() / 2f, dust.scale * 0.1f, default, 0);

				sb.Draw(t1, dust.position - Main.screenPosition, null, dust.color * (dust.alpha / 225f), 0, t1.Size() / 2f, dust.scale, default, 0);

				return false;
			}

		}
		private class Skill_2_Dust_2 : ModDust
		{

			static Texture2D t1;
			public override void Load() {
				t1 = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

				base.Load();
			}
			public override void OnSpawn(Dust d) {
				d.color = Color.Red;
				d.scale = 0.5f;
				d.alpha = 255;
				d.fadeIn = 0.1f;
				d.rotation = 3;
				base.OnSpawn(d);
			}
			public override bool Update(Dust dust) {
				dust.scale = MathHelper.Lerp(dust.scale, dust.rotation, dust.fadeIn);
				dust.alpha -= (int)(100 * dust.fadeIn);
				if (dust.alpha < 0)
					dust.active = false;
				return false;
			}
			public override bool PreDraw(Dust dust) {
				var sb = Main.spriteBatch;

				var white = Color.White;
				var black = Color.Black;
				for (int i = 0; i < 3; i++)
					sb.Draw(t1, dust.position - Main.screenPosition, null, black * (dust.alpha / 255f), dust.velocity.ToRotation(), t1.Size() / 2f, dust.scale * 1.1f * new Vector2(1, 0.5f), default, 0);

				sb.End();
				sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

				for (int i = 0; i < 4; i++) {
					sb.Draw(t1, dust.position - Main.screenPosition, null, dust.color * (dust.alpha / 255f), dust.velocity.ToRotation(), t1.Size() / 2f, dust.scale * new Vector2(1, 0.7f), default, 0);

					sb.Draw(t1, dust.position - Main.screenPosition, null, white * (dust.alpha / 255f), dust.velocity.ToRotation(), t1.Size() / 2f, dust.scale * 0.9f * new Vector2(1, 0.3f), default, 0);
				}
				sb.End();
				sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

				return false;
			}

		}
		private class Skill_2_Dust_3 : ModDust
		{

			static Texture2D t1;
			public override void Load() {
				t1 = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

				base.Load();
			}
			public override void OnSpawn(Dust d) {
				d.color = Color.Red;
				d.scale = 0.7f;
				d.alpha = 255;
				base.OnSpawn(d);
			}
			public override bool Update(Dust dust) {
				dust.position += dust.velocity;
				dust.velocity *= 0.94f;
				if (dust.velocity.Length() < 4)
					dust.alpha -= 20;
				if (dust.alpha < 0)
					dust.active = false;
				return false;
			}
			public override bool PreDraw(Dust dust) {
				var sb = Main.spriteBatch;
				var c = dust.color;
				c.A = 0;
				sb.Draw(LightCircle_1, dust.position - Main.screenPosition, null, c * (dust.alpha / 255f), MathHelper.PiOver4, LightCircle_1.Size() / 2f, dust.scale * 0.2f, default, 0);

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


				sb.Draw(t1, dust.position - Main.screenPosition, null, Color.White * (dust.alpha / 255f), dust.velocity.ToRotation(), new Vector2(96, 64), dust.scale, default, 0);
				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

				return false;
			}

		}
		private class Skill_2_Dust_4 : ModDust
		{

			static Texture2D t1;
			public override void Load() {
				t1 = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

				base.Load();
			}
			public override void OnSpawn(Dust d) {
				d.color = Color.Orange;
				d.scale = 0;
				d.fadeIn = 3;
				d.alpha = 255;
				base.OnSpawn(d);
			}
			public override bool Update(Dust dust) {
				dust.scale = MathHelper.Lerp(dust.scale, dust.fadeIn, 0.1f);
				dust.alpha -= 10;
				if (dust.alpha < 0)
					dust.active = false;
				return false;
			}
			public override bool PreDraw(Dust dust) {
				var sb = Main.spriteBatch;
				for (int i = 0; i < 2; i++)
					sb.Draw(t1, dust.position - Main.screenPosition, null, dust.color * (dust.alpha / 135f), MathHelper.PiOver4, t1.Size() / 2f, dust.scale, default, 0);

				for (int i = 0; i < 4; i++)
					sb.Draw(t1, dust.position - Main.screenPosition, null, Color.White * (dust.alpha / 255f), 0, t1.Size() / 2f, dust.scale, default, 0);

				return false;
			}

		}

		private class Dragon_Proj : ModProjectile
		{
			public override void SetDefaults() {
				Projectile.width = 40;
				Projectile.height = 40;
				Projectile.scale = 1f;
				Projectile.DamageType = DamageClass.Melee;
				Projectile.friendly = true; // 友方弹幕
				Projectile.aiStyle = -1; // 不使用默认的 AI 样式，自定义弹幕行为
				Projectile.tileCollide = false;//false就能让他穿墙
				Projectile.penetrate = -1;//表示能穿透几次
				Projectile.ignoreWater = true;//无视液体
				Projectile.timeLeft = 10;
			}
			private List<Vector2> RecordPos = new List<Vector2>();
			private Vector2 HeadPos = Vector2.Zero;
			private Vector2 OldHeadPos = Vector2.Zero;
			private Vector2 AttackPos = Vector2.Zero;
			public override void Load() {
				base.Load();
			}
			public override bool? CanDamage() {
				return AttackPos.Length() > 2;
			}
			public override void AI() {
				if (Projectile.ai[1] == 0)

					Projectile.timeLeft = 40;
				if (RecordPos.Count > 13) {
					RecordPos.RemoveAt(0);
				}
				foreach (var p in Main.projectile) {
					if (p.active && p != null)
						if (p.type == ModContent.ProjectileType<sw_Proj_2>()) {
							var player = Main.player[Projectile.owner];
							if (player != null) {
								if (!player.controlUseItem) {
									if (Projectile.ai[1] == 0 && p.ai[0] > 80) {
										if (Vector2.Distance(Main.MouseWorld, HeadPos) > 500)
											AttackPos = HeadPos + new Vector2(500, 0).RotatedBy((Main.MouseWorld - HeadPos).ToRotation());
										else
											AttackPos = Main.MouseWorld;
									}
									Projectile.ai[1]++;
								}
								if (Projectile.ai[1] > 0) {
									HeadPos = Vector2.Lerp(HeadPos, AttackPos, 0.1f);
									Projectile.Center = HeadPos;
									if (Vector2.Distance(RecordPos[RecordPos.Count - 1], HeadPos) > 10) {
										RecordPos.Add(HeadPos);
									}
									Projectile.rotation = Projectile.rotation.AngleLerp((HeadPos - OldHeadPos).ToRotation(), 0.1f);//ro +  MathHelper.PiOver2 * player.direction;
								}
								else {

									var val = Math.Clamp(Projectile.ai[0] * 0.03f, 0, 1);
									var ro = MathHelper.Lerp(0.7f, MathHelper.Pi + MathHelper.PiOver2, val) * player.direction + (player.direction == 1 ? 0 : MathHelper.Pi);
									var center = new Vector2(90, 0).RotatedBy(ro);
									Projectile.Center = Vector2.Lerp(Projectile.Center, player.MountedCenter, 0.1f);
									HeadPos = center + Projectile.Center;

									Projectile.rotation = Projectile.rotation.AngleLerp((HeadPos - OldHeadPos).ToRotation() + 0.1f * player.direction, 0.2f);//ro +  MathHelper.PiOver2 * player.direction;
																																							 //Projectile.rotation = Projectile.rotation.AngleLerp((Main.MouseWorld - HeadPos).ToRotation(), 0.1f);

									Projectile.direction = player.direction;
									if (Projectile.ai[0] == 0) {
										RecordPos.Add(HeadPos);
									}
									else {
										if (Vector2.Distance(RecordPos[RecordPos.Count - 1], HeadPos) > 10) {
											RecordPos.Add(HeadPos);
										}
									}
									for (int i = 0; i < RecordPos.Count - 1; ++i) {
										var valll = i / (float)RecordPos.Count * 0.1f * (1 - Math.Clamp(player.velocity.Length() * 0.2f, 0, 1));
										float oriRo = 2 * player.direction + (player.direction == 1 ? 0 : MathHelper.Pi);
										oriRo = MathHelper.Lerp(oriRo, ro, i / (float)(RecordPos.Count + 1f));
										var tovvv = new Vector2(90, 0).RotatedBy(oriRo) + player.MountedCenter;
										RecordPos[i] = Vector2.Lerp(RecordPos[i], tovvv, valll);
									}

									Projectile.ai[0]++;
									HeadPos += new Vector2(0, 10);
								}
								OldHeadPos = RecordPos[Math.Clamp(RecordPos.Count - 3, 0, 15)];

								if (p.ai[0] == 80) {
									var d = Dust.NewDustPerfect(HeadPos, ModContent.DustType<Skill_2_Dust_1>());
								}

							}
							return;
						}
				}
				Projectile.Kill();
				base.AI();
			}
			public override bool PreDraw(ref Color lightColor) {
				var sb = Main.spriteBatch;
				var gd = Main.graphics.GraphicsDevice;

				var head = TextureAssets.Projectile[Type].Value;
				var headPos = HeadPos;
				var player = Main.player[Projectile.owner];

				var a = Projectile.timeLeft / 40f;
				if (player != null) {
					{
						Main.spriteBatch.End();
						Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

						var vertices = new List<Vertex>();
						var i = 0f;
						var count = RecordPos.Count + 1;
						var OldPos = RecordPos[0];
						foreach (var pos in RecordPos) {
							Color coordColor = Color.Red * a;
							if (i != 0) {
								//coordColor.A = 0;
								float ro = (pos - OldPos).ToRotation();
								var len = MathHelper.Lerp(15, 30, i / count);

								vertices.Add(new Vertex(pos + new Vector2(len, 0).RotatedBy(ro - MathHelper.PiOver2 * player.direction) - Main.screenPosition,
														new Vector3((float)i / count, 0, 1),
														coordColor));
								vertices.Add(new Vertex(pos + new Vector2(len, 0).RotatedBy(ro + MathHelper.PiOver2 * player.direction) - Main.screenPosition,
														new Vector3((float)i / count, 1, 1),
														coordColor));
							}
							else {


								vertices.Add(new Vertex(pos - Main.screenPosition,
														new Vector3(0, 0.5f, 1),
														coordColor));

							}
							i++;
							OldPos = pos;

						}
						if (vertices.Count >= 3) {
							//Main.NewText(Color.Red.ToVector3());
							gd.Textures[0] = head;
							gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

						}
						Main.spriteBatch.End();
						Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

					}


					sb.Draw(head,
						headPos - Main.screenPosition,
						null,
						Color.Red * a,
						Projectile.rotation,
						new Vector2(212, 115),
						0.3f,
						player.direction == 1 ? default : SpriteEffects.FlipVertically,
						0);
				}
				// Main.NewText(Projectile.direction);
				return false;
			}
		}
		private class Skill_2_Attack_Proj : ModProjectile
		{
			public override void SetDefaults() {
				Projectile.width = 30;
				Projectile.height = 30;
				Projectile.scale = 1f;
				Projectile.DamageType = DamageClass.Melee;
				Projectile.friendly = true; // 友方弹幕
				Projectile.aiStyle = -1; // 不使用默认的 AI 样式，自定义弹幕行为
				Projectile.tileCollide = false;//false就能让他穿墙
				Projectile.penetrate = -1;//表示能穿透几次
				Projectile.ignoreWater = true;//无视液体
				Projectile.timeLeft = 45;
			}
			public override void SetStaticDefaults() {
				ProjectileID.Sets.TrailingMode[Type] = 2;
				ProjectileID.Sets.TrailCacheLength[Type] = 10;
				base.SetStaticDefaults();
			}

			private Queue<NPC> Target = new Queue<NPC>();
			private static Texture2D[] Tex = new Texture2D[4];
			public override void Load() {
				Tex[0] = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
				for (int i = 1; i < Tex.Length; i++) {
					Tex[i] = ModContent.Request<Texture2D>(Texture + "_" + (i + 1), AssetRequestMode.ImmediateLoad).Value;
				}
				base.Load();
			}
			private int T_Count = 0;
			public override void OnSpawn(IEntitySource source) {
				T_Count = Main.rand.Next(0, 4);
				RecordCenter = Projectile.Center;
				base.OnSpawn(source);
			}
			private Vector2 RecordCenter = Vector2.Zero;
			public override void AI() {
				Projectile.velocity *= 0.9f;
				Projectile.rotation = Projectile.velocity.ToRotation();
				Projectile.ai[0]++;
				/*switch (T_Count)
				{
					case 0:
						{

						}
						break;
				}*/

				base.AI();
			}
			public override bool PreDraw(ref Color lightColor) {
				var sb = Main.spriteBatch;
				var gd = Main.graphics.GraphicsDevice;
				{

					var vertices = new List<Vertex>();

					if (Projectile.ai[0] != 0) {
						sb.End();
						sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

						for (float i = 0; i <= 1; i += 0.1f) {
							var p = Vector2.Lerp(Projectile.Center, RecordCenter, i) - Main.screenPosition;
							var col = Color.Lerp(Color.White, Color.Red, Math.Clamp(i * 2, 0, 1)) * (Projectile.timeLeft / 20f);
							// col = Color.Lerp(col, Color.Orange, Math.Clamp((i - 0.3f) * 2, 0, 1))
							vertices.Add(new Vertex(p + new Vector2(0, -23).RotatedBy(Projectile.rotation), new Vector3((float)i, 0, 0), col));

							vertices.Add(new Vertex(p + new Vector2(0, 23).RotatedBy(Projectile.rotation), new Vector3((float)i, 1, 0), col));

							//Main.NewText(p);
						}
						if (vertices.Count >= 3) {
							//Main.NewText(Color.Red.ToVector3());
							gd.Textures[0] = SwordLightTex_8;
							gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

						}
						sb.End();
						sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

					}

				}


				var t = Tex[T_Count];
				var a = Projectile.timeLeft / 25f;
				var eff = Projectile.velocity.X > 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
				for (int i = Projectile.oldPos.Length - 2; i >= 0; i--) {
					float aa = i / (float)Projectile.oldPos.Length;
					sb.Draw(t, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, null, Color.White * a * aa, Projectile.rotation + MathHelper.Pi, t.Size() * 0.5f, 0.45f, eff, 0);
				}
				return false;
			}
			public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
				Target.Enqueue(target);
				base.OnHitNPC(target, hit, damageDone);
			}
			public override bool? CanHitNPC(NPC target) {
				return !Target.Contains(target);
			}
		}
		private Vector2 AttackPos = Vector2.Zero;
		private void Skill_2_AI() {
			var rand = Main.rand;
			player.heldProj = Projectile.whoAmI;
			Projectile.Center = player.MountedCenter + new Vector2(5).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + MathHelper.Pi);
			player.itemAnimation = player.itemTime = 6;
			Swing_DrawScale = new Vector2(1);
			//Main.NewText(player.MountedCenter);

			if (!player.controlUseItem) {
				if (Projectile.localAI[0] == 0 && Projectile.ai[0] > 80) {
					if (Vector2.Distance(Main.MouseWorld, player.MountedCenter) > 500)
						AttackPos = player.MountedCenter + new Vector2(500, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation());
					else
						AttackPos = Main.MouseWorld;

					Projectile.timeLeft = 150;
				}
				Projectile.localAI[0]++;
			}
			if (Projectile.localAI[0] != 0) {
				if (Projectile.ai[0] > 80) {
					player.velocity *= 0;
					Swing_DrawScale = Vector2.Zero;
					player.immuneAlpha = 255;
					var ScrPla = player.GetModPlayer<Skill_2_Player>();
					ScrPla.SetScreenPos(AttackPos);
					ScrPla.CanBeDamaged = false;
					if (Projectile.timeLeft % 12 == 0 && Projectile.ai[2] < 10) {
						Projectile.ai[2]++;
						var AttackRo = rand.NextFloat(-0.9f, 0.9f);
						var AttackDir = rand.NextBool() ? 1 : -1;
						var AttackStartPos = AttackPos + new Vector2(110 * AttackDir, 0).RotatedBy(AttackRo);
						var AttackStartPos_Vel = new Vector2(-30 * AttackDir, 0).RotatedBy(AttackRo).RotatedByRandom(0.2);

						var p = Projectile.NewProjectileDirect(player.GetSource_FromThis(), AttackStartPos, AttackStartPos_Vel, ModContent.ProjectileType<Skill_2_Attack_Proj>(), 50, 1);
						float dust_RandRo = rand.NextFloat(-0.3f, 0.3f);
						for (int dustC = 0; dustC < 30; dustC++) {
							int dt = 222 + (rand.NextBool(3) ? -3 : 0);
							var u = Dust.NewDustPerfect(AttackPos, dt);
							u.noGravity = true;
							u.velocity = AttackStartPos_Vel.RotatedBy(dust_RandRo).RotatedByRandom(0.4) * 0.2f * rand.NextFloat(0.5f, 1.5f);
							u.fadeIn = 0.4f;
							u.scale = 0.5f;
						}
						//圆圈
						{
							var randVec = new Vector2(rand.NextFloat(-20, 20)).RotatedByRandom(7);
							var u = Dust.NewDustPerfect(AttackPos + randVec, ModContent.DustType<Skill_2_Dust_4>());
							u.fadeIn = rand.NextFloat(0.15f, 0.3f);
						}
						//刀锋
						{
							var randVec = new Vector2(rand.NextFloat(-20, 20)).RotatedByRandom(7);
							var d = Dust.NewDustPerfect(AttackPos + randVec, ModContent.DustType<Skill_2_Dust_2>());
							d.velocity = AttackRo.ToRotationVector2();
						}
					}
					//Main.NewText(Projectile.ai[2]);
					if (Projectile.ai[2] == 10 && Projectile.timeLeft == 10) {
						var AttackRo = 0f;
						var AttackDir = player.MountedCenter.X < AttackPos.X ? 1 : -1;
						var AttackStartPos = AttackPos + new Vector2(120 * AttackDir, 0).RotatedBy(AttackRo);
						var AttackStartPos_Vel = new Vector2(-35 * AttackDir, 0).RotatedBy(AttackRo).RotatedByRandom(0.2);
						for (int dustC = -30; dustC < 30; dustC++) {
							int dt = 222 + (rand.NextBool(3) ? -3 : 0);

							var u = Dust.NewDustPerfect(AttackPos, dt);
							u.noGravity = true;
							u.velocity = new Vector2(10 * dustC / (float)(Math.Abs(dustC) + 1), 0).RotatedByRandom(0.6) * 2 * rand.NextFloat(0.5f, 1.5f);
							u.fadeIn = 0.4f;
							u.scale = 0.6f;
						}
						//刀锋切割
						{
							var d = Dust.NewDustPerfect(AttackPos, ModContent.DustType<Skill_2_Dust_2>());
							d.rotation = AttackRo;
							d.fadeIn = 0.05f;
							d.rotation = 5;
							d.velocity = new Vector2(1, 0);
						}
						//箭头
						{
							for (int i = -1; i <= 1; i += 2) {
								var u = Dust.NewDustPerfect(AttackPos, ModContent.DustType<Skill_2_Dust_3>());
								u.velocity = new Vector2(15 * i, 0);
								u.color = Color.Orange;
							}
						}

						//圆圈
						{
							var u = Dust.NewDustPerfect(AttackPos, ModContent.DustType<Skill_2_Dust_4>());
							u.fadeIn = 0.8f;
						}

						var p = Projectile.NewProjectileDirect(player.GetSource_FromThis(), AttackStartPos, AttackStartPos_Vel, ModContent.ProjectileType<Skill_2_Attack_Proj>(), 50, 1);

					}
				}
			}
			else {
				if (Projectile.ai[0] == 0)
					Projectile.NewProjectile(player.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Dragon_Proj>(), 10, 1, player.whoAmI);
				Projectile.timeLeft = 10;
				Projectile.ai[0]++;
				if (player.MountedCenter.X < Main.MouseWorld.X) {
					if (player.direction == -1) {
						Projectile.rotation += MathHelper.TwoPi * 2;
					}
					player.direction = 1;
				}
				else {
					if (player.direction == 1) {
						Projectile.rotation -= MathHelper.TwoPi * 2;
					}
					player.direction = -1;
				}
				var st = Math.Clamp(Projectile.ai[0] / 60f * 0.14f, 0, 1);
				Projectile.rotation = MathHelper.Lerp(Projectile.rotation, 10 * player.direction, st);

			}
		}
		private void Skill_2_Draw(GraphicsDevice gd) {
			TextureAssets.Projectile[Type] = sw_Proj_2_t2;
			var vertices = new List<Vertex>();
			var count = Projectile.oldRot.Length - 1;
			//var Vertex_Num = 0.1f;

			for (float i = 0; i < count; i++) {
				//for (float j = 0.0f; j < 1f; j += Vertex_Num)
				{
					var coordColor = Color.Lerp(Color.Orange, Color.DarkRed, (float)Math.Clamp(i / 6f, 0, 1));

					float ro = Projectile.oldRot[(int)i];//.AngleLerp(Projectile.oldRot[(int)i + 1], j);

					if (i == 0)
						ro = Projectile.rotation;

					if (player.direction == 1) {
						vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 77f,
						new Vector3((float)i / count, 1, 1),
						coordColor));
						vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 50f,
												new Vector3((float)i / count, 0, 1),
												coordColor));
					}
					else {
						vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 50f,
												new Vector3((float)i / count, 0, 1),
												coordColor));
						vertices.Add(new Vertex(Projectile.Center - Main.screenPosition + new Vector2(0, -1).RotatedBy(ro) * 77f,
						new Vector3((float)i / count, 1, 1),
						coordColor));

					}
					//Main.NewText(a);
				}


			}
			if (vertices.Count >= 3) {
				//Main.NewText(Color.Red.ToVector3());
				gd.Textures[0] = SwordLightTex_8;
				gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);

			}

			Projectile.KZ_QuicklyDraw_Proj(Swing_DrawScale);
			TextureAssets.Projectile[Type] = sw_Proj_2_old;
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
			//Skill_1_Draw();
			Skill_2_Draw(gd);

			return false;
		}
	}
}