using System;
using System.IO;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Fiammetta
{
	/// <summary>
	/// 菲亚梅塔的曲射炮弹：先按炮口方向自然减速升空，到达最高点后才搜索落点附近的敌人，
	/// 只会轻微追踪鼠标十五格内且位于初始下落锥角中的目标；无敌人时落向开火时记录的鼠标位置。
	/// </summary>
	public class FiammettaMortarShell : ModProjectile
	{
		private const float AscentDrag = 0.995f;
		private const float TargetSearchRadius = 15f * 16f;
		private const float MinimumDiveSpeed = 5f;
		private const float MaximumDiveSpeed = 18f;
		// 以 Projectile.extraUpdates = 2 计数：120 次 AI 即 40 个真实游戏帧，和 Blissful Bombardier 的升空段一致。
		private const int AscentUpdates = 120;
		private const float SkyAnchorHeight = 600f;
		private static readonly float MaximumHomingAngle = MathHelper.ToRadians(32f);

		private bool initialized;
		private bool descending;
		private bool detonating;
		private int descentAge;
		private int lockedTargetIndex = -1;
		private float ballisticDiveHeading;
		private float gravityDirection = 1f;

		private Vector2 MouseTarget => new(Projectile.ai[0], Projectile.ai[1]);
		protected virtual int FlightMode => Math.Clamp((int)Projectile.ai[2], 0, 3);
		protected virtual Color TrailOuter => new(255, 55, 20);
		protected virtual Color TrailInner => new(255, 233, 160);
		protected virtual float TrailWidth => FlightMode == 3 ? 26f : 23f;
		private NPC LockedTarget {
			get {
				if (!Main.npc.IndexInRange(lockedTargetIndex))
					return null;
				NPC target = Main.npc[lockedTargetIndex];
				if (!target.CanBeChasedBy(Projectile, false)
					|| Vector2.DistanceSquared(MouseTarget, target.Center)
						> TargetSearchRadius * TargetSearchRadius)
					return null;
				return target;
			}
		}
		private Vector2 StrikePoint => LockedTarget?.Center ?? MouseTarget;

		public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RocketI}";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 36;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 28;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 700;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.netImportant = true;
			// 让上升段与参考迫击炮同样按每 tick 三次更新推进，保留高速升入天幕的距离感。
			Projectile.extraUpdates = 2;
		}

		public override bool ShouldUpdatePosition() => false;
		public override bool? CanDamage() => false;

		public override void AI() {
			if (!initialized)
				Initialize();

			if (descending)
				UpdateDescent();
			else
				UpdateAscent();

			if (Projectile.velocity.LengthSquared() > 0.01f)
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			float descentLight = descending ? MathHelper.Clamp(descentAge / 50f, 0f, 1f) : 0f;
			Lighting.AddLight(Projectile.Center,
				new Vector3(0.78f, 0.17f, 0.035f) * (0.42f + descentLight * 0.36f));
			SpawnFlightDust();
			Projectile.localAI[0]++;
		}

		private void Initialize() {
			initialized = true;
			gravityDirection = Main.player[Projectile.owner].gravDir < 0f ? -1f : 1f;
			if (Projectile.velocity.LengthSquared() < 0.01f)
				Projectile.velocity = -Vector2.UnitY * gravityDirection * ArtilleryBallistics.LaunchSpeed;
		}

		private void UpdateAscent() {
			Projectile.Center += Projectile.velocity;
			Projectile.velocity *= AscentDrag;
            if (Projectile.localAI[0] > 6) {
                bool hit = Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
                foreach (NPC npc in Main.ActiveNPCs)
                    if (npc.CanBeChasedBy(Projectile) && npc.Hitbox.Intersects(Projectile.Hitbox)) { hit = true; break; }
                if (hit) { Detonate(); return; }
            }

			if (Projectile.localAI[0] >= AscentUpdates)
				BeginDescent();
		}

		private void BeginDescent() {
			descending = true;
			descentAge = 0;
			// 这不是普通抛物线折返：炮弹先冲入高空，再从鼠标 X 坐标正上方的天幕压下来。
			// 清空旧轨迹，避免跨越整张屏幕的历史拖尾把“高空转入”画成错误的斜线。
			Player owner = Main.player[Projectile.owner];
			Projectile.Center = new Vector2(MouseTarget.X, owner.Center.Y - SkyAnchorHeight * gravityDirection);
			Array.Fill(Projectile.oldPos, Projectile.position);
			ballisticDiveHeading = (MouseTarget - Projectile.Center)
				.SafeNormalize(Vector2.UnitY).ToRotation();
			LockNearestTarget();

			Vector2 desiredDirection = GetConstrainedDiveDirection(StrikePoint);
			Vector2 fallingStart = new(Projectile.velocity.X * 0.35f, 2.6f * gravityDirection);
			Projectile.velocity = Vector2.Lerp(fallingStart,
				desiredDirection * MinimumDiveSpeed, 0.22f);
			if (Projectile.velocity.Y * gravityDirection < 1.5f)
				Projectile.velocity.Y = 1.5f * gravityDirection;
			Projectile.netUpdate = true;
		}

		private void UpdateDescent() {
			descentAge++;
			if (LockedTarget == null && descentAge % 10 == 1)
				LockNearestTarget();

			Vector2 strikePoint = StrikePoint;
			Vector2 desiredDirection = GetConstrainedDiveDirection(strikePoint);
			float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.18f,
				MinimumDiveSpeed, MaximumDiveSpeed);
			// 每次额外更新最多转约 1.4 度，并且整条追踪路径不能偏离原落点方向超过 32 度。
			float turnRate = MathHelper.Lerp(0.01f, 0.024f,
				Utils.GetLerpValue(0f, 72f, descentAge, true));
			float angle = Projectile.velocity.SafeNormalize(Vector2.UnitY).ToRotation()
				.AngleTowards(desiredDirection.ToRotation(), turnRate);
			Projectile.velocity = angle.ToRotationVector2() * speed;

			Vector2 nextCenter = Projectile.Center + Projectile.velocity;
			if (Collision.SolidCollision(nextCenter - Projectile.Size * 0.5f,
				Projectile.width, Projectile.height)) {
				Projectile.Center = nextCenter;
				Detonate();
				return;
			}

			Projectile.Center = nextCenter;
			NPC target = LockedTarget;
			bool reachedEnemy = target != null && target.Hitbox.Intersects(Projectile.Hitbox);
			float distance = Vector2.Distance(Projectile.Center, strikePoint);
			bool reachedPoint = target == null && distance <= 26f;
			bool passedClosePoint = distance <= 74f
				&& Vector2.Dot(strikePoint - Projectile.Center, Projectile.velocity) < 0f;
			if (reachedEnemy || reachedPoint || passedClosePoint)
				Detonate();
		}

		private void LockNearestTarget() {
            if (Projectile.owner != Main.myPlayer) return;
			NPC closest = null;
			float bestDistance = TargetSearchRadius;
			Vector2 intendedDirection = (MouseTarget - Projectile.Center).SafeNormalize(Vector2.UnitY);
			float intendedAngle = intendedDirection.ToRotation();
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy(Projectile, false))
					continue;
				float distance = Vector2.Distance(MouseTarget, npc.Center);
				if (distance >= bestDistance)
					continue;
				float candidateAngle = (npc.Center - Projectile.Center)
					.SafeNormalize(intendedDirection).ToRotation();
				if (MathF.Abs(MathHelper.WrapAngle(candidateAngle - intendedAngle)) > MaximumHomingAngle)
					continue;
				bestDistance = distance;
				closest = npc;
			}

			int newTargetIndex = closest?.whoAmI ?? -1;
			if (newTargetIndex == lockedTargetIndex)
				return;
			lockedTargetIndex = newTargetIndex;
			Projectile.netUpdate = true;
		}

		private Vector2 GetConstrainedDiveDirection(Vector2 strikePoint) {
			float desiredAngle = (strikePoint - Projectile.Center)
				.SafeNormalize(Vector2.UnitY).ToRotation();
			float constrainedAngle = ballisticDiveHeading.AngleTowards(desiredAngle, MaximumHomingAngle);
			return constrainedAngle.ToRotationVector2();
		}

		private void Detonate() {
			if (Projectile.owner != Main.myPlayer || detonating)
				return;
			detonating = true;
			Projectile.netUpdate = true;
			Projectile.Kill();
		}

		private void SpawnFlightDust() {
			ArtilleryFlightVisuals.Emit(Projectile, TrailOuter, TrailInner);
		}

		public override void OnKill(int timeLeft) {
			if (detonating && Projectile.owner == Main.myPlayer)
				SpawnExplosion(Math.Max(1, (int)MathF.Round(Projectile.damage * (Projectile.velocity.Y * gravityDirection > 0f ? 1.25f : 1f))));
		}

        protected virtual void SpawnExplosion(int damage) =>
            FiammettaExplosion.Spawn(Projectile.GetSource_FromThis(), Projectile.Center,
                damage, Projectile.knockBack, Projectile.owner, FlightMode, Projectile.CritChance);

		public override bool PreDraw(ref Color lightColor) {
			ArtilleryFlightVisuals.Draw(Projectile, TrailOuter, TrailInner, TrailWidth);

			Texture2D texture = TextureAssets.Projectile[ProjectileID.RocketI].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
				Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f,
				Projectile.scale, SpriteEffects.None);
			return false;
		}

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write(Projectile.localAI[0]);
			writer.Write(descending);
			writer.Write(detonating);
			writer.Write((short)lockedTargetIndex);
			writer.Write((short)Math.Clamp(descentAge, 0, short.MaxValue));
			writer.Write(ballisticDiveHeading);
			writer.Write(gravityDirection);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			bool wasDescending = descending;
			Projectile.localAI[0] = reader.ReadSingle();
			descending = reader.ReadBoolean();
			detonating = reader.ReadBoolean();
			lockedTargetIndex = reader.ReadInt16();
			descentAge = reader.ReadInt16();
			ballisticDiveHeading = reader.ReadSingle();
			gravityDirection = reader.ReadSingle() < 0f ? -1f : 1f;
			if (descending != wasDescending)
				Array.Fill(Projectile.oldPos, Projectile.position);
			initialized = true;
		}
	}
}
