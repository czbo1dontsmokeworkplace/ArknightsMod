using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using HeatWaveRTEffect = ArknightsMod.Content.SwingHelper.HeatWaveRTEffect;
using RTHelper = ArknightsMod.Content.SwingHelper.RTHelper;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	/// <summary>
	/// Laevatain 的独立重型戳刺动作。
	/// 目标角度只在第一次调用时锁定，之后剑身始终沿锁定轴线。
	/// </summary>
	public class Laevatain_SwingHelper : SwingHelper.SwingHelper
	{
		public const float StabAreaScale = 1.75f;
		public const float StabBaseLineWidth = 24f;
		public const float StabLineWidth = StabBaseLineWidth * StabAreaScale;
		private const float StabTipExtension = 28f;
		public const int StabWindupFrames = 7;
		public const int StabHoldFrames = 2;
		public const int StabThrustFrames = 4;
		public const int StabImpactFrames = 3;
		public const int StabRecoverFrames = 10;
		public const int StabTotalFrames =
			StabWindupFrames + StabHoldFrames + StabThrustFrames + StabImpactFrames + StabRecoverFrames;

		// 兼容原有调用方保留的手部角度字段。
		public float stabRad_hand;
		public float startRad_hand;
		public float endRad_hand;

		private bool stabInitialized;
		private bool stabFinished;
		private float lockedStabRad;
		private float initialHandRad;
		private float initialSwordRad;
		private Vector2 initialSetoff;
		private int lastStabFrame = -1;
		private int activeStabDuration;
		private int windupFrames;
		private int holdFrames;
		private int thrustFrames;
		private int impactFrames;
		private int recoverFrames;
		public float StabEffectOpacity { get; private set; }

		public bool IsStabbing => stabInitialized && !stabFinished;
		public bool IsStabThrustStart => stabInitialized && lastStabFrame == windupFrames + holdFrames;
		public bool IsStabThrustPhase => stabInitialized &&
			lastStabFrame >= windupFrames + holdFrames &&
			lastStabFrame < windupFrames + holdFrames + thrustFrames;

		/// <summary>本次戳刺总时长，单位为游戏帧；最短 5 帧以确保每个阶段至少运行一帧。</summary>
		public int TotalStabDuration
		{
			get => StabUseTime;
			set => StabUseTime = Math.Max(5, value);
		}

		/// <summary>当前帧是否属于可以造成伤害的突刺/顶住阶段。</summary>
		public bool IsStabDamageWindow => stabInitialized &&
			lastStabFrame >= windupFrames + holdFrames &&
			lastStabFrame < windupFrames + holdFrames + thrustFrames + impactFrames;

		/// <summary>锁定的世界戳刺方向。</summary>
		public Vector2 StabDirection => lockedStabRad.ToRotationVector2();

		/// <summary>连击间沿同一旋转方向补完剩余弧线，不触发碰撞。</summary>
		public void ContinueSwingPose(float angle)
		{
			swordRad = angle;
			swordDir = 1f;
			SwordAHandCon(0f, swordRad, texLength.Length(), handleLength.Length(), swordLength.Length(),
				true, Player.CompositeArmStretchAmount.Full);
		}

		public Laevatain_SwingHelper(int index, int catmullScale, bool isBackArm = false)
			: base(index, catmullScale, isBackArm)
		{
			StabUseTime = StabTotalFrames;
		}

		// BB 式短促节奏：起手迅速加速，越过中点后迅速制动，末端仍连续。
		protected override float EaseSwingProgress(float progress)
		{
			float t = MathHelper.Clamp(progress, 0f, 1f);
			return t * t * t * (t * (t * 6f - 15f) + 10f);
		}

		/// <summary>清除本次戳刺的锁定状态。</summary>
		public new Laevatain_SwingHelper ResetTime(int lagtime = 0)
		{
			base.ResetTime(lagtime);
			stabInitialized = false;
			stabFinished = false;
			lockedStabRad = 0f;
			initialHandRad = 0f;
			initialSwordRad = 0f;
			initialSetoff = oldSetoff;
			lastStabFrame = -1;
			StabEffectOpacity = 0f;
			return this;
		}

		/// <summary>使用鼠标世界坐标开始/推进戳刺。鼠标只在第一帧读取一次。</summary>
		public bool Stab(Vector2 mouseWorldPosition)
		{
			Vector2 aim = mouseWorldPosition - player.Center;
			float targetRad = aim.LengthSquared() > 0.0001f ? aim.ToRotation() : swordRot;
			return RunStab(targetRad);
		}

		/// <summary>兼容基类的无参数调用，使用外部提前设置的 stabRad。</summary>
		public override bool Stab(float swordtohand = 0f)
		{
			return RunStab(stabRad);
		}

		private bool RunStab(float targetRad)
		{
			if (!stabInitialized)
				BeginStab(targetRad);

			if (stabFinished)
				return true;

			int t = stabTime;
			if (t >= activeStabDuration)
			{
				stabFinished = true;
				setoff = oldSetoff;
				return true;
			}
			lastStabFrame = t;
			scale = Vector2.One;

			float desiredHandRad;
			float desiredSwordRad;
			float travel;
			StabEffectOpacity = 0f;

			if (t < windupFrames)
			{
				float p = SmoothStep(t / (float)windupFrames);
				desiredHandRad = LerpAngle(initialHandRad, lockedStabRad + MathHelper.ToRadians(34f), p);
				desiredSwordRad = lockedStabRad;
				travel = MathHelper.Lerp(0f, -16f, p);
			}
			else if (t < windupFrames + holdFrames)
			{
				desiredHandRad = lockedStabRad + MathHelper.ToRadians(34f);
				desiredSwordRad = lockedStabRad;
				travel = -16f;
			}
			else if (t < windupFrames + holdFrames + thrustFrames)
			{
				float p = (t - windupFrames - holdFrames + 1f) / thrustFrames;
				float burst = 1f - MathF.Pow(1f - MathHelper.Clamp(p, 0f, 1f), 3f);
				StabEffectOpacity = 0.35f + 0.65f * MathF.Sin(MathHelper.Clamp(p, 0f, 1f) * MathF.PI);
				desiredHandRad = LerpAngle(lockedStabRad + MathHelper.ToRadians(34f), lockedStabRad, burst);
				desiredSwordRad = lockedStabRad;
				travel = MathHelper.Lerp(-20f, 88f, burst);
			}
			else if (t < windupFrames + holdFrames + thrustFrames + impactFrames)
			{
				float p = (t - windupFrames - holdFrames - thrustFrames + 1f) / impactFrames;
				StabEffectOpacity = 0.65f * (1f - SmoothStep(p));
				desiredHandRad = lockedStabRad;
				desiredSwordRad = lockedStabRad;
				travel = MathHelper.Lerp(88f, 76f, SmoothStep(p));
			}
			else
			{
				float p = (t - windupFrames - holdFrames - thrustFrames - impactFrames + 1f) /
					recoverFrames;
				float eased = SmoothStep(MathHelper.Clamp(p, 0f, 1f));
				desiredHandRad = LerpAngle(lockedStabRad, initialHandRad, eased);
				desiredSwordRad = LerpAngle(lockedStabRad, initialSwordRad, eased);
				travel = MathHelper.Lerp(76f, 0f, eased);
			}

			stabRad_hand = desiredHandRad;
			SwordAHandCon(desiredSwordRad - desiredHandRad, desiredHandRad,
				texLength.Length(), handleLength.Length(), swordLength.Length(),
				armType: Player.CompositeArmStretchAmount.Full);

			// setoff 的 X 轴就是剑身轴线；沿它移动会让绘制出来的整把剑前后移动。
			setoff = initialSetoff + new Vector2(travel, 0f);
			swordDir = 1f;
			Vector2 drawOffset = setoff.RotatedBy(swordRot);
			SavePos(swordHead + drawOffset, swordRot, handlePos + drawOffset);
			stabAction?.Invoke();
			stabTime++;
			return false;
		}

		private void BeginStab(float targetRad)
		{
			stabInitialized = true;
			stabFinished = false;
			stabTime = 0;
			StabEffectOpacity = 0f;
			activeStabDuration = Math.Max(5, StabUseTime);
			StabUseTime = activeStabDuration;
			DistributePhaseFrames(activeStabDuration);
			lockedStabRad = targetRad;
			stabRad = targetRad;
			initialSetoff = oldSetoff;

			Vector2 handVector = handlePos - player.Center;
			Vector2 swordVector = swordPos - handlePos;
			initialHandRad = handVector.LengthSquared() > 0.0001f ? handVector.ToRotation() : targetRad;
			initialSwordRad = swordVector.LengthSquared() > 0.0001f ? swordVector.ToRotation() : swordRot;
			startRad_hand = initialHandRad;
			endRad_hand = targetRad;
			stabRad_hand = targetRad + MathHelper.ToRadians(28f);
		}

		private void DistributePhaseFrames(int duration)
		{
			int[] weights = [StabWindupFrames, StabHoldFrames, StabThrustFrames, StabImpactFrames, StabRecoverFrames];
			int[] frames = new int[weights.Length];
			int[] remainders = new int[weights.Length];
			int remaining = duration;

			for (int i = 0; i < weights.Length; i++)
			{
				int scaled = duration * weights[i];
				frames[i] = scaled / StabTotalFrames;
				remainders[i] = scaled % StabTotalFrames;
				remaining -= frames[i];
			}

			// Very short custom durations still give every phase at least one update.
			for (int i = 0; i < frames.Length; i++)
			{
				if (frames[i] != 0)
					continue;
				frames[i] = 1;
				remainders[i] = -1;
				remaining--;
			}

			while (remaining-- > 0)
			{
				int best = 0;
				for (int i = 1; i < remainders.Length; i++)
					if (remainders[i] > remainders[best])
						best = i;
				frames[best]++;
				remainders[best] = -1;
			}

			windupFrames = frames[0];
			holdFrames = frames[1];
			thrustFrames = frames[2];
			impactFrames = frames[3];
			recoverFrames = frames[4];
		}

		private static float SmoothStep(float value)
		{
			value = MathHelper.Clamp(value, 0f, 1f);
			return value * value * (3f - 2f * value);
		}

		private static float LerpAngle(float from, float to, float amount)
		{
			return from + MathHelper.WrapAngle(to - from) * MathHelper.Clamp(amount, 0f, 1f);
		}

		/// <summary>二技能判定与绘制共用的突刺轴线，长度相对原判定扩大 1.75 倍。</summary>
		public void GetStabHitLine(out Vector2 start, out Vector2 end)
		{
			Vector2 drawOffset = setoff.RotatedBy(swordRot);
			start = handlePos + drawOffset;
			Vector2 originalEnd = swordPos + drawOffset + StabDirection * StabTipExtension;
			end = start + (originalEnd - start) * StabAreaScale;
		}

		/// <summary>碰撞线与 DrawBlade 的 setoff 位移保持一致。</summary>
		public new bool Colliding(Rectangle targetHitbox)
		{
			if (stabInitialized && !IsStabDamageWindow)
				return false;

			Vector2 start;
			Vector2 end;
			float width;
			if (stabInitialized) {
				GetStabHitLine(out start, out end);
				width = StabLineWidth;
			}
			else {
				Vector2 drawOffset = setoff.RotatedBy(swordRot);
				start = handlePos + drawOffset;
				end = swordPos + drawOffset;
				width = 20f;
			}
			return Collision.CheckAABBvLineCollision(
				targetHitbox.TopLeft(),
				targetHitbox.Size(),
				start,
				end,
				width,
				ref point);
		}
	}

	internal enum LaevatainStabWindShape
	{
		Chevron,
		Parallel
	}

	/// <summary>Laevatain 戳刺的破空线和局部 RT 扭曲。</summary>
	internal sealed class LaevatainStabEffect
	{
		private static readonly Rectangle Pixel = new(0, 0, 1, 1);
		private float previousExtension;
		private bool hasPreviousExtension;
		private float tipSpeed;
		private Vector2 impactCenter;
		private int impactAge = -1;
		private const int ImpactRippleFrames = 5;

		public LaevatainStabWindShape WindShape { get; set; } = LaevatainStabWindShape.Parallel;
		/// <summary>“=”两条线相对戳刺方向的整体旋转角，单位为弧度。</summary>
		public float WindAngleOffsetRadians { get; set; }
		/// <summary>“=”两条线的中心间距，沿线条法线测量，随镜头缩放。</summary>
		public float ParallelSpacing { get; set; } = 18f;
		/// <summary>破空线颜色；透明度仍由戳刺动作自动控制。</summary>
		public Color WindColor { get; set; } = new(255, 250, 235);
		/// <summary>破空线沿剑身的位置比例：0 为剑柄，1 为剑尖。</summary>
		public float WindPositionAlongBlade { get; set; } = 0.8f;
		/// <summary>流动亮段速度，单位为屏幕像素/秒。</summary>
		public float WindFlowSpeed { get; set; } = 80f;
		/// <summary>兼容 Chevron 模式的开口角度，单位为弧度。</summary>
		public float ChevronHalfAngleRadians { get; set; } = MathHelper.ToRadians(22f);

		public void Reset()
		{
			hasPreviousExtension = false;
			tipSpeed = 0f;
			previousExtension = 0f;
			impactAge = -1;
		}

		public void Update(Laevatain_SwingHelper helper)
		{
			tipSpeed = hasPreviousExtension ? MathF.Abs(helper.setoff.X - previousExtension) : 0f;
			previousExtension = helper.setoff.X;
			hasPreviousExtension = true;

			if (impactAge >= 0 && ++impactAge >= ImpactRippleFrames)
				impactAge = -1;
		}

		public void OnHit(Vector2 worldPosition)
		{
			impactCenter = worldPosition;
			impactAge = 0;
		}

		public void OnBurst(Vector2 worldPosition)
		{
			impactCenter = worldPosition;
			impactAge = 0;
		}

		public void DrawDistortion(Laevatain_SwingHelper helper, SpriteBatch spriteBatch)
		{
			float opacity = MathF.Max(helper.StabEffectOpacity,
				helper.IsStabDamageWindow ? 0.7f : 0f);
			bool drawImpact = impactAge >= 0;
			if (opacity <= 0.01f && !drawImpact)
				return;

			helper.GetStabHitLine(out Vector2 startWorld, out Vector2 endWorld);
			Vector2 axis = helper.StabDirection;
			Vector2 side = new(-axis.Y, axis.X);
			Matrix view = Main.GameViewMatrix.TransformationMatrix;

			Vector2 screenStart = Vector2.Transform(startWorld - Main.screenPosition, view);
			Vector2 screenEnd = Vector2.Transform(endWorld - Main.screenPosition, view);
			float halfWidth = Vector2.TransformNormal(side * (Laevatain_SwingHelper.StabLineWidth * 0.5f), view).Length();
			Vector2 screenImpact = drawImpact
				? Vector2.Transform(impactCenter - Main.screenPosition, view)
				: Vector2.Zero;
			float impactProgress = drawImpact ? impactAge / (float)(ImpactRippleFrames - 1) : 0f;
			float impactRadius = drawImpact
				? Vector2.TransformNormal(new Vector2((10f + impactAge * 12f) * Laevatain_SwingHelper.StabAreaScale, 0f), view).Length()
				: 0f;
			float impactOpacity = drawImpact ? 0.95f * (1f - impactProgress) : 0f;
			float strength = 34f + MathHelper.Clamp(tipSpeed * 1.0f, 0f, 24f);

			ModContent.GetInstance<RTHelper>().DrawScreen(spriteBatch, source =>
				HeatWaveRTEffect.DrawStab(spriteBatch, source,
					screenStart, screenEnd, halfWidth, strength, opacity, Main.GlobalTimeWrappedHourly,
					screenImpact, impactRadius, impactOpacity, BlendState.Opaque));
		}

		public void DrawWindLines(Laevatain_SwingHelper helper)
		{
			float opacity = MathF.Max(helper.StabEffectOpacity,
				helper.IsStabDamageWindow ? 0.8f : 0f);
			if (opacity <= 0.01f)
				return;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
				SamplerState.LinearClamp, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			helper.GetStabHitLine(out Vector2 start, out Vector2 end);
			Vector2 axis = helper.StabDirection;
			float bladeLength = Vector2.Distance(start, end);
			float motion = MathHelper.Clamp(tipSpeed / 20f, 0.35f, 1f);
			Vector2 screenWindCenter = Vector2.Lerp(start, end,
				MathHelper.Clamp(WindPositionAlongBlade, 0.05f, 0.95f)) - Main.screenPosition;
			Vector2 windDirection = axis.RotatedBy(WindAngleOffsetRadians);
			Vector2 windSide = new(-axis.Y, axis.X);
			Color windColor = WindColor;
			float windLength = MathHelper.Clamp(bladeLength * 0.42f + motion * 8f, 28f, 130f);
			Vector2 coreStart = start - Main.screenPosition;
			Vector2 coreEnd = end - Main.screenPosition;
			float halfWidth = Laevatain_SwingHelper.StabLineWidth * 0.5f;
			DrawLine(coreStart, coreEnd, windColor * (opacity * 0.42f), halfWidth * 1.5f);
			DrawLine(coreStart, coreEnd, new Color(255, 220, 170) * (opacity * 0.9f), 5f);
			foreach (int sign in new[] { -1, 1 })
				DrawLine(coreStart + windSide * (sign * halfWidth), coreEnd + windSide * (sign * halfWidth),
					new Color(255, 105, 25) * (opacity * 0.36f), 2.5f);
			Vector2 screenTip = coreEnd;
			DrawLine(screenTip - windSide * halfWidth, screenTip + windSide * halfWidth,
				new Color(255, 163, 48) * (opacity * 0.7f), 5f);

			if (WindShape == LaevatainStabWindShape.Parallel)
			{
				// 上下两条线分别反向倾斜，间距仍沿剑身法线调整。
				float halfLength = windLength * 0.5f;
				float halfSpacing = MathHelper.Clamp(ParallelSpacing, 0f, 100f) * 0.5f;
				foreach (int sign in new[] { -1, 1 })
				{
					Vector2 lineDirection = axis.RotatedBy(sign * WindAngleOffsetRadians);
					Vector2 lineCenter = screenWindCenter + windSide * (sign * halfSpacing);
					DrawFlowingLine(lineCenter - lineDirection * halfLength, lineCenter + lineDirection * halfLength,
						windColor, opacity, 4.2f, sign > 0 ? 0f : 0.5f);
				}
			}
			else
			{
				// 两条线从剑身偏前段向前张开，形成 <；开口角度可单独调整。
				Vector2 root = screenWindCenter;
				float halfAngle = MathHelper.Clamp(ChevronHalfAngleRadians,
					MathHelper.ToRadians(1f), MathHelper.ToRadians(80f));
				foreach (int sign in new[] { -1, 1 })
				{
					Vector2 branchDirection = windDirection.RotatedBy(sign * halfAngle);
					DrawFlowingLine(root, root + branchDirection * windLength,
						windColor, opacity, 4.2f, sign > 0 ? 0f : 0.5f);
				}
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin();
		}

		private void DrawFlowingLine(Vector2 start, Vector2 end, Color color, float opacity, float width, float phase)
		{
			Vector2 delta = end - start;
			float length = delta.Length();
			if (!float.IsFinite(length) || length < 0.01f)
				return;

			Vector2 direction = delta / length;
			DrawLine(start, end, color * (opacity * 0.32f), width);

			float dashLength = MathHelper.Clamp(length * 0.22f, 10f, 20f);
			float travelRange = length + dashLength;
			float cycle = travelRange * 2f;
			float timeOffset = Main.GlobalTimeWrappedHourly * MathHelper.Clamp(WindFlowSpeed, 0f, 500f);
			for (int i = 0; i < 2; i++)
			{
				float distance = (timeOffset + (phase + i * 0.5f) * cycle) % cycle - dashLength;
				float dashStart = MathHelper.Clamp(distance, 0f, length);
				float dashEnd = MathHelper.Clamp(distance + dashLength, 0f, length);
				if (dashEnd <= dashStart)
					continue;

				float pulseOpacity = i == 0 ? 0.95f : 0.68f;
				DrawLine(start + direction * dashStart, start + direction * dashEnd,
					color * (opacity * pulseOpacity), width + 1.2f);
			}
		}

		private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
		{
			Vector2 delta = end - start;
			float length = delta.Length();
			if (!float.IsFinite(length) || length < 0.01f)
				return;

			Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, Pixel, color,
				delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(length, width), SpriteEffects.None);
		}
	}
}
