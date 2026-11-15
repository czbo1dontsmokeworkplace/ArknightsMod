using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer
{
	public class RosmontiWeaponInPlayerShootAnimation : RosmontiWeaponInPlayerAnimationBase
	{
		private readonly float rotationAngle;
		private readonly int rotationDirection;
		private readonly float rotationTime;
		private readonly Func<float, float> rotationEasing;
		private readonly float upwardHeight;
		private readonly float upwardTime;
		private float startRotation;
		private Vector2 startPosition;
		private float elapsedTime;
		private bool isStarted;

		private int dir;

		private float startUpTime;

		public bool shouldReturn = true;

		public RosmontiWeaponInPlayerShootAnimation(
			RosmontiWeaponInPlayerDrawer drawer,
			float rotationAngle,
			int rotationDirection,
			float rotationTime,
			Func<float, float> rotationEasing,
			float upwardHeight,
			float upwardTime,
			bool shouldReturn)
			: base(drawer) {
			dir = drawer.Player.direction;
			this.shouldReturn = shouldReturn;
			switch (drawer.Index) {
				case 0: {
					this.rotationAngle = MathHelper.ToRadians(-15*dir) ;
					this.rotationTime = 10;
					startUpTime =5;
					this.upwardTime =20;
					break;
				}
				case 1: {
					this.rotationAngle = MathHelper.ToRadians(-15*dir) ;
					this.rotationTime = 10;
					startUpTime =5;
					this.upwardTime =20;
					break;
				}
				case 2: {
					this.rotationAngle = MathHelper.ToRadians(-30*dir) ;
					this.rotationTime = 10;
					startUpTime =5;
					this.upwardTime =20;
					break;
				}
				case 3: {
					this.rotationAngle = MathHelper.ToRadians(-30*dir) ;
					this.rotationTime = 10;
					startUpTime =5;
					this.upwardTime =20;
					break;
				}
			}

			this.upwardHeight = 400;



			this.rotationDirection = Math.Sign(rotationDirection);
			this.rotationEasing = rotationEasing ?? EaseOutQuart;



			elapsedTime = 0f;
			isStarted = true;
			IsVisible = true;
			startRotation = drawer.Rotation;
			startPosition = drawer.Position;
			drawer.Velocity = Vector2.Zero;
			drawer.Acceleration = Vector2.Zero;
			MotionRotationOffset = 0f;
			SpriteEffects = dir == 1? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			if (rotationTime <= 0f && upwardTime <= 0f)
				FinishAnimation();
		}

		public override void StartShootAnimation(int indexs)
		{

		}

		protected override void UpdateAnimation()
		{
			if (!isStarted)
				return;

			float animationTime = MathHelper.Max(rotationTime, upwardTime + startUpTime);
			if (animationTime <= 0f)
				return;

			elapsedTime += 1f;


			float rotationProgress = rotationTime <= 0f
				? 1f
				: MathHelper.Clamp(elapsedTime / rotationTime, 0f, 1f);
			float rotationEasedProgress = rotationEasing(rotationProgress);
			drawer.Rotation = MathHelper.Lerp(startRotation, rotationAngle * rotationDirection, rotationEasedProgress);



			if (elapsedTime >= startUpTime) {
				float upwardProgress = upwardTime <= 0f
					? 1f
					: MathHelper.Clamp(elapsedTime /( upwardTime + startUpTime), 0f, 1f);
				float upwardEasedProgress = EaseInBack(upwardProgress);
				drawer.Position = startPosition + new Vector2(0f, -upwardHeight * upwardEasedProgress);
				drawer.Alpha = MathHelper.Lerp(1,0, upwardProgress) * 2;
			}
			else {
				drawer.Alpha = MathHelper.Lerp(	drawer.Alpha, 1f, rotationProgress);
			}


			drawer.Velocity = Vector2.Zero;
			drawer.Acceleration = Vector2.Zero;
			SpriteEffects = dir == 1? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			if (elapsedTime >= animationTime + 20)
				FinishAnimation();
		}

		private void FinishAnimation()
		{
			isStarted = false;
			drawer.Velocity = Vector2.Zero;
			drawer.Acceleration = Vector2.Zero;
			if (shouldReturn) {
				ExpectedAnimationType = RosmontiWeaponAnimationType.OnEquipingWeapon;
			}
			else {

			}

		}

		private static float EaseOutQuart(float progress)
		{
			float invertedProgress = 1f - progress;
			return 1f - invertedProgress * invertedProgress * invertedProgress * invertedProgress;
		}

		private static float EaseInBack(float progress)
		{
			const float c1 = 1.70158f;
			const float c3 = c1 + 1f;
			return c3 * progress * progress * progress - c1 * progress * progress;
		}
	}
}
