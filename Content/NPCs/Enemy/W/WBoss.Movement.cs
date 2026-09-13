using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	// W 的地面移动与传送落点搜索：算法沿用弑君者 Movement 的地表探测/脱困思路。
	// W 不能穿墙行走（传送可以穿），行走仅发生在物块/平台上。
	public partial class WBoss
	{
		private const int MoveSafeSearchRings = 24;

		private float Move_FootY => NPC.position.Y + NPC.height;

		private Vector2 MoveBoxTopLeft(Vector2 center) => center - NPC.Size * 0.5f;

		private static float Move_GetSlopeSurfaceOffset(SlopeType slope, float localX) {
			localX = MathHelper.Clamp(localX, 0f, 16f);
			return slope switch {
				SlopeType.SlopeDownLeft => 16f - localX,
				SlopeType.SlopeDownRight => localX,
				SlopeType.SlopeUpLeft => localX,
				SlopeType.SlopeUpRight => 16f - localX,
				_ => 0f
			};
		}

		// 从 fromFootY 向下探最多 6 格，找实体砖表面（不含平台——平台站立交给原版碰撞）
		private static bool Move_TryGetSurfaceY(float worldX, float fromFootY, out float surfaceFootY) {
			int tx = (int)(worldX / 16f);
			int startTy = (int)(fromFootY / 16f) - 2;

			for (int ty = startTy; ty <= startTy + 6; ty++) {
				if (!WorldGen.InWorld(tx, ty, 1))
					continue;

				Tile tile = Main.tile[tx, ty];
				if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
					continue;

				float surface = ty * 16f;
				if (tile.IsHalfBlock)
					surface += 8f;
				else if (tile.Slope != SlopeType.Solid)
					surface += Move_GetSlopeSurfaceOffset(tile.Slope, worldX - tx * 16f);

				if (fromFootY <= surface + 12f) {
					surfaceFootY = surface;
					return true;
				}
			}

			surfaceFootY = fromFootY;
			return false;
		}

		// 站立面检测：实体砖或平台皆可（传送落点允许落平台，符合策划「空地或平台」）
		private static bool Move_HasStandableBelow(Vector2 center, float halfHeight, out float footY) {
			int tx = (int)(center.X / 16f);
			int startTy = (int)((center.Y + halfHeight) / 16f) - 1;
			for (int ty = startTy; ty <= startTy + 8; ty++) {
				if (!WorldGen.InWorld(tx, ty, 1))
					continue;
				Tile tile = Main.tile[tx, ty];
				if (!tile.HasUnactuatedTile)
					continue;
				bool solid = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
				bool platform = Main.tileSolidTop[tile.TileType] || TileID.Sets.Platforms[tile.TileType];
				if (!solid && !platform)
					continue;
				footY = ty * 16f;
				return true;
			}
			footY = 0f;
			return false;
		}

		private bool Move_IsWallEmbedded(Vector2 center) {
			Vector2 box = MoveBoxTopLeft(center) + new Vector2(6f, 10f);
			int w = NPC.width - 12;
			int h = NPC.height - 18;
			return w > 8 && h > 8 && Collision.SolidCollision(box, w, h);
		}

		private bool Move_IsBlocked(Vector2 center) =>
			Collision.SolidCollision(MoveBoxTopLeft(center), NPC.width, NPC.height) && Move_IsWallEmbedded(center);

		// 传送/落地候选：脚下 8 格内有可站面且身体不嵌墙
		private bool Move_IsGoodLanding(Vector2 center) {
			if (Move_IsWallEmbedded(center) || Collision.SolidCollision(MoveBoxTopLeft(center), NPC.width, NPC.height))
				return false;
			return Move_HasStandableBelow(center, NPC.height * 0.5f, out _);
		}

		// 把候选点吸到站立面上（脚贴地）
		private Vector2 Move_SettleOnGround(Vector2 center) {
			if (Move_HasStandableBelow(center, NPC.height * 0.5f, out float footY))
				return new Vector2(center.X, footY - NPC.height * 0.5f);
			return center;
		}

		private Vector2 Move_FindOpen(Vector2 from) {
			if (Move_IsGoodLanding(from))
				return Move_SettleOnGround(from);

			for (int ring = 1; ring <= MoveSafeSearchRings; ring++) {
				for (int x = -ring; x <= ring; x++) {
					for (int y = -ring; y <= ring; y++) {
						if (Math.Abs(x) != ring && Math.Abs(y) != ring)
							continue;
						Vector2 p = from + new Vector2(x * 16f, y * 16f);
						if (Move_IsGoodLanding(p))
							return Move_SettleOnGround(p);
					}
				}
			}
			return from;
		}

		/// <summary>
		/// 计算传送目标（仅服务端/单机端调用）。<br/>
		/// Entrance：登场用，玩家身侧（W 刷新的那一侧）280~460px 的空地/平台；<br/>
		/// Approach：在 W 与玩家连线之间按列采样找空地/平台（近玩家侧优先）；<br/>
		/// DodgeAway：向玩家反方向 260~420px 找落点。全部失败逐级回退，最终原地。
		/// </summary>
		public Vector2 FindTeleportDest(Player target, TeleportKind kind) {
			if (kind == TeleportKind.Perch) {
				// 玩家头顶上方一段距离的可站面（平台/台阶），左右各试几档；找不到就退化为拉近
				foreach (float dx in new[] { 120f, -120f, 220f, -220f, 40f, -40f }) {
					foreach (float dy in new[] { -180f, -240f, -130f }) {
						Vector2 cand = target.Center + new Vector2(dx, dy);
						Vector2 landed = Move_FindOpen(cand);
						if (!Move_IsGoodLanding(landed))
							continue;
						if (landed.Y > target.Center.Y - 100f || Vector2.Distance(landed, target.Center) < 150f)
							continue;
						return landed;
					}
				}
				kind = TeleportKind.Approach;
			}

			if (kind == TeleportKind.CrossOver) {
				// 从玩家另一侧落地：越过玩家 240~320px
				float side = Math.Sign(target.Center.X - NPC.Center.X);
				if (side == 0)
					side = -target.direction;
				foreach (float dist in new[] { 280f, 240f, 320f, 200f }) {
					Vector2 cand = new(target.Center.X + side * dist, target.Center.Y - 32f);
					Vector2 landed = Move_FindOpen(cand);
					if (Move_IsGoodLanding(landed) && Vector2.Distance(landed, target.Center) >= 160f)
						return landed;
				}
				kind = TeleportKind.DodgeAway;
			}

			if (kind == TeleportKind.Entrance) {
				float side = Math.Sign(NPC.Center.X - target.Center.X);
				if (side == 0)
					side = -target.direction;
				foreach (float dist in new[] { 340f, 400f, 280f, 460f }) {
					Vector2 cand = new(target.Center.X + side * dist, target.Center.Y - 32f);
					Vector2 landed = Move_FindOpen(cand);
					if (Move_IsGoodLanding(landed) && Vector2.Distance(landed, target.Center) >= 200f)
						return landed;
				}
				kind = TeleportKind.Approach;
			}

			if (kind == TeleportKind.DodgeAway) {
				float dir = Math.Sign(NPC.Center.X - target.Center.X);
				if (dir == 0)
					dir = -target.direction;
				for (float dist = 420f; dist >= 200f; dist -= 55f) {
					Vector2 cand = new(NPC.Center.X + dir * dist, NPC.Center.Y - 32f);
					Vector2 landed = Move_FindOpen(cand);
					if (Move_IsGoodLanding(landed) || landed != cand)
						return landed;
				}
				// 反方向没路 → 退化为拉近打法（仍换位，避免贴脸）
				kind = TeleportKind.Approach;
			}

			// Approach：连线之间按 t 采样，偏向玩家一侧但保持最小 160px 距离
			Vector2 from = NPC.Center;
			Vector2 to = target.Center;
			float[] samples = { 0.62f, 0.5f, 0.72f, 0.38f, 0.82f, 0.28f };
			foreach (float t in samples) {
				Vector2 cand = Vector2.Lerp(from, to, t);
				cand.Y = Math.Min(cand.Y, to.Y + 64f); //别钻到玩家脚下太深
				Vector2 landed = Move_FindOpen(cand);
				if (!Move_IsGoodLanding(landed))
					continue;
				if (Vector2.Distance(landed, to) < 160f)
					continue;
				return landed;
			}
			// 兜底：玩家另一侧
			Vector2 fallback = new(to.X + Math.Sign(from.X - to.X) * -260f, to.Y - 32f);
			Vector2 fbLanded = Move_FindOpen(fallback);
			return Move_IsGoodLanding(fbLanded) ? fbLanded : from;
		}

		// 卡地形看门狗（WWalkState 消费）
		public int StuckTicks;
		private Vector2 stuckAnchor;

		/// <summary>从 point 往下找最多 maxTiles 格的实体砖/平台顶面，返回贴地点；找不到原样返回（曲射落点标记用）</summary>
		public Vector2 GroundBelow(Vector2 point, int maxTiles) {
			int tx = (int)(point.X / 16f);
			int startTy = (int)(point.Y / 16f);
			for (int ty = startTy; ty <= startTy + maxTiles; ty++) {
				if (!WorldGen.InWorld(tx, ty, 1))
					break;
				Tile tile = Main.tile[tx, ty];
				if (!tile.HasUnactuatedTile)
					continue;
				bool solid = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
				bool platform = Main.tileSolidTop[tile.TileType] || TileID.Sets.Platforms[tile.TileType];
				if (solid || platform)
					return new Vector2(point.X, ty * 16f);
			}
			return point;
		}

		/// <summary>
		/// 跃退投雷的落点体检：hopDir 方向约 110px 处身体不嵌墙、脚下 8 格内有可站面，且中途没有整面墙。
		/// 两个方向都不行则不起跳（调度层据此不把该动作放进池子）。
		/// </summary>
		public bool CanHopTo(int hopDir) {
			Vector2 land = NPC.Center + new Vector2(hopDir * 110f, -20f);
			if (!Move_IsGoodLanding(land))
				return false;
			Vector2 mid = NPC.Center + new Vector2(hopDir * 55f, -60f);
			return !Collision.SolidCollision(MoveBoxTopLeft(mid), NPC.width, NPC.height);
		}

		/// <summary>
		/// 行走物理（仅在行走类状态调用）：向 wantDir 加速/减速，带坡度助跳与平台下穿。
		/// </summary>
		public void Move_WalkTick(Player target, int wantDir) {
			const float maxSpeed = 2.6f;
			const float accel = 0.22f;
			const float friction = 0.16f;

			if (wantDir != 0) {
				NPC.velocity.X += wantDir * accel;
				// 前方台阶 ≤22px 时助跳
				if (NPC.collideX && NPC.velocity.Y >= -1f) {
					float footY = Move_FootY;
					if (Move_TryGetSurfaceY(NPC.Center.X, footY + 6f, out float hereY)
						&& Move_TryGetSurfaceY(NPC.Center.X + wantDir * 20f, footY + 10f, out float aheadY)) {
						float stepUp = hereY - aheadY;
						if (stepUp > 4f && stepUp <= 22f)
							NPC.velocity.Y = -6.2f;
					}
					else {
						NPC.velocity.Y = -6.2f; //前方探不到面：小跳试探（哥布林式）
					}
				}
			}
			else {
				if (NPC.velocity.X > 0f) {
					NPC.velocity.X = Math.Max(0f, NPC.velocity.X - friction);
				}
				else if (NPC.velocity.X < 0f) {
					NPC.velocity.X = Math.Min(0f, NPC.velocity.X + friction);
				}
			}
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxSpeed, maxSpeed);

			// 走起来就面朝移动方向（后撤时是转身跑而不是倒着滑步）；站定后由 AI() 的默认逻辑转回面向目标
			if (Math.Abs(NPC.velocity.X) > 1.2f)
				NPC.spriteDirection = NPC.velocity.X > 0f ? -1 : 1;

			// 卡地形监测：想走却原地不动累计计数
			if (wantDir != 0) {
				if (Vector2.DistanceSquared(stuckAnchor, NPC.position) < 4f)
					StuckTicks++;
				else {
					StuckTicks = Math.Max(0, StuckTicks - 3);
					stuckAnchor = NPC.position;
				}
			}
			else {
				StuckTicks = Math.Max(0, StuckTicks - 2);
				stuckAnchor = NPC.position;
			}

			// 目标在下方且脚下是平台 → 下穿
			if (target.Center.Y > NPC.Bottom.Y + 48f) {
				int tx = (int)(NPC.Center.X / 16f);
				int ty = (int)((NPC.position.Y + NPC.height + 8f) / 16f);
				if (WorldGen.InWorld(tx, ty, 1) && Main.tile[tx, ty].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[tx, ty].TileType])
					NPC.position.Y += 1f;
			}
		}

		/// <summary>
		/// 此面向敌布设点：玩家附近随机空地（可站立、彼此间距 ≥80px、离玩家 ≥48px）。
		/// 找不齐时空投兜底（带初速下落，靠弹幕自身落地布设）。
		/// </summary>
		public System.Collections.Generic.List<Vector2> FindClaymoreSpots(Player target, int count) {
			var spots = new System.Collections.Generic.List<Vector2>();
			for (int attempt = 0; attempt < 48 && spots.Count < count; attempt++) {
				Vector2 cand = target.Center + new Vector2(Main.rand.NextFloat(-420f, 420f), -32f);
				Vector2 landed = Move_FindOpen(cand);
				if (!Move_IsGoodLanding(landed))
					continue;
				if (Vector2.Distance(landed, target.Center) < 48f)
					continue;
				bool tooClose = false;
				foreach (Vector2 s in spots) {
					if (Vector2.Distance(s, landed) < 80f) {
						tooClose = true;
						break;
					}
				}
				if (tooClose)
					continue;
				spots.Add(landed);
			}
			while (spots.Count < count)
				spots.Add(target.Center + new Vector2(Main.rand.NextFloat(-220f, 220f), -96f));
			return spots;
		}

		/// <summary>状态机之后的统一贴地/防嵌墙修正（仅地面类状态生效）</summary>
		private void Move_PostState() {
			if (InTeleport)
				return;
			if (NPC.noTileCollide)
				return;

			if (Move_IsWallEmbedded(NPC.Center)) {
				Vector2 nudge = Move_FindOpen(NPC.Center);
				if (!Move_IsWallEmbedded(nudge)) {
					NPC.Center = nudge;
					NPC.velocity = Vector2.Zero;
					NPC.netUpdate = true;
				}
			}
		}
	}
}
