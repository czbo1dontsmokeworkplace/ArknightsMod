using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Valarqvin
{
	public class ValarqvinProj3_Hit : ModProjectile
	{
		// 这个弹丸的总帧数，同时也是各种光效的持续时间基准
		private const int TotalFrames = 25;
		// 十字光效的三个关键时间点：峰值（快速拉伸结束）、收缩结束（之后缓慢变小）
		private const int PeakFrame = 12;
		private const int ShrinkEndFrame = 16;

		// 主十字光效的尺寸参数，控制它从出现到消失的宽高变化
		private const float MainStartWidth = 28f;
		private const float MainStartHeight = 8f;
		private const float MainPeakWidth = 65f;
		private const float MainPeakHeight = 16f;
		private const float MainShrinkWidth = 45f;
		private const float MainShrinkHeight = 11f;
		private const float MainEndWidth = 8f;
		private const float MainEndHeight = 3f;

		// 外光晕相对于主光晕的缩放倍数，同样随时间变化
		private const float OuterScaleStart = 1.8f;
		private const float OuterScalePeak = 1.6f;
		private const float OuterScaleShrink = 1.4f;
		private const float OuterScaleEnd = 1.1f;

		// 十字光效的随机角度偏移，让每次出现的角度略有不同，不那么死板
		private const float CrossAngleError = 0.15f;

		// 主十字光效的颜色：亮蓝白 + 深蓝紫
		private static readonly Color MainColor = new Color(150, 180, 255);
		private static readonly Color OuterColor = new Color(68, 90, 172);

		// 更大一圈的十字光效，尺寸是原来的1.8倍，颜色更淡，用来做外围光晕
		private const float LargerCrossScale = 1.8f;
		private const float LargerCrossAlpha = 0.25f;
		private static readonly Color LargerCrossColor = new Color(190, 220, 255);
		private const float LargerCrossAngleOffset = 0.4f;

		// 中心冲击波光效：一个先快速放大再缓慢缩小的圆形光晕
		private const int GlowTotalFrames = 20;
		private const int GlowPeakFrame = 8;
		private const float GlowStartSize = 15f;
		private const float GlowPeakSize = 55f;
		private const float GlowEndSize = 5f;
		private const float GlowOuterScale = 2.2f;
		private static readonly Color GlowMainColor = new Color(180, 210, 255);
		private static readonly Color GlowOuterColor = new Color(68, 90, 172);

		// 更大的圆形光晕，覆盖范围更广，用来营造扩散感
		private const int LargeGlowTotalFrames = 18;
		private const float LargeGlowStartSize = 40f;
		private const float LargeGlowPeakSize = 130f;
		private const float LargeGlowEndSize = 10f;
		private const float LargeGlowAlpha = 0.8f;
		private static readonly Color LargeGlowColor = new Color(180, 210, 255);

		// 纯黑色的十字光效，用 AlphaBlend 叠在上面，形成一个短暂的暗色冲击轮廓
		private const int DarkCrossStartFrame = 3;
		private const int DarkCrossDuration = 8;
		private const float DarkCrossStartWidth = 45f;
		private const float DarkCrossStartHeight = 10f;
		private const float DarkCrossEndWidth = 10f;
		private const float DarkCrossEndHeight = 3f;
		private const float DarkCrossAlpha = 0.55f;
		private static readonly Color DarkCrossColor = Color.Black;

		// 各种粒子的数量
		private const int LightParticleCount = 10;
		private const int HitParticleCount = 15;
		// 普通多边形粒子（不是科技风那种，就是简单的爆炸碎片多边形）
		private const int PolyParticleCount = 12;
		private const int PolyParticleMinLife = 25;
		private const int PolyParticleMaxLife = 45;

		// 随机乱飞的小粒子
		private const int RandomParticleCount = 15;
		private const int RandomParticleMinLife = 15;
		private const int RandomParticleMaxLife = 30;

		// 锯齿爆炸的参数：45 根放射状尖刺，最大半径 50 像素
		private const int ExplosionSpikeCount = 45;
		private const float ExplosionMaxRadius = 50f;
		private const float ExplosionSpikeMinLength = 0.25f;
		private const float ExplosionSpikeMaxLength = 1.0f;
		// 沿弹丸飞行方向拉伸的强度
		private const float ExplosionDirectionWeight = 0.4f;

		// 羽化消散蒙版：从第 3 帧开始，一个逐渐扩大的圆把爆炸中心镂空
		private const int MaskStartFrame = 3;
		private const float MaskExpandSpeed = 2.5f;
		private const float MaskFeatherWidth = 0.6f;

		// 爆炸的填充色和边缘色，深蓝黑
		private static readonly Color ExplosionFillColor = new Color(12, 20, 40);
		private static readonly Color ExplosionEdgeColor = new Color(30, 45, 80);
		private const float ExplosionAlpha = 0.65f;

		// 多边形粒子可用的颜色，和 Proj2 的多边形粒子保持一致
		private static readonly Color[] PolyColors = new Color[]
		{
			new Color(0, 200, 255),
			new Color(0, 150, 255),
			new Color(30, 80, 220),
			new Color(0, 100, 200),
			new Color(20, 60, 180),
			new Color(0, 180, 230),
		};

		// 小光点粒子结构
		private struct LightParticle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Size;
			public float MaxSize;
			public int Life;
			public int MaxLife;
			public Color Color;
			public bool Active => Life > 0;
		}

		// 普通多边形粒子结构，边长和颜色会随时间变化
		private struct PolyParticle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Size;
			public float MaxSize;
			public int Sides;
			public float Rotation;
			public float RotationSpeed;
			public Color Color;
			public int Life;
			public int MaxLife;
			public bool Active => Life > 0;
		}

		// 随机散逸粒子结构
		private struct RandomParticle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Size;
			public float MaxSize;
			public int Life;
			public int MaxLife;
			public Color Color;
			public bool Active => Life > 0;
		}

		// 锯齿爆炸的数据：存储每根尖刺的末端相对坐标和长度比例
		private struct ExplosionData
		{
			public Vector2[] SpikeEnds;
			public float[] SpikeLengths;
		}

		// 贴图
		private Texture2D _lightTexture;
		private Texture2D _crossTexture;
		// 爆炸特效生成的位置，锁定在这里防止跟随弹丸速度移动
		private Vector2 _spawnPosition;
		// 十字光效的随机基础角度
		private float _crossBaseAngle;
		// 粒子列表
		private List<LightParticle> lightParticles = new List<LightParticle>();
		private List<PolyParticle> polyParticles = new List<PolyParticle>();
		private List<RandomParticle> randomParticles = new List<RandomParticle>();
		// 锯齿爆炸数据
		private ExplosionData _explosionData;
		// 用来画多边形和爆炸的 BasicEffect，需要手动应用视口矩阵
		private BasicEffect _basicEffect;
		// 确保粒子只生成一次
		private bool spawnedParticles = false;

		public override void SetDefaults() {
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = TotalFrames;
			Projectile.penetrate = -1;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			// 第一帧生成所有粒子和爆炸数据
			if (!spawnedParticles) {
				spawnedParticles = true;
				_spawnPosition = Projectile.Center;
				_crossBaseAngle = Main.rand.NextFloat(-CrossAngleError, CrossAngleError);
				InitializeExplosion();
				SpawnParticles();
			}

			// 更新小光点粒子：移动、减速、快速缩小
			for (int i = lightParticles.Count - 1; i >= 0; i--) {
				LightParticle lp = lightParticles[i];
				lp.Life--;
				lp.Position += lp.Velocity;
				lp.Velocity *= 0.94f;
				float progress = 1f - (float)lp.Life / lp.MaxLife;
				lp.Size = lp.MaxSize / (1f + progress * 3f);
				lightParticles[i] = lp;
				if (!lp.Active)
					lightParticles.RemoveAt(i);
			}

			// 更新多边形粒子：移动、减速、旋转、边数减少、颜色变换、微抖动
			for (int i = polyParticles.Count - 1; i >= 0; i--) {
				PolyParticle pp = polyParticles[i];
				pp.Life--;
				pp.Position += pp.Velocity;
				pp.Velocity *= 0.96f;
				pp.Rotation += pp.RotationSpeed;

				float progress = 1f - (float)pp.Life / pp.MaxLife;
				pp.Size = pp.MaxSize / (1f + progress * 2f);

				// 每隔 8 帧边数减一，同时随机换色
				if (pp.Life % 8 == 0 && pp.Sides > 3) {
					pp.Sides--;
					pp.Color = PolyColors[Main.rand.Next(PolyColors.Length)];
				}

				// 给粒子加一点正弦抖动，看起来更生动
				pp.Position += new Vector2(
					(float)Math.Sin(pp.Life * 0.3f) * 0.4f,
					(float)Math.Cos(pp.Life * 0.5f) * 0.4f);

				polyParticles[i] = pp;
				if (!pp.Active)
					polyParticles.RemoveAt(i);
			}

			// 更新随机散逸粒子：移动、减速、线性缩小
			for (int i = randomParticles.Count - 1; i >= 0; i--) {
				RandomParticle rp = randomParticles[i];
				rp.Life--;
				rp.Position += rp.Velocity;
				rp.Velocity *= 0.92f;
				float progress = 1f - (float)rp.Life / rp.MaxLife;
				rp.Size = rp.MaxSize * (1f - progress);
				randomParticles[i] = rp;
				if (!rp.Active)
					randomParticles.RemoveAt(i);
			}
		}

		// 初始化锯齿爆炸：生成 45 根尖刺，长短由噪声决定，飞行方向拉伸
		private void InitializeExplosion() {
			_explosionData = new ExplosionData();
			_explosionData.SpikeEnds = new Vector2[ExplosionSpikeCount];
			_explosionData.SpikeLengths = new float[ExplosionSpikeCount];

			float angleStep = MathHelper.TwoPi / ExplosionSpikeCount;
			Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float baseAngle = direction.ToRotation();

			for (int i = 0; i < ExplosionSpikeCount; i++) {
				float angle = i * angleStep;

				// 用多层正弦波叠加制造自然的锯齿凹凸
				float noise = MathF.Sin(i * 1.7f + 0.5f) * 0.35f
							+ MathF.Sin(i * 4.1f + 2.3f) * 0.25f
							+ MathF.Cos(i * 2.5f + 1.1f) * 0.2f
							+ MathF.Sin(i * 6.7f + 3.8f) * 0.15f;
				if (Main.rand.NextFloat() < 0.3f)
					noise += Main.rand.NextFloat(-0.3f, 0.4f);

				float height = MathHelper.Clamp(0.5f + noise, 0.1f, 1.0f);
				float lengthRatio = MathHelper.Lerp(ExplosionSpikeMinLength, ExplosionSpikeMaxLength, height);

				// 让飞行前方的尖刺更长，后方的更短
				float angleDiff = angle - baseAngle;
				float dirFactor = 1f + ExplosionDirectionWeight * MathF.Cos(angleDiff);
				lengthRatio *= MathHelper.Clamp(dirFactor, 0.5f, 1.5f);

				_explosionData.SpikeLengths[i] = lengthRatio;

				float r = ExplosionMaxRadius * lengthRatio;
				_explosionData.SpikeEnds[i] = new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
			}
		}

		// 生成所有粒子：HitParticle 用独立的弹丸，光点和多边形用结构体自己管理
		private void SpawnParticles() {
			Vector2 hitPos = _spawnPosition;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float baseSpeed = Projectile.velocity.Length();

			// HitParticle：扇形散开的蓝色形变粒子，用独立的弹丸实现
			for (int i = 0; i < HitParticleCount; i++) {
				float angle = dir.ToRotation() + Main.rand.NextFloat(-1.0f, 1.0f);
				float speed = baseSpeed * Main.rand.NextFloat(0.5f, 0.9f);
				Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

				int life = Main.rand.Next(35, 55);
				float size = Main.rand.NextFloat(12f, 22f);
				Color col = new Color(68, 90, 172).MultiplyRGB(Color.White * Main.rand.NextFloat(0.8f, 1f));

				Projectile p = Projectile.NewProjectileDirect(
					Projectile.GetSource_FromThis(), hitPos, vel,
					ModContent.ProjectileType<ValarqvinHitParticle>(), 0, 0f, Projectile.owner);

				if (p.ModProjectile is ValarqvinHitParticle particle)
					particle.Initialize(life, size, 2f, vel, hitPos, col);
			}

			// 小光点粒子
			for (int i = 0; i < LightParticleCount; i++) {
				LightParticle lp = new LightParticle();
				lp.Position = hitPos + Main.rand.NextVector2Circular(8f, 8f);
				lp.Velocity = Main.rand.NextVector2Circular(0.5f, 1.5f) + dir * Main.rand.NextFloat(1f, 2.5f);
				lp.MaxSize = Main.rand.NextFloat(0.8f, 1.8f);
				lp.Size = lp.MaxSize;
				lp.MaxLife = Main.rand.Next(25, 40);
				lp.Life = lp.MaxLife;
				lp.Color = Color.White;
				lightParticles.Add(lp);
			}

			// 多边形粒子：大小、边数、颜色都随机，扩散速度较大
			for (int i = 0; i < PolyParticleCount; i++) {
				PolyParticle pp = new PolyParticle();
				pp.Position = hitPos + Main.rand.NextVector2Circular(6f, 6f);
				pp.Velocity = Main.rand.NextVector2Circular(3f, 3f) + dir * Main.rand.NextFloat(1.5f, 4f);
				pp.MaxSize = Main.rand.NextFloat(7f, 14f);
				pp.Size = pp.MaxSize;
				pp.Sides = Main.rand.Next(3, 7);
				pp.Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
				pp.RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f);
				pp.Color = PolyColors[Main.rand.Next(PolyColors.Length)];
				pp.MaxLife = Main.rand.Next(PolyParticleMinLife, PolyParticleMaxLife);
				pp.Life = pp.MaxLife;
				polyParticles.Add(pp);
			}

			// 随机散逸粒子：纯随机方向飞走，速度较快
			for (int i = 0; i < RandomParticleCount; i++) {
				RandomParticle rp = new RandomParticle();
				rp.Position = hitPos + Main.rand.NextVector2Circular(5f, 5f);
				rp.Velocity = Main.rand.NextVector2Circular(3f, 3f);
				rp.MaxSize = Main.rand.NextFloat(0.5f, 1.2f);
				rp.Size = rp.MaxSize;
				rp.MaxLife = Main.rand.Next(RandomParticleMinLife, RandomParticleMaxLife);
				rp.Life = rp.MaxLife;
				rp.Color = PolyColors[Main.rand.Next(PolyColors.Length)];
				randomParticles.Add(rp);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			// 加载贴图，找不到就不画
			if (_lightTexture == null)
				_lightTexture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Caster/Valarqvin/Light").Value;
			if (_crossTexture == null)
				_crossTexture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Rogue/Dedication/Light_horizontal").Value;

			if (_lightTexture == null || _crossTexture == null)
				return false;

			int frame = TotalFrames - Projectile.timeLeft;
			if (frame < 0 || frame >= TotalFrames)
				return false;

			// 从最底层到最上层依次绘制
			DrawExplosion(frame);
			DrawCoreGlow(frame);
			DrawLargeGlow(frame);
			DrawLightParticles();
			DrawPolyParticles();
			DrawCrossEffect(frame);
			DrawLargerCrossEffect(frame);
			DrawRandomParticles();
			DrawDarkCross(frame);

			return false;
		}

		// 画锯齿爆炸：用 BasicEffect 提交三角形扇形，AlphaBlend 混合
		private void DrawExplosion(int frame) {
			if (_explosionData.SpikeEnds == null)
				return;

			Main.spriteBatch.End();
			GraphicsDevice gd = Main.graphics.GraphicsDevice;
			BlendState prevBlend = gd.BlendState;
			RasterizerState prevRaster = gd.RasterizerState;
			gd.BlendState = BlendState.AlphaBlend;
			gd.RasterizerState = RasterizerState.CullNone;

			// 设置视口矩阵以适配 UI 缩放和分辨率变化
			if (_basicEffect == null) {
				_basicEffect = new BasicEffect(gd);
				_basicEffect.VertexColorEnabled = true;
			}
			_basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
			_basicEffect.View = Main.GameViewMatrix.TransformationMatrix;
			_basicEffect.World = Matrix.Identity;

			float progress = (float)frame / TotalFrames;
			float globalAlpha = ExplosionAlpha * (1f - progress * progress);

			// EaseOut 缩放：前 35% 时间快速放大到满尺寸
			float scaleT = MathHelper.Clamp(progress / 0.35f, 0f, 1f);
			float scale = 1f - (1f - scaleT) * (1f - scaleT);

			Vector2 screenCenter = _spawnPosition - Main.screenPosition;

			// 羽化蒙版半径
			float maskRadius = 0f;
			if (frame >= MaskStartFrame)
				maskRadius = (frame - MaskStartFrame) * MaskExpandSpeed;

			float featherOuter = maskRadius;
			float featherInner = maskRadius * (1f - MaskFeatherWidth);

			// 计算每根尖刺的屏幕坐标和到中心的距离
			Vector2[] screenVerts = new Vector2[ExplosionSpikeCount];
			float[] distances = new float[ExplosionSpikeCount];
			for (int i = 0; i < ExplosionSpikeCount; i++) {
				screenVerts[i] = screenCenter + _explosionData.SpikeEnds[i] * scale;
				distances[i] = Vector2.Distance(screenVerts[i], screenCenter);
			}

			List<VertexPositionColor> triangles = new List<VertexPositionColor>();

			// 每个三角形扇形：中心 → 当前尖刺 → 下一尖刺
			for (int i = 0; i < ExplosionSpikeCount; i++) {
				int next = (i + 1) % ExplosionSpikeCount;

				float alpha1 = GetMaskAlpha(distances[i], featherOuter, featherInner, globalAlpha);
				float alpha2 = GetMaskAlpha(distances[next], featherOuter, featherInner, globalAlpha);
				float alphaC = GetMaskAlpha(0f, featherOuter, featherInner, globalAlpha);

				if (alphaC < 0.01f && alpha1 < 0.01f && alpha2 < 0.01f)
					continue;

				triangles.Add(new VertexPositionColor(new Vector3(screenCenter, 0), ExplosionFillColor * alphaC));
				triangles.Add(new VertexPositionColor(new Vector3(screenVerts[i], 0), ExplosionEdgeColor * alpha1));
				triangles.Add(new VertexPositionColor(new Vector3(screenVerts[next], 0), ExplosionEdgeColor * alpha2));
			}

			if (triangles.Count >= 3) {
				foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes) {
					pass.Apply();
					gd.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.ToArray(), 0, triangles.Count / 3);
				}
			}

			gd.BlendState = prevBlend;
			gd.RasterizerState = prevRaster;
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 羽化蒙版 Alpha：距离越靠近蒙版内圈越透明
		private float GetMaskAlpha(float dist, float maskOuter, float maskInner, float baseAlpha) {
			if (maskOuter <= 0f)
				return baseAlpha;
			if (dist <= maskInner)
				return 0f;
			if (dist >= maskOuter)
				return baseAlpha;
			float t = (dist - maskInner) / (maskOuter - maskInner);
			return baseAlpha * t;
		}

		// 中心圆形光效：先快速放大再缓慢缩小
		private void DrawCoreGlow(int frame) {
			if (frame >= GlowTotalFrames)
				return;
			if (_lightTexture == null || _lightTexture.IsDisposed)
				return;

			float progress = (float)frame / GlowTotalFrames;
			float glowSize;
			if (frame <= GlowPeakFrame) {
				float t = frame / (float)GlowPeakFrame;
				float easeOut = 1f - (1f - t) * (1f - t);
				glowSize = GlowStartSize + (GlowPeakSize - GlowStartSize) * easeOut;
			}
			else {
				float t = (frame - GlowPeakFrame) / (float)(GlowTotalFrames - 1 - GlowPeakFrame);
				float easeIn = t * t;
				glowSize = GlowPeakSize - (GlowPeakSize - GlowEndSize) * easeIn;
			}

			float alpha = 1f - progress * progress;
			Vector2 screenPos = _spawnPosition - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearClamp, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			// 主光晕
			Color mainColor = GlowMainColor * alpha;
			Rectangle mainRect = new Rectangle(
				(int)(screenPos.X - glowSize * 0.5f),
				(int)(screenPos.Y - glowSize * 0.5f),
				(int)glowSize, (int)glowSize);
			Main.spriteBatch.Draw(_lightTexture, mainRect, null, mainColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);

			// 外光晕
			float outerSize = glowSize * GlowOuterScale;
			Color outerColor = GlowOuterColor * alpha * 0.7f;
			Rectangle outerRect = new Rectangle(
				(int)(screenPos.X - outerSize * 0.5f),
				(int)(screenPos.Y - outerSize * 0.5f),
				(int)outerSize, (int)outerSize);
			Main.spriteBatch.Draw(_lightTexture, outerRect, null, outerColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 大型圆形光晕：用正弦曲线快速膨胀后缓慢缩小
		private void DrawLargeGlow(int frame) {
			if (frame >= LargeGlowTotalFrames)
				return;
			if (_lightTexture == null || _lightTexture.IsDisposed)
				return;

			float progress = (float)frame / LargeGlowTotalFrames;
			float size = LargeGlowStartSize + (LargeGlowPeakSize - LargeGlowStartSize) * (float)Math.Sin(progress * MathHelper.Pi);
			float alpha = LargeGlowAlpha * (1f - progress * progress);
			Vector2 screenPos = _spawnPosition - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearClamp, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			Rectangle rect = new Rectangle(
				(int)(screenPos.X - size * 0.5f),
				(int)(screenPos.Y - size * 0.5f),
				(int)size, (int)size);
			Main.spriteBatch.Draw(_lightTexture, rect, null, LargeGlowColor * alpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None,
				Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 小光点粒子：白色，Additive 混合，快速缩小
		private void DrawLightParticles() {
			if (lightParticles.Count == 0)
				return;
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (var lp in lightParticles) {
				float alpha = (float)lp.Life / lp.MaxLife;
				float size = lp.Size * 14f;
				Vector2 pos = lp.Position - Main.screenPosition;
				Rectangle rect = new Rectangle((int)(pos.X - size * 0.5f), (int)(pos.Y - size * 0.5f), (int)size, (int)size);
				Main.spriteBatch.Draw(_lightTexture, rect, null, lp.Color * alpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 普通多边形粒子：Additive 混合，边数递减，带抖动
		private void DrawPolyParticles() {
			if (polyParticles.Count == 0)
				return;

			Main.spriteBatch.End();
			GraphicsDevice gd = Main.graphics.GraphicsDevice;
			BlendState prevBlend = gd.BlendState;
			RasterizerState prevRaster = gd.RasterizerState;
			gd.BlendState = BlendState.Additive;
			gd.RasterizerState = RasterizerState.CullNone;

			if (_basicEffect == null) {
				_basicEffect = new BasicEffect(gd);
				_basicEffect.VertexColorEnabled = true;
			}
			_basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
			_basicEffect.View = Main.GameViewMatrix.TransformationMatrix;
			_basicEffect.World = Matrix.Identity;

			foreach (var pp in polyParticles) {
				float alpha = (float)pp.Life / pp.MaxLife;
				Color drawColor = pp.Color * alpha;
				Color centerColor = drawColor * 0.3f;
				Vector2 center = pp.Position - Main.screenPosition;
				float r = pp.Size;
				int sides = Math.Max(pp.Sides, 3);

				VertexPositionColor[] verts = new VertexPositionColor[sides * 3];
				for (int j = 0; j < sides; j++) {
					float a1 = pp.Rotation + j * MathHelper.TwoPi / sides;
					float a2 = pp.Rotation + ((j + 1) % sides) * MathHelper.TwoPi / sides;
					Vector2 o1 = center + new Vector2(MathF.Cos(a1) * r, MathF.Sin(a1) * r);
					Vector2 o2 = center + new Vector2(MathF.Cos(a2) * r, MathF.Sin(a2) * r);
					verts[j * 3 + 0] = new VertexPositionColor(new Vector3(center, 0), centerColor);
					verts[j * 3 + 1] = new VertexPositionColor(new Vector3(o1, 0), drawColor);
					verts[j * 3 + 2] = new VertexPositionColor(new Vector3(o2, 0), drawColor);
				}
				foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes) {
					pass.Apply();
					gd.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, sides);
				}
			}

			gd.BlendState = prevBlend;
			gd.RasterizerState = prevRaster;
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
				Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 主十字光效：两条 45° 和 135° 的光柱，带随机角度偏移
		private void DrawCrossEffect(int frame) {
			GetCrossSize(frame, out float mW, out float mH, out float oW, out float oH);
			float alpha = CalculateAlpha(frame);
			Vector2 sp = _spawnPosition - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			DrawRotatedCrossPart(sp, mW, mH, oW, oH, alpha, MathHelper.PiOver4 + _crossBaseAngle);
			DrawRotatedCrossPart(sp, mW, mH, oW, oH, alpha, -MathHelper.PiOver4 + _crossBaseAngle);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 更大的十字光效：水平和垂直方向，尺寸放大 1.8 倍，透明度更低
		private void DrawLargerCrossEffect(int frame) {
			GetCrossSize(frame, out float mW, out float mH, out float oW, out float oH);
			float largeMW = mW * LargerCrossScale;
			float largeMH = mH * LargerCrossScale;
			float largeOW = oW * LargerCrossScale;
			float largeOH = oH * LargerCrossScale;
			float alpha = CalculateAlpha(frame) * LargerCrossAlpha;
			if (alpha <= 0.01f)
				return;

			Vector2 sp = _spawnPosition - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			DrawRotatedCrossPart(sp, largeMW, largeMH, largeOW, largeOH, alpha, 0f + _crossBaseAngle);
			DrawRotatedCrossPart(sp, largeMW, largeMH, largeOW, largeOH, alpha, MathHelper.PiOver2 + _crossBaseAngle);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 画一根旋转过的光柱，同时画主光晕和外光晕
		private void DrawRotatedCrossPart(Vector2 c, float mW, float mH, float oW, float oH, float a, float r) {
			Vector2 o = _crossTexture.Size() / 2f;
			Main.spriteBatch.Draw(_crossTexture, c, null, MainColor * a, r, o,
				new Vector2(mW / _crossTexture.Width, mH / _crossTexture.Height), SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(_crossTexture, c, null, OuterColor * a, r, o,
				new Vector2(oW / _crossTexture.Width, oH / _crossTexture.Height), SpriteEffects.None, 0f);
		}

		// 纯黑十字光效：短暂出现，从大到小收缩，AlphaBlend 混合
		private void DrawDarkCross(int frame) {
			int localFrame = frame - DarkCrossStartFrame;
			if (localFrame < 0 || localFrame >= DarkCrossDuration)
				return;

			float progress = (float)localFrame / (DarkCrossDuration - 1);
			float w = DarkCrossStartWidth + (DarkCrossEndWidth - DarkCrossStartWidth) * progress;
			float h = DarkCrossStartHeight + (DarkCrossEndHeight - DarkCrossStartWidth) * progress;
			float alpha = DarkCrossAlpha * (1f - progress * progress);
			if (alpha <= 0f)
				return;

			Vector2 sp = _spawnPosition - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
				SamplerState.LinearClamp, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			Color darkColor = DarkCrossColor * alpha;
			Vector2 origin = _crossTexture.Size() / 2f;
			float scaleX = w / _crossTexture.Width;
			float scaleY = h / _crossTexture.Height;

			Main.spriteBatch.Draw(_crossTexture, sp, null, darkColor,
				MathHelper.PiOver4 + _crossBaseAngle, origin, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(_crossTexture, sp, null, darkColor,
				-MathHelper.PiOver4 + _crossBaseAngle, origin, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 随机散逸粒子：Additive 混合，线性缩小
		private void DrawRandomParticles() {
			if (randomParticles.Count == 0)
				return;
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (var rp in randomParticles) {
				float alpha = (float)rp.Life / rp.MaxLife;
				float size = rp.Size * 10f;
				Vector2 pos = rp.Position - Main.screenPosition;
				Rectangle rect = new Rectangle((int)(pos.X - size * 0.5f), (int)(pos.Y - size * 0.5f), (int)size, (int)size);
				Main.spriteBatch.Draw(_lightTexture, rect, null, rp.Color * alpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		// 根据当前帧计算十字光效的主尺寸和外光晕尺寸
		private void GetCrossSize(int f, out float mW, out float mH, out float oW, out float oH) {
			float mw, mh;
			if (f <= PeakFrame) {
				float t = f / (float)PeakFrame;
				float e = 1f - (1f - t) * (1f - t);
				mw = MainStartWidth + (MainPeakWidth - MainStartWidth) * e;
				mh = MainStartHeight + (MainPeakHeight - MainStartHeight) * e;
			}
			else if (f <= ShrinkEndFrame) {
				float t = (f - PeakFrame) / (float)(ShrinkEndFrame - PeakFrame);
				float e = t * t;
				mw = MainPeakWidth + (MainShrinkWidth - MainPeakWidth) * e;
				mh = MainPeakHeight + (MainShrinkHeight - MainPeakHeight) * e;
			}
			else {
				float t = (f - ShrinkEndFrame) / (float)(TotalFrames - 1 - ShrinkEndFrame);
				float e = 1f - (1f - t) * (1f - t);
				mw = MainShrinkWidth + (MainEndWidth - MainShrinkWidth) * e;
				mh = MainShrinkHeight + (MainEndHeight - MainShrinkHeight) * e;
			}
			mW = Math.Max(1, mw);
			mH = Math.Max(1, mh);

			float s;
			if (f <= PeakFrame) { float t = f / (float)PeakFrame; float e = 1f - (1f - t) * (1f - t); s = OuterScaleStart + (OuterScalePeak - OuterScaleStart) * e; }
			else if (f <= ShrinkEndFrame) { float t = (f - PeakFrame) / (float)(ShrinkEndFrame - PeakFrame); float e = t * t; s = OuterScalePeak + (OuterScaleShrink - OuterScalePeak) * e; }
			else { float t = (f - ShrinkEndFrame) / (float)(TotalFrames - 1 - ShrinkEndFrame); float e = 1f - (1f - t) * (1f - t); s = OuterScaleShrink + (OuterScaleEnd - OuterScaleShrink) * e; }
			oW = mW * s;
			oH = mH * s;
		}

		// 透明度曲线：先慢后快消失
		private float CalculateAlpha(int f) {
			float t = f / (float)(TotalFrames - 1);
			return 1f - t * t;
		}
	}
}