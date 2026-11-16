using ArknightsMod.Content.Projectiles.Sniper.Rosmontis.WeaponInPlayer;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis
{
	public class RosmontisProjectileBase : ModProjectile
	{
		public enum State
		{
			Ready,
			Attack,
			InGround,
			Back,
			InNPC
		}
		private State state = State.Ready;

		private Vector2 BaseVelocity = Vector2.Zero;

		public Vector2 TargetPosition { get; set; }

		private RosmontisWeaponRender render;

		public static float Acceleration = 0.2f;

		public static float MaxVelocity = 30f;

		private bool IsCollideToGround;

		private int timer = 0;

		private int AftershockTimer;

		private int AftershockTimerMax = 15;

		private int InGroundTime = 60;

		private Vector2 InGroundPosition = Vector2.Zero;

		public int SkillID => (int)Projectile.ai[0];


		private int TargetIndex {
			set => Projectile.ai[1] = value;
			get => (int)Projectile.ai[1];
		}
		public Vector2 TargetOffest;
		public override void SetDefaults() {
			Projectile.width = 34;
			Projectile.height = 80;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 600;
			Projectile.extraUpdates = 1;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 60;
		}

		public override void OnSpawn(IEntitySource source) {
			string texturePath = SkillID switch {
				1 => "ArknightsMod/Content/Projectiles/Sniper/Rosmontis/RosmontisProjectile_Skill2",
				_ => "ArknightsMod/Content/Projectiles/Sniper/Rosmontis/RosmontisProjectileBase",
			};
			string textureGlowPath = SkillID switch {
				1 => "ArknightsMod/Content/Projectiles/Sniper/Rosmontis/RosmontisProjectile_Skill2_Glow",
				_ => "ArknightsMod/Content/Projectiles/Sniper/Rosmontis/RosmontisProjectileBase_Glow",
			};
			switch (SkillID) {
				case 1: {
					Projectile.width = 58;
					break;
				}
			}

			Main.NewText(SkillID);
			Texture2D texture  = ModContent.Request<Texture2D>(texturePath).Value;
			Texture2D glow_texture  = ModContent.Request<Texture2D>(textureGlowPath).Value;
			render = new RosmontisWeaponRender(texture,glow_texture);
			BaseVelocity = Projectile.velocity;
			MaxVelocity = BaseVelocity.Length() * 2;

		}

		private bool triggered = false;
		public override void AI() {
			timer++;
			AftershockTimer--;
			if (!IsCollideToGround) {
				Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
			}
			switch (state) {
				case State.Ready:
					if (timer < 40) {
						Projectile.velocity = Vector2.Zero;
						Projectile.rotation =BaseVelocity.ToRotation() - MathHelper.PiOver2;
						if (SkillID ==3) {
							BaseVelocity = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.Zero) * BaseVelocity.Length();
						}
						return;
					}
					else {
						timer = 0;
						state = State.Attack;
						Projectile.velocity = BaseVelocity;
					}
					break;
				case State.Attack:
					RecordTrails();
					Projectile.velocity = Math.Clamp(Projectile.velocity.Length() * (1 + Acceleration),0,MaxVelocity) * Projectile.velocity.SafeNormalize(Vector2.Zero);

					if (CheckRotatedTileCollision(Projectile.Center, Projectile.Size * 0.5f, Projectile.rotation, Projectile.velocity, out bool isPlatform, out Vector2 collisionPoint, out Vector2 collisionCenter)) {
						Projectile.Center = collisionCenter;
						if (isPlatform) {

						}
						else {
							IsCollideToGround = true;
							Projectile.velocity = Vector2.Zero;
							state = State.InGround;
							timer = 0;
							InGroundPosition = Projectile.Center;
						}
						int aftershockProjectileType =  ModContent.ProjectileType<RosmontisProjectileExplosionInGround>();
						float radius = SkillID switch {
							1 => 15f,
							_ => 1
						};
						Vector2 point = new Vector2((Projectile.Center.X + collisionPoint.X)/2, collisionPoint.Y);
						TryTriggerAftershock(point,aftershockProjectileType,radius,!isPlatform);
					}

					break;
				case State.InGround:
					if (timer >InGroundTime ) {
						timer = 0;
						state = State.Back;
						Projectile.timeLeft = 40;
					}
					RecordTrails();
					break;
				case State.Back: {
					RecordTrails();
					float progress =Math.Clamp( timer / 40f,0,1);
					progress = EaseOutQuart(progress);
					PlaySkill4ExitAnimation();
					Projectile.Center = Vector2.Lerp(InGroundPosition, InGroundPosition - BaseVelocity.SafeNormalize(Vector2.Zero) * 150, progress);
				}
					break;
				case State.InNPC: {
					NPC target = Main.npc[TargetIndex];
					if (target is not null&&target.active) {
						Projectile.Center = target.Center + TargetOffest;
						Projectile.timeLeft = 100;
						RecordTrails();

					}
					else {
						Skill4Exit();
					}

					WeaponPlayer weaponPlayer = Main.player[Projectile.owner].GetModPlayer<WeaponPlayer>();
					if (weaponPlayer != null && !weaponPlayer.SkillActive) {
						Skill4Exit();
					}
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}


		}

		public override bool ShouldUpdatePosition() {
			switch (state) {
				case State.Ready:
					return false;
				case State.Attack:
					break;
				case State.InGround:
					return false;
				case State.Back:
					return false;
				case State.InNPC:
					return false;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			base.ShouldUpdatePosition();
			return true;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			switch (state) {
				case State.Ready:
					return false;
				case State.Attack:
					break;
				case State.InGround:
					return false;
				case State.Back:
					break;
				case State.InNPC:
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return base.Colliding(projHitbox, targetHitbox);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (SkillID ==4 && state != State.InNPC && state == State.Attack) {
				TargetIndex = target.whoAmI;
				state = State.InNPC;
				TargetOffest = target.Center - Projectile.Center;
				RosmontisModNPC modNPC = target.GetGlobalNPC<RosmontisModNPC>();
				modNPC.TryAddTagged(target);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			switch (state) {
				case State.Ready:

					render.Alpha = MathHelper.Clamp((float)(timer -15) / 15f, 0, 1);
					break;
				case State.Attack:


					break;
				case State.InGround:
					break;
				case State.Back: {
					float progress =Math.Clamp( timer / 40f,0,1);
					progress = EaseOutQuart(progress);
					render.Alpha = MathHelper.Lerp(1, 0, progress);
				}
					break;
				case State.InNPC:
					render.Alpha =1;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}





			render.Position = Projectile.Center;
			render.Rotation =Projectile.rotation;
			render.SpriteEffects = SpriteEffects.None;
			render?.Draw();


			return false;
		}

		private void RecordTrails() {
			if (render is not null && timer % 2 ==0) {
				render.TrailPositions.Insert(0,Projectile.Center);
				if (render.TrailPositions.Count > 5) {
					render.TrailPositions.RemoveAt(render.TrailPositions.Count - 1);
				}
			}
		}

		private void TryTriggerAftershock(Vector2 position,int projType,float scale,bool force = false) {
			if (AftershockTimer <=0 || force) {
				AftershockTimer = AftershockTimerMax;


				 Projectile aftershock = Terraria.Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), position,
					 Vector2.Zero, projType, Projectile.damage, Projectile.knockBack, Projectile.owner);
				 aftershock.scale = scale;
				 aftershock.netUpdate = true;

			}
		}

		private  float EaseOutQuart(float progress)
		{
			float invertedProgress = 1f - progress;
			return 1f - invertedProgress * invertedProgress * invertedProgress * invertedProgress;
		}

		public void Skill4Exit() {
			timer = 0;
			this.state  = State.Back;
			Projectile.timeLeft = 40;
			PlaySkill4ExitAnimation();
		}

		private void PlaySkill4ExitAnimation() {
			if (SkillID ==4 && !triggered) {
				triggered = true;
				NPC target = Main.npc[TargetIndex];
				if (target is not null) {
					RosmontisModNPC modNPC = target.GetGlobalNPC<RosmontisModNPC>();
					modNPC.TryRemoveTagged(target);
					var modplayer = Main.player[Projectile.owner].GetModPlayer<RosmontisModPlayer>();
					modplayer.TryReturnSkill4Projectile(this.Projectile.whoAmI);
				}
			}
		}




		#region 碰撞检测

		private static bool CheckRotatedTileCollision(Vector2 center, Vector2 size, float rotation, Vector2 velocity, out bool isPlatform, out Vector2 collisionPoint, out Vector2 collisionCenter)
		{
			collisionCenter = center;
			isPlatform = false;
			collisionPoint = center;

			if (CheckRotatedTileCollisionAtPosition(
					center, size, rotation, velocity,
					out isPlatform, out collisionPoint))
				return true;

			if (velocity.LengthSquared() <= 0.0001f)
				return false;

			int sampleCount = Math.Clamp(
				(int)MathF.Ceiling(velocity.Length() / 2f), 4, 32);
			float previousT = 0f;
			float hitT = 1f;
			bool found = false;
			bool sampledPlatform = false;
			Vector2 sampledPoint = center;

			for (int i = 1; i <= sampleCount; i++) {
				float t = i / (float)sampleCount;
				Vector2 sampleCenter = center + velocity * t;
				if (!CheckRotatedTileCollisionAtPosition(
						sampleCenter, size, rotation, velocity,
						out sampledPlatform, out sampledPoint)) {
					previousT = t;
					continue;
				}

				hitT = t;
				found = true;
				break;
			}

			if (!found)
				return false;

			for (int i = 0; i < 10; i++) {
				float middleT = (previousT + hitT) * 0.5f;
				Vector2 middleCenter = center + velocity * middleT;
				if (CheckRotatedTileCollisionAtPosition(
						middleCenter, size, rotation, velocity,
						out sampledPlatform, out sampledPoint))
					hitT = middleT;
				else
					previousT = middleT;
			}

			collisionCenter = center + velocity * hitT;
			if (CheckRotatedTileCollisionAtPosition(
					collisionCenter, size, rotation, velocity,
					out isPlatform, out collisionPoint))
				return true;

			isPlatform = sampledPlatform;
			collisionPoint = sampledPoint;
			return true;
		}

		private static bool CheckRotatedTileCollisionAtPosition(Vector2 center, Vector2 size, float rotation, Vector2 velocity, out bool isPlatform, out Vector2 collisionPoint)
		{
			Vector2 halfSize = size * 0.5f;
			float cos = MathF.Cos(rotation);
			float sin = MathF.Sin(rotation);

			Vector2 axisX = new(cos, sin);
			Vector2 axisY = new(-sin, cos);

			float boundHalfWidth =
				MathF.Abs(axisX.X) * halfSize.X +
				MathF.Abs(axisY.X) * halfSize.Y;
			float boundHalfHeight =
				MathF.Abs(axisX.Y) * halfSize.X +
				MathF.Abs(axisY.Y) * halfSize.Y;

			int minTileX = Math.Clamp(
				(int)MathF.Floor((center.X - boundHalfWidth) / 16f),
				0,
				Main.maxTilesX - 1);
			int maxTileX = Math.Clamp(
				(int)MathF.Floor((center.X + boundHalfWidth) / 16f),
				0,
				Main.maxTilesX - 1);
			int minTileY = Math.Clamp(
				(int)MathF.Floor((center.Y - boundHalfHeight) / 16f),
				0,
				Main.maxTilesY - 1);
			int maxTileY = Math.Clamp(
				(int)MathF.Floor((center.Y + boundHalfHeight) / 16f),
				0,
				Main.maxTilesY - 1);

			isPlatform = false;
			collisionPoint = center;
			bool found = false;
			float lowestY = float.MinValue;

			for (int tileX = minTileX; tileX <= maxTileX; tileX++) {
				for (int tileY = minTileY; tileY <= maxTileY; tileY++) {
					Tile tile = Framing.GetTileSafely(tileX, tileY);
					if (!tile.HasUnactuatedTile)
						continue;

					ushort tileType = tile.TileType;
					bool tileIsPlatform =
						TileID.Sets.Platforms[tileType] ||
						Main.tileSolidTop[tileType];
					bool tileIsSolidBlock =
						Main.tileSolid[tileType] && !tileIsPlatform;

					if (!tileIsPlatform && !tileIsSolidBlock)
						continue;

					// 平台只接受从上方朝下的碰撞；完整方块支持四个方向。
					if (tileIsPlatform && velocity.Y <= 0f)
						continue;

					Rectangle tileBounds = new(
						tileX * 16,
						tileY * 16,
						16,
						tileIsPlatform ? 4 : 16);

					if (!RotatedRectangleIntersectsAabb(
							center, halfSize, axisX, axisY, tileBounds))
						continue;

					Vector2 point;
					bool hasPoint;
					if (tileIsPlatform) {
						// 平台只使用顶部碰撞平面，不把平台的侧面或底面当作碰撞。
						hasPoint = TryGetHorizontalPlaneContactPoint(
								center, halfSize, axisX, axisY,
								tileBounds.Left, tileBounds.Right,
								tileBounds.Top, out point);
					}
					else {
						// 完整方块直接求 OBB 与 16x16 AABB 的实际边界交点，
						// 不再根据速度主轴猜测碰撞面。
						hasPoint = TryGetLowestAabbContactPoint(
								center, halfSize, axisX, axisY,
								tileBounds, out point);
					}

					if (!hasPoint)
						continue;

					// Terraria 世界坐标中 Y 越大越靠下。
					if (!found || point.Y > lowestY) {
						found = true;
						lowestY = point.Y;
						collisionPoint = point;
						isPlatform = tileIsPlatform;
					}
				}
			}

			return found;
		}

		private static bool TryGetLowestAabbContactPoint(Vector2 center, Vector2 halfSize, Vector2 axisX, Vector2 axisY, Rectangle aabb, out Vector2 point)
		{
			Vector2[] obb = GetRotatedRectangleCorners(
				center, halfSize, axisX, axisY);
			Vector2[] box = {
				new(aabb.Left, aabb.Top),
				new(aabb.Right, aabb.Top),
				new(aabb.Right, aabb.Bottom),
				new(aabb.Left, aabb.Bottom)
			};
			List<Vector2> contacts = new(16);

			// OBB 边和方块边的交点。
			for (int i = 0; i < 4; i++) {
				Vector2 a = obb[i];
				Vector2 b = obb[(i + 1) % 4];
				for (int j = 0; j < 4; j++) {
					if (TryGetSegmentIntersection(
							a, b, box[j], box[(j + 1) % 4], out Vector2 hit))
						contacts.Add(hit);
				}
			}

			// 一个图形完全包含另一个图形时没有边交点，需要补充顶点。
			for (int i = 0; i < 4; i++) {
				if (PointInAabb(obb[i], aabb))
					contacts.Add(obb[i]);
				if (PointInObb(box[i], center, halfSize, axisX, axisY))
					contacts.Add(box[i]);
			}

			if (contacts.Count == 0) {
				point = default;
				return false;
			}

			point = contacts[0];
			for (int i = 1; i < contacts.Count; i++) {
				if (contacts[i].Y > point.Y)
					point = contacts[i];
			}
			return true;
		}

		private static bool TryGetSegmentIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 point)
		{
			Vector2 r = b - a;
			Vector2 s = d - c;
			float denominator = Cross(r, s);
			Vector2 cMinusA = c - a;

			if (MathF.Abs(denominator) < 0.0001f) {
				if (MathF.Abs(Cross(cMinusA, r)) >= 0.0001f) {
					point = default;
					return false;
				}

				if (PointOnSegment(a, c, d)) { point = a; return true; }
				if (PointOnSegment(b, c, d)) { point = b; return true; }
				if (PointOnSegment(c, a, b)) { point = c; return true; }
				if (PointOnSegment(d, a, b)) { point = d; return true; }
				point = default;
				return false;
			}

			float t = Cross(cMinusA, s) / denominator;
			float u = Cross(cMinusA, r) / denominator;
			if (t < -0.0001f || t > 1.0001f || u < -0.0001f || u > 1.0001f) {
				point = default;
				return false;
			}

			point = a + r * MathHelper.Clamp(t, 0f, 1f);
			return true;
		}

		private static bool PointOnSegment(Vector2 point, Vector2 start, Vector2 end)
		{
			return point.X >= MathF.Min(start.X, end.X) - 0.0001f &&
				point.X <= MathF.Max(start.X, end.X) + 0.0001f &&
				point.Y >= MathF.Min(start.Y, end.Y) - 0.0001f &&
				point.Y <= MathF.Max(start.Y, end.Y) + 0.0001f;
		}

		private static bool PointInAabb(Vector2 point, Rectangle aabb)
		{
			return point.X >= aabb.Left - 0.0001f &&
				point.X <= aabb.Right + 0.0001f &&
				point.Y >= aabb.Top - 0.0001f &&
				point.Y <= aabb.Bottom + 0.0001f;
		}

		private static bool PointInObb(Vector2 point, Vector2 center, Vector2 halfSize, Vector2 axisX, Vector2 axisY)
		{
			Vector2 delta = point - center;
			return MathF.Abs(Vector2.Dot(delta, axisX)) <= halfSize.X + 0.0001f &&
				MathF.Abs(Vector2.Dot(delta, axisY)) <= halfSize.Y + 0.0001f;
		}

		private static float Cross(Vector2 a, Vector2 b)
		{
			return a.X * b.Y - a.Y * b.X;
		}

		private static bool TryGetHorizontalPlaneContactPoint(Vector2 center, Vector2 halfSize, Vector2 axisX, Vector2 axisY, float minX, float maxX, float planeY, out Vector2 point)
		{
			Vector2[] corners = GetRotatedRectangleCorners(
				center, halfSize, axisX, axisY);
			List<float> intersections = new();
			for (int i = 0; i < corners.Length; i++) {
				Vector2 a = corners[i];
				Vector2 b = corners[(i + 1) % corners.Length];
				float dy = b.Y - a.Y;

				if (MathF.Abs(dy) < 0.0001f) {
					if (MathF.Abs(a.Y - planeY) < 0.0001f) {
						intersections.Add(a.X);
						intersections.Add(b.X);
					}
					continue;
				}

				float t = (planeY - a.Y) / dy;
				if (t >= 0f && t <= 1f)
					intersections.Add(a.X + (b.X - a.X) * t);
			}

			return GetClampedIntervalMidpoint(intersections, minX, maxX, planeY, out point);
		}

		private static bool TryGetVerticalPlaneContactPoint(Vector2 center, Vector2 halfSize, Vector2 axisX, Vector2 axisY, float minY, float maxY, float planeX, out Vector2 point)
		{
			Vector2[] corners = GetRotatedRectangleCorners(
				center, halfSize, axisX, axisY);
			List<float> intersections = new();
			for (int i = 0; i < corners.Length; i++) {
				Vector2 a = corners[i];
				Vector2 b = corners[(i + 1) % corners.Length];
				float dx = b.X - a.X;

				if (MathF.Abs(dx) < 0.0001f) {
					if (MathF.Abs(a.X - planeX) < 0.0001f) {
						intersections.Add(a.Y);
						intersections.Add(b.Y);
					}
					continue;
				}

				float t = (planeX - a.X) / dx;
				if (t >= 0f && t <= 1f)
					intersections.Add(a.Y + (b.Y - a.Y) * t);
			}

			if (intersections.Count == 0) {
				point = default;
				return false;
			}

			float lower = MathF.Max(minY, intersections.Min());
			float upper = MathF.Min(maxY, intersections.Max());
			if (lower > upper + 0.0001f) {
				point = default;
				return false;
			}

			// Y 越大越靠下，返回竖直接触面上最下方的有效点。
			point = new Vector2(planeX, upper);
			return true;
		}

		private static bool GetClampedIntervalMidpoint(List<float> intersections, float minX, float maxX, float planeY, out Vector2 point)
		{
			if (intersections.Count == 0) {
				point = default;
				return false;
			}

			float lower = MathF.Max(minX, intersections.Min());
			float upper = MathF.Min(maxX, intersections.Max());
			if (lower > upper + 0.0001f) {
				point = default;
				return false;
			}

			point = new Vector2((lower + upper) * 0.5f, planeY);
			return true;
		}

		private static Vector2[] GetRotatedRectangleCorners(Vector2 center, Vector2 halfSize, Vector2 axisX, Vector2 axisY)
		{
			return new[] {
				center - axisX * halfSize.X - axisY * halfSize.Y,
				center + axisX * halfSize.X - axisY * halfSize.Y,
				center + axisX * halfSize.X + axisY * halfSize.Y,
				center - axisX * halfSize.X + axisY * halfSize.Y
			};
		}

		private static bool RotatedRectangleIntersectsAabb(Vector2 rotatedCenter, Vector2 rotatedHalfSize, Vector2 rotatedAxisX, Vector2 rotatedAxisY, Rectangle rectangle) { Vector2 rectangleCenter = new(rectangle.Center.X, rectangle.Center.Y);

			Vector2 rectangleHalfSize = new(
				rectangle.Width * 0.5f,
				rectangle.Height * 0.5f
			);

			Vector2 centerDelta =
				rectangleCenter - rotatedCenter;

			float absAxisXX = MathF.Abs(rotatedAxisX.X);
			float absAxisXY = MathF.Abs(rotatedAxisX.Y);
			float absAxisYX = MathF.Abs(rotatedAxisY.X);
			float absAxisYY = MathF.Abs(rotatedAxisY.Y);

			// 检查旋转矩形局部 X 轴
			float distanceOnRotatedX =
				MathF.Abs(Vector2.Dot(centerDelta, rotatedAxisX));

			float rectangleRadiusOnRotatedX =
				rectangleHalfSize.X * absAxisXX +
				rectangleHalfSize.Y * absAxisXY;

			if (distanceOnRotatedX >
			    rotatedHalfSize.X + rectangleRadiusOnRotatedX) {
				return false;
			}

			// 检查旋转矩形局部 Y 轴
			float distanceOnRotatedY =
				MathF.Abs(Vector2.Dot(centerDelta, rotatedAxisY));

			float rectangleRadiusOnRotatedY =
				rectangleHalfSize.X * absAxisYX +
				rectangleHalfSize.Y * absAxisYY;

			if (distanceOnRotatedY >
			    rotatedHalfSize.Y + rectangleRadiusOnRotatedY) {
				return false;
			}

			// 检查世界 X 轴
			float rotatedRadiusOnWorldX =
				rotatedHalfSize.X * absAxisXX +
				rotatedHalfSize.Y * absAxisYX;

			if (MathF.Abs(centerDelta.X) >
			    rectangleHalfSize.X + rotatedRadiusOnWorldX) {
				return false;
			}

			// 检查世界 Y 轴
			float rotatedRadiusOnWorldY =
				rotatedHalfSize.X * absAxisXY +
				rotatedHalfSize.Y * absAxisYY;

			if (MathF.Abs(centerDelta.Y) >
			    rectangleHalfSize.Y + rotatedRadiusOnWorldY) {
				return false;
			}

			return true;
		}

		#endregion
	}
}
