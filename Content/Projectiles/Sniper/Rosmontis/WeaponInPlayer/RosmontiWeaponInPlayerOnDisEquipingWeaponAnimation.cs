using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer
{
	public class RosmontiWeaponInPlayerOnDisEquipingWeaponAnimation : RosmontiWeaponInPlayerAnimationBase
	{
		private readonly float totalTime;
		private readonly Vector2 distanceFromTargetHeight;
		private Vector2 targetPosition;
		private Vector2 previousVelocity;
		private float elapsedTime;
		private bool isStarted;

		public RosmontiWeaponInPlayerOnDisEquipingWeaponAnimation(
			RosmontiWeaponInPlayerDrawer drawer,
			float totalTime,
			Vector2 distanceFromTargetHeight)
			: base(drawer)
		{
			this.totalTime = MathHelper.Max(0f, totalTime);
			this.distanceFromTargetHeight = distanceFromTargetHeight;
			SpriteEffects = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetSpriteEffects(drawer);

			targetPosition = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetPosition(drawer);
			drawer.Position = targetPosition;
			drawer.Velocity = Vector2.Zero;
			drawer.Acceleration = Vector2.Zero;
			previousVelocity = Vector2.Zero;
			drawer.Rotation = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetRotation(drawer);
			SpriteEffects = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetSpriteEffects(drawer);
			elapsedTime = 0f;
			isStarted = true;
			IsVisible = true;
			drawer.Alpha = 1f;

			if (totalTime <= 0f)
				FinishAnimation(Vector2.Zero);
		}

		public override void StartOnDisEquipingWeaponAnimation()
		{

		}

		protected override void UpdateAnimation()
		{
			if (!isStarted || totalTime <= 0f)
				return;

			elapsedTime = MathHelper.Min(elapsedTime + 1f, totalTime);
			float progress = EaseOutBack(1f - elapsedTime / totalTime);
			Vector2 currentTargetPosition = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetPosition(drawer);
			Vector2 currentPosition = Vector2.Lerp(
				currentTargetPosition - distanceFromTargetHeight,
				currentTargetPosition,
				progress);
			targetPosition = currentTargetPosition;

			Vector2 currentVelocity = currentPosition - drawer.Position;
			drawer.Velocity = currentVelocity;
			drawer.Acceleration = currentVelocity - previousVelocity;
			drawer.Position = currentPosition;
			drawer.Rotation = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetRotation(drawer);
			SpriteEffects = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetSpriteEffects(drawer);
			previousVelocity = currentVelocity;

			UpdateMotionRotation(currentVelocity);


			drawer.Rotation = MotionRotationOffset
				+ RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetRotation(drawer);

			// Invert the equip animation's alpha rule: the overshoot stays fully opaque.
			drawer.Alpha = progress < 1f ? progress : 1f;

			if (elapsedTime >= totalTime)
				FinishAnimation(currentVelocity);
		}

		private void UpdateMotionRotation(Vector2 currentVelocity)
		{
			Player player = drawer.Player;
			float horizontalPercent = MathHelper.Clamp(MathF.Abs(currentVelocity.X) / 12f, 0f, 1f);
			Vector2 rotatedVelocity = new Vector2(currentVelocity.X).RotatedBy(MathHelper.PiOver4);
			float horizontalRotation = currentVelocity.Length() < 0.1f
				? 0f
				: MathF.Atan2(rotatedVelocity.Y, rotatedVelocity.X) * horizontalPercent;

			bool absoluteRight = player.direction == 1 && (drawer.Index == 0 || drawer.Index == 2)
				|| player.direction == -1 && (drawer.Index == 1 || drawer.Index == 3);
			float verticalPercent = MathHelper.Clamp(currentVelocity.Y / 12f, -1f, 1f);
			float verticalRotation = absoluteRight
				? verticalPercent * MathHelper.PiOver4 / 4f
				: -verticalPercent * MathHelper.PiOver4 / 4f;

			MotionRotationOffset = MathHelper.Lerp(
				MotionRotationOffset,
				horizontalRotation * 0.2f + verticalRotation,
				0.8f);
		}

		private void FinishAnimation(Vector2 finalVelocity)
		{
			Vector2 finalPosition = targetPosition - distanceFromTargetHeight;
			drawer.Velocity = finalVelocity;
			drawer.Acceleration = Vector2.Zero;
			drawer.Position = finalPosition;
			drawer.Alpha = 0f;
			IsVisible = false;
			isStarted = false;
		}

		private static float EaseOutBack(float progress)
		{
			const float c1 = 1.70158f;
			const float c3 = c1 + 1f;
			float invertedProgress = progress - 1f;
			return 1f + c3 * invertedProgress * invertedProgress * invertedProgress
				+ c1 * invertedProgress * invertedProgress;
		}
	}
}
