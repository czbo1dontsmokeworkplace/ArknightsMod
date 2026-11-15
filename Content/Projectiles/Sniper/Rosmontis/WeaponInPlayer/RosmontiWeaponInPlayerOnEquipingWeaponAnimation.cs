using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer
{
	public class RosmontiWeaponInPlayerOnEquipingWeaponAnimation : RosmontiWeaponInPlayerAnimationBase
	{
		private readonly float totalTime;
		private readonly Vector2 distanceFromTarget;
		private Vector2 targetPosition;
		private Vector2 previousPosition;
		private Vector2 previousVelocity;
		private float elapsedTime;
		private bool isStarted;

		private bool firstGreaterThanOne = false;

		public RosmontiWeaponInPlayerOnEquipingWeaponAnimation(
			RosmontiWeaponInPlayerDrawer drawer,
			float totalTime,
			Vector2 distanceFromTarget)
			: base(drawer)
		{
			this.totalTime = MathHelper.Max(0f, totalTime);
			this.distanceFromTarget = distanceFromTarget;
			SpriteEffects = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetSpriteEffects(drawer);

			targetPosition = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetPosition(drawer);
			previousPosition = targetPosition - Vector2.UnitY * distanceFromTarget;
			drawer.Position = previousPosition;
			drawer.Velocity = Vector2.Zero;
			drawer.Acceleration = Vector2.Zero;
			previousVelocity = Vector2.Zero;
			drawer.Rotation = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetRotation(drawer);
			SpriteEffects = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetSpriteEffects(drawer);
			elapsedTime = 0f;
			isStarted = true;
			IsVisible = true;

			if (totalTime <= 0f)
				FinishAnimation(Vector2.Zero);
		}

		public override void StartOnEquipingWeaponAnimation()
		{

		}

		protected override void UpdateAnimation()
		{
			if (!isStarted)
				return;

			if (totalTime <= 0f)
				return;

			elapsedTime = MathHelper.Min(elapsedTime + 1f, totalTime);
			float progress = EaseOutBack(elapsedTime / totalTime);

			Vector2 currentPosition = Vector2.Lerp(
				RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetPosition(drawer) -distanceFromTarget,
				RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetPosition(drawer),
				progress);

			Vector2 currentVelocity = currentPosition - drawer.Position;
			drawer.Velocity = currentVelocity;
			drawer.Acceleration = currentVelocity - previousVelocity;
			drawer.Position = currentPosition;
			drawer.Rotation = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetRotation(drawer);
			SpriteEffects = RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetSpriteEffects(drawer);
			previousPosition = currentPosition;
			previousVelocity = currentVelocity;



			Player player = drawer.Player;
			float horizontalPercent = MathHelper.Clamp(MathF.Abs(currentVelocity.X) / 12f, 0f, 1f);
			Vector2 rotatedVelocity = new Vector2(currentVelocity.X).RotatedBy(MathHelper.PiOver4);
			float horizontalRotation =currentVelocity.Length() < 0.1f
				? 0f
				: MathF.Atan2(rotatedVelocity.Y, rotatedVelocity.X) * horizontalPercent;

			bool absoluteRight = player.direction == 1 && (drawer.Index == 0 || drawer.Index == 2)
			                     || player.direction == -1 && (drawer.Index == 1 || drawer.Index == 3);
			float verticalPercent = MathHelper.Clamp(currentVelocity.Y / 12f, -1f, 1f);
			float verticalRotation = absoluteRight
				? verticalPercent * MathHelper.PiOver4 / 3f
				: -verticalPercent * MathHelper.PiOver4 / 3f;

			MotionRotationOffset = MathHelper.Lerp(
				MotionRotationOffset,
				horizontalRotation * 0.2f + verticalRotation,
				0.8f);

			if (progress < 0.7f) {
				MotionRotationOffset = 0;
			}

			drawer.Rotation = MotionRotationOffset + RosmontiWeaponInPlayerAnimation.GetChasingPlayerTargetRotation(drawer);

			if (!firstGreaterThanOne && progress >= 1) {
				firstGreaterThanOne = true;
				drawer.Alpha = 1;
			}

			if (!firstGreaterThanOne) {
				drawer.Alpha = progress;
			}



		}

		private void FinishAnimation(Vector2 finalVelocity)
		{
			Vector2 finalPosition = targetPosition;
			drawer.Velocity = finalVelocity;
			drawer.Acceleration = Vector2.Zero;
			drawer.Position = finalPosition;
			isStarted = false;
			ExpectedAnimationType = RosmontiWeaponAnimationType.Weaponing;
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
