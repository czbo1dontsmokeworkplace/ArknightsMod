using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Lupine
{
	// ============================================================
	//  狼之绯 挥砍弹幕（独立实现）
	//  炫酷刀光 = 月牙刀光（Slash 贴图，多层加色 + 缩放弹出）
	//            + 干净带状 ribbon（沿刀身扫过路径，KnifeLight 着色器）
	//  红色气雾 = 月牙外层红晕 + 红紫尘埃
	//  —— 手持贴图 LupineScarlet_protile（横向，尖右柄左）
	// ============================================================
	public class LupineScarletSwingProjectile : ModProjectile
	{
		private const string RibbonShapePath = "ArknightsMod/Content/Projectiles/Guard/Lupine/LupineRibbon";
		private const string SlashPath       = "ArknightsMod/Content/Projectiles/Guard/Lupine/LupineSlash";
		private const string BladeTexPath    = "ArknightsMod/Content/Items/Weapons/Guard/Lupine/LupineScarlet_protile";

		// === 可调常量（视觉微调入口）===
		private const float CrescentRotOffset = MathHelper.PiOver2; // 月牙朝向修正
		private const float CrescentLength    = 0.55f;              // 月牙沿弧长缩放
		private const float CrescentThick     = 0.85f;              // 月牙厚度缩放
		private const float RibbonWidthMul    = 1.0f;               // ribbon 宽度系数

		private const int TrailLength = 22;
		private const float DisFromPlayer = 6f;

		// 挥砍状态
		private Vector2 mainVec;
		private Vector2[] trailVec;
		private int Timer;
		private int attackType;
		private const int maxAttackType = 2;
		private bool isAttacking;
		private bool oldIsAttacking;
		private bool UseTrail = true;
		private int killTimer;
		private bool hitThisAttack;
		private float lockedDir = 1f;

		// 当前段攻击的活跃窗口（用于月牙渐隐）
		private int segActiveStart;
		private int segActiveEnd;

		// 程序生成主刀光渐变（白→紫）
		private static Texture2D _mainColorTex;

		Player Owner => Main.player[Projectile.owner];
		public override string Texture => BladeTexPath;

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 15;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 30;
			Projectile.scale = 1.15f;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
			Projectile.DamageType = DamageClass.Melee;
			trailVec = new Vector2[TrailLength];
		}

		public override bool ShouldUpdatePosition() => false;

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write(attackType);
			writer.Write(lockedDir);
			writer.WriteVector2(mainVec);
		}
		public override void ReceiveExtraAI(BinaryReader reader) {
			attackType = reader.ReadInt32();
			lockedDir = reader.ReadSingle();
			mainVec = reader.ReadVector2();
		}

		// ── AI ────────────────────────────────────────────
		public override void AI() {
			Player p = Owner;
			if (!p.active || p.dead || p.noItems || p.CCed) { Projectile.Kill(); return; }

			p.heldProj = Projectile.whoAmI;
			Projectile.Center = p.MountedCenter + Normalize(mainVec) * DisFromPlayer;
			Projectile.timeLeft = 30;
			isAttacking = false;

			p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
				mainVec.ToRotation() - MathHelper.PiOver2);

			Attack(p);
			Timer++;

			bool shouldEnd = !isAttacking && !p.controlUseItem;
			if (shouldEnd) {
				killTimer++;
				if (killTimer > 8) { Projectile.Kill(); return; }
			} else {
				killTimer = 0;
			}

			if (isAttacking) {
				p.direction = (int)lockedDir;
				Projectile.spriteDirection = (int)lockedDir;
			}
			if (!oldIsAttacking && isAttacking) hitThisAttack = false;
			oldIsAttacking = isAttacking;

			if (UseTrail) {
				for (int i = TrailLength - 1; i > 0; i--) trailVec[i] = trailVec[i - 1];
				trailVec[0] = mainVec;
			} else {
				Array.Clear(trailVec, 0, TrailLength);
			}
		}

		private void LockDirFromMouse(Player p) {
			if (Main.myPlayer != Projectile.owner) return;
			float newDir = Main.MouseWorld.X > p.Center.X ? 1f : -1f;
			if (newDir != lockedDir) { lockedDir = newDir; Projectile.netUpdate = true; }
		}

		private void NextAttackType() {
			Timer = 0;
			attackType++;
			if (attackType > maxAttackType) attackType = 0;
		}

		// ── 三段连击 ──────────────────────────────────────
		private void Attack(Player p) {
			UseTrail = true;
			float dir = lockedDir;

			if (attackType == 0) {
				segActiveStart = 14; segActiveEnd = 34;
				if (Timer == 14) AttSound(SoundID.Item1);
				if (Timer < 14) {
					UseTrail = false; LockDirFromMouse(p); dir = lockedDir;
					float target = -MathHelper.PiOver2 - dir * 0.7f;
					mainVec = Vector2.Lerp(mainVec, target.ToRotationVector2() * 105f, 0.18f);
					mainVec += Normalize(mainVec) * 3f;
					Projectile.rotation = mainVec.ToRotation();
				}
				if (Timer > 14 && Timer < 34) {
					isAttacking = true;
					Projectile.rotation += dir * 0.38f;
					mainVec = Projectile.rotation.ToRotationVector2() * 108f;
					SpawnSwingDust(p);
				}
				if (Timer > 34) NextAttackType();
			}

			if (attackType == 1) {
				segActiveStart = 10; segActiveEnd = 30;
				if (Timer == 10) AttSound(SoundID.Item1);
				if (Timer < 10) {
					UseTrail = false; LockDirFromMouse(p); dir = lockedDir;
					float target = MathHelper.PiOver2 + dir * 0.9f;
					mainVec = Vector2.Lerp(mainVec, target.ToRotationVector2() * 108f, 0.22f);
					mainVec += Normalize(mainVec) * 3f;
					Projectile.rotation = mainVec.ToRotation();
				}
				if (Timer > 10 && Timer < 30) {
					isAttacking = true;
					Projectile.rotation -= dir * 0.44f;
					mainVec = Projectile.rotation.ToRotationVector2() * 112f;
					SpawnSwingDust(p);
				}
				if (Timer > 30) NextAttackType();
			}

			if (attackType == 2) {
				segActiveStart = 18; segActiveEnd = 42;
				if (Timer == 18) AttSound(SoundID.Item1);
				if (Timer < 18) {
					UseTrail = false; LockDirFromMouse(p); dir = lockedDir;
					float target = -MathHelper.PiOver2 * 1.1f - dir * 0.5f;
					mainVec = Vector2.Lerp(mainVec, target.ToRotationVector2() * 112f, 0.12f);
					mainVec += Normalize(mainVec) * 3f;
					Projectile.rotation = mainVec.ToRotation();
				}
				if (Timer > 18 && Timer < 42) {
					isAttacking = true;
					Projectile.rotation += dir * 0.32f;
					mainVec = Projectile.rotation.ToRotationVector2() * 118f;
					SpawnSwingDust(p);
				}
				if (Timer > 42) { attackType = -1; NextAttackType(); }
			}
		}

		private void AttSound(SoundStyle s) => SoundEngine.PlaySound(s, Projectile.Center);

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (!isAttacking) return false;
			float point = 0f;
			if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
					Projectile.Center + mainVec * Projectile.scale * 0.1f,
					Projectile.Center + mainVec * Projectile.scale,
					Projectile.height * 1.5f, ref point))
				return true;
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			hitThisAttack = true;
			modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
		}

		public override void CutTiles() {
			if (!isAttacking) return;
			DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
			Terraria.Utils.PlotTileLine(Projectile.Center, Projectile.Center + mainVec,
				Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
		}

		// ── 挥砍尘埃（红紫气焰）──────────────────────────
		private void SpawnSwingDust(Player p) {
			if (!Main.rand.NextBool(2)) return;
			for (int i = 3; i < 6; i++) {
				if (!Main.rand.NextBool()) continue;
				float frac = i / 5f;
				Vector2 spawnPos = Projectile.Center + mainVec * frac;
				bool red = Main.rand.NextBool(3);
				int dustType = red ? DustID.RedTorch : DustID.PurpleTorch;
				Color dustColor = red ? new Color(255, 50, 50) : new Color(180, 80, 255);
				Dust d = Dust.NewDustDirect(spawnPos, 6, 6, dustType, 0f, 0f, 0, dustColor,
					Main.rand.NextFloat(0.9f, 1.4f));
				d.noGravity = true;
				d.velocity = (mainVec.ToRotation() + lockedDir * MathHelper.PiOver2).ToRotationVector2()
					* Main.rand.NextFloat(1.0f, 2.6f);
				d.velocity += Main.rand.NextVector2Circular(0.7f, 0.7f);
			}
		}

		// ── 绘制 ──────────────────────────────────────────
		public override bool PreDraw(ref Color lightColor) {
			if (Main.dedServ) return false;
			DrawRibbon();        // 连续带状拖尾（底）
			DrawCrescent();      // 月牙刀光（炫酷主体）
			DrawBlade();         // 刀身
			return false;
		}

		private float TrailAlpha(float factor) {
			float smooth = MathHelper.SmoothStep(0f, 1f, factor);
			return MathHelper.Lerp(0.02f, 1.1f, smooth);
		}

		// 带状 ribbon：沿刀身扫过路径，白→紫，KnifeLight 着色器
		private void DrawRibbon() {
			if (ArknightsMod.LupineKnifeLight?.Value == null) return;
			Texture2D colorTex = GetMainColorTex();
			if (colorTex == null) return;

			float counts = 0f;
			for (int i = 0; i < trailVec.Length; i++) if (trailVec[i] != Vector2.Zero) counts += 1f;
			if (counts < 2f) return;

			var bars = new List<Vertex>();
			for (int j = 0; j < trailVec.Length; j++) {
				if (trailVec[j] == Vector2.Zero) continue;
				float factor = 1f - j / counts;
				float w = TrailAlpha(factor) * RibbonWidthMul;
				Vector2 inner = Projectile.Center + trailVec[j] * 0.15f * Projectile.scale;
				Vector2 outer = Projectile.Center + trailVec[j] * Projectile.scale;
				bars.Add(new Vertex(inner, new Vector3(factor, 1f, 0f), Color.White));
				bars.Add(new Vertex(outer, new Vector3(factor, 0f, w), Color.White));
			}
			if (bars.Count < 3) return;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone);

			Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
			Matrix model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f))
				* Main.GameViewMatrix.ZoomMatrix;
			Effect fx = ArknightsMod.LupineKnifeLight.Value;
			fx.Parameters["uTransform"].SetValue(model * projection);
			fx.Parameters["tex0"].SetValue(ModContent.Request<Texture2D>(RibbonShapePath).Value);
			fx.Parameters["tex1"].SetValue(colorTex);
			fx.CurrentTechnique.Passes["Trail0"].Apply();

			Main.graphics.GraphicsDevice.DrawUserPrimitives(
				PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
				null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 月牙刀光：多层加色（红晕 + 紫体 + 白芯），缩放弹出 + 渐隐
		private void DrawCrescent() {
			// 段内进度（0→1），峰值在中段
			float denom = Math.Max(1, segActiveEnd - segActiveStart);
			float prog = MathHelper.Clamp((Timer - segActiveStart) / denom, 0f, 1f);
			if (Timer < segActiveStart - 2 || Timer > segActiveEnd + 4) return;
			float fade = (float)Math.Sin(prog * Math.PI);           // 0→1→0
			if (fade <= 0.01f) return;
			float pop = MathHelper.Lerp(0.7f, 1.25f, prog);          // 逐渐展开

			Texture2D slash = ModContent.Request<Texture2D>(SlashPath).Value;
			Vector2 origin = slash.Size() * 0.5f;
			Vector2 pos = Projectile.Center + mainVec * 0.5f - Main.screenPosition;
			float rot = mainVec.ToRotation() + CrescentRotOffset;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
				null, Main.GameViewMatrix.TransformationMatrix);

			// 基础缩放：把贴图(128x256)映射到弧长/厚度
			float baseY = mainVec.Length() * 2f / slash.Height * CrescentLength;
			float baseX = mainVec.Length() / slash.Width * CrescentThick;

			// 红色气雾外晕（最大、最柔）
			Main.spriteBatch.Draw(slash, pos, null, new Color(255, 40, 40, 0) * (fade * 0.45f),
				rot, origin, new Vector2(baseX * 1.35f, baseY * 1.12f) * pop, SpriteEffects.None, 0f);
			// 紫色主体
			Main.spriteBatch.Draw(slash, pos, null, new Color(165, 65, 255, 0) * (fade * 0.95f),
				rot, origin, new Vector2(baseX, baseY) * pop, SpriteEffects.None, 0f);
			// 白色亮芯（细、最亮）
			Main.spriteBatch.Draw(slash, pos, null, new Color(255, 235, 255, 0) * fade,
				rot, origin, new Vector2(baseX * 0.55f, baseY * 0.98f) * pop, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
				null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 刀身：横向 protile（尖右柄左），柄锚定在玩家手部
		private void DrawBlade() {
			Texture2D tex = ModContent.Request<Texture2D>(BladeTexPath).Value;
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied,
				Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
				null, Main.GameViewMatrix.ZoomMatrix);

			bool faceLeft = lockedDir < 0;
			SpriteEffects fx = faceLeft ? SpriteEffects.FlipVertically : SpriteEffects.None;
			float rot = mainVec.ToRotation();
			Vector2 origin = new Vector2(0f, tex.Height / 2f); // 柄在左中
			Color col = Lighting.GetColor((int)(Projectile.Center.X / 16), (int)(Projectile.Center.Y / 16));
			float lenScale = mainVec.Length() / tex.Width; // 刀长贴合攻击距离

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, col, rot,
				origin, new Vector2(lenScale, 1f) * Projectile.scale, fx, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
				null, Main.GameViewMatrix.TransformationMatrix);
		}

		// ── 程序生成主刀光渐变（faint紫 → 亮紫 → 白核）──
		private static Texture2D GetMainColorTex() {
			if (_mainColorTex != null) return _mainColorTex;
			if (Main.dedServ || Main.instance?.GraphicsDevice == null) return null;
			const int w = 256;
			var tex = new Texture2D(Main.instance.GraphicsDevice, w, 1);
			var data = new Color[w];
			for (int x = 0; x < w; x++) {
				float t = x / (w - 1f);
				Color c;
				if (t < 0.45f) c = Color.Lerp(new Color(40, 0, 80), new Color(150, 50, 240), t / 0.45f);
				else if (t < 0.78f) c = Color.Lerp(new Color(150, 50, 240), new Color(210, 150, 255), (t - 0.45f) / 0.33f);
				else c = Color.Lerp(new Color(210, 150, 255), new Color(255, 255, 255), (t - 0.78f) / 0.22f);
				float alphaT = MathHelper.Clamp((t - 0.08f) / 0.92f, 0f, 1f);
				alphaT *= alphaT;
				byte a = (byte)MathHelper.Clamp(255f * alphaT * 1.4f, 0f, 255f);
				data[x] = new Color(c.R, c.G, c.B, a);
			}
			tex.SetData(data);
			_mainColorTex = tex;
			return tex;
		}

		private static Vector2 Normalize(Vector2 v) => v == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(v);

		private struct Vertex : IVertexType {
			private static readonly VertexDeclaration _decl = new VertexDeclaration(
				new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
				new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
				new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));
			public Vector2 Position;
			public Color Color;
			public Vector3 TexCoord;
			public Vertex(Vector2 position, Vector3 texCoord, Color color) {
				Position = position; TexCoord = texCoord; Color = color;
			}
			public VertexDeclaration VertexDeclaration => _decl;
		}
	}
}
