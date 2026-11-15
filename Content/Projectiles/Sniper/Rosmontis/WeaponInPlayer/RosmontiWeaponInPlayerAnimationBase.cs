using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer
{
	public abstract class RosmontiWeaponInPlayerAnimationBase
	{
		protected readonly RosmontiWeaponInPlayerDrawer drawer;

		protected float MotionRotationOffset;

		public RosmontiWeaponAnimationType ExpectedAnimationType { get;  set; } = RosmontiWeaponAnimationType.None;
		public SpriteEffects SpriteEffects { get; protected set; }
		public bool IsVisible { get; protected set; } = true;
		public float DrawRotation => drawer.Rotation + MotionRotationOffset;

		protected RosmontiWeaponInPlayerAnimationBase(RosmontiWeaponInPlayerDrawer drawer)
		{
			this.drawer = drawer;
		}

		public void Update()
		{
			if (ApplyExpectedAnimation())
				return;

			UpdateAnimation();
		}

		public virtual void StartOnEquipingWeaponAnimation()
		{

		}

		public virtual void StartOnDisEquipingWeaponAnimation()
		{

		}

		public virtual void StartShootAnimation(int indexs)
		{

		}

		protected abstract void UpdateAnimation();

		private bool ApplyExpectedAnimation()
		{
			if (ExpectedAnimationType == RosmontiWeaponAnimationType.None)
				return false;

			RosmontiWeaponAnimationType expectedAnimationType = ExpectedAnimationType;
			ExpectedAnimationType = RosmontiWeaponAnimationType.None;
			drawer.CurrentAnimationType = expectedAnimationType;

			if (expectedAnimationType == RosmontiWeaponAnimationType.OnEquipingWeapon)
				IsVisible = true;
			else if (expectedAnimationType == RosmontiWeaponAnimationType.OnDisEquipingWeapon)
				IsVisible = false;

			return true;
		}
	}
}
