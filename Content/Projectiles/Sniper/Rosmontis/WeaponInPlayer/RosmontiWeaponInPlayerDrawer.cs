using ArknightsMod.Content.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer
{
	public enum RosmontiWeaponAnimationType
	{
		None,
		Weaponing,
		OnEquipingWeapon,
		OnDisEquipingWeapon,
		Shoot,
		Skill3SpecialShoot
	}

	public class RosmontiWeaponInPlayerDrawer : IPlayerExtraDrawer
	{
		private Vector2 velocity;
		private Vector2 acceleration;
		private float rotation;
		private RosmontiWeaponInPlayerAnimationBase animation;
		private readonly int index;
		private readonly bool chasingPlayer;
		private readonly RosmontisWeaponRender render;
		private float equipAnimationTotalTime = 35;
		private Vector2 _equipAnimationDistanceFromTarget = new Vector2(25,350);
		private RosmontiWeaponAnimationType currentAnimationType = RosmontiWeaponAnimationType.None;

		private bool shouldReturn = true;
		public bool DrawInFrontOfPlayer { get; set; }
		public ref float Alpha => ref render.Alpha;
		public RosmontiWeaponAnimationType CurrentAnimationType
		{
			get => currentAnimationType;
			internal set
			{
				if (currentAnimationType == value && animation != null)
					return;

				currentAnimationType = value;
				animation = CreateAnimation(value);
			}
		}
		public bool DrawBehindPlayer { get; set; }

		public Player Player { get; set; }
		public ref Vector2 Position => ref render.Position;
		public int Index => index;
		public ref float Rotation => ref rotation;
		public Vector2 Velocity
		{
			get => velocity;
			set => velocity = value;
		}
		public Vector2 Acceleration
		{
			get => acceleration;
			set => acceleration = value;
		}
		public bool ChasingPlayer => chasingPlayer;
		public float EquipAnimationTotalTime
		{
			get => equipAnimationTotalTime;
			set => equipAnimationTotalTime = MathF.Max(0f, value);
		}
		public Vector2 EquipAnimationDistanceFromTarget
		{
			get => _equipAnimationDistanceFromTarget;
			set => _equipAnimationDistanceFromTarget = value;
		}
		public float ShootRotationAngle => MathHelper.ToRadians(90f);
		public int ShootRotationDirection => index == 0 || index == 2 ? -1 : 1;
		public float ShootRotationTime => 60f;
		public Func<float, float> ShootRotationEasing => EaseOutQuart;
		public float ShootUpwardHeight => 350f;
		public float ShootUpwardTime => 60f;
		public Texture2D Texture => render.Texture;
		public Texture2D GlowTexture => render.GlowTexture;
		public RosmontiWeaponInPlayerAnimationBase Animation => animation;
		public RosmontisWeaponRender Render => render;

		public RosmontiWeaponInPlayerDrawer(
			Player player,
			float rotation,
			bool chasingPlayer,
			Texture2D texture,
			Texture2D glowTexture,
			int index)
		{
			this.Player = player;
			this.chasingPlayer = chasingPlayer;
			this.index = index;
			this.rotation = rotation;
			render = new RosmontisWeaponRender(texture, glowTexture);
			CurrentAnimationType = RosmontiWeaponAnimationType.Weaponing;
		}

		public void Update()
		{
			animation.Update();
		}

		public void OnPrePlayerDraw()
		{
			if (DrawBehindPlayer && animation.IsVisible)
				Draw();
		}

		public void OnPrePlayerDrawPrimitive() {

		}

		public void OnPostPlayerDraw()
		{
			if (DrawInFrontOfPlayer && animation.IsVisible)
				Draw();
		}

		public void OnPostPlayerDrawPrimitive() {

		}

		private void Draw()
		{
			render.Rotation = animation.DrawRotation;
			render.SpriteEffects = animation.SpriteEffects;
			render.Draw();
		}

		public void StartOnEquipingWeaponAnimation()
		{
			CurrentAnimationType = RosmontiWeaponAnimationType.OnEquipingWeapon;
			animation.StartOnEquipingWeaponAnimation();
			shouldReturn = true;
		}

		private RosmontiWeaponInPlayerAnimationBase CreateAnimation(RosmontiWeaponAnimationType animationType)
		{
			int dir2 = index == 0|| index == 2? -1 : 1;

			return animationType switch
			{
				RosmontiWeaponAnimationType.Weaponing => new RosmontiWeaponInPlayerAnimation(this),
				RosmontiWeaponAnimationType.OnEquipingWeapon => new RosmontiWeaponInPlayerOnEquipingWeaponAnimation(
					this,
					EquipAnimationTotalTime,
					EquipAnimationDistanceFromTarget * Player.Directions * new Vector2(dir2, 1)),
				RosmontiWeaponAnimationType.OnDisEquipingWeapon => new RosmontiWeaponInPlayerOnDisEquipingWeaponAnimation(
					this,
					EquipAnimationTotalTime,
					EquipAnimationDistanceFromTarget * Player.Directions * new Vector2(dir2, 1)),
				RosmontiWeaponAnimationType.Shoot => new RosmontiWeaponInPlayerShootAnimation(
					this,
					ShootRotationAngle,
					ShootRotationDirection,
					ShootRotationTime,
					ShootRotationEasing,
					ShootUpwardHeight,
					ShootUpwardTime,
					shouldReturn),
				RosmontiWeaponAnimationType.Skill3SpecialShoot =>new RosmontiWeaponInPlayerShootAnimation(
					this,
					ShootRotationAngle,
					ShootRotationDirection,
					ShootRotationTime,
					ShootRotationEasing,
					ShootUpwardHeight,
					ShootUpwardTime,
					shouldReturn),
				_ => animation
			};
		}

		public void StartOnDisEquipingWeaponAnimation()
		{
			CurrentAnimationType = RosmontiWeaponAnimationType.OnDisEquipingWeapon;
			animation.StartOnDisEquipingWeaponAnimation();
		}

		public void StartShootAnimation(int indexs, bool shouldReturn = true)
		{

			this.shouldReturn = shouldReturn;
			CurrentAnimationType = RosmontiWeaponAnimationType.Shoot;
		}

		public void StartSpeicalShootAnimation(int indexs, bool shouldReturn = true)
		{
			this.shouldReturn = shouldReturn;
			CurrentAnimationType = RosmontiWeaponAnimationType.Skill3SpecialShoot;
		}

		private static float EaseOutQuart(float progress)
		{
			float invertedProgress = 1f - progress;
			return 1f - invertedProgress * invertedProgress * invertedProgress * invertedProgress;
		}
	}
}
