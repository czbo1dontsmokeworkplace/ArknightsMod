using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4
{
	// 弑君者地面移动与脱困：贴地、嵌墙、脱困逻辑集中在此，主 AI 只调用公开入口。
	public partial class Crownslayer
	{
		private const int MoveStuckThreshold = 48;
		private const int MoveWallBumpUnstuck = 28;
		private const int MoveUnstuckCooldown = 90;
		private const int MoveSafeSearchRings = 24;

		private int moveStuckTicks;
		private int moveWallBumpTicks;
		private int moveUnstuckCooldown;
		private Vector2 moveStuckAnchor;

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

		// 半身以上嵌入墙体（不含正常站在斜坡/半砖上）。
		private bool Move_IsWallEmbedded(Vector2 center) {
			Vector2 box = MoveBoxTopLeft(center) + new Vector2(6f, 10f);
			int w = NPC.width - 12;
			int h = NPC.height - 18;
			return w > 8 && h > 8 && Collision.SolidCollision(box, w, h);
		}

		private bool Move_IsBlocked(Vector2 center) {
			if (!Collision.SolidCollision(MoveBoxTopLeft(center), NPC.width, NPC.height))
				return false;

			if (Move_IsWallEmbedded(center))
				return true;

			return !Move_HasGround(center);
		}

		private bool Move_HasGround(Vector2 center) {
			float footProbe = center.Y + NPC.height * 0.5f + 6f;
			return Move_TryGetSurfaceY(center.X, footProbe, out float surfaceY)
				&& footProbe >= surfaceY - 4f
				&& footProbe <= surfaceY + 18f;
		}

		private bool Move_NeedsLanding(Vector2 center) =>
			Move_IsWallEmbedded(center) || !Move_HasGround(center);

		private bool Move_TryCompareSurfaceAhead(int dir, out float hereY, out float aheadY) {
			hereY = 0f;
			aheadY = 0f;
			float footY = Move_FootY;
			if (!Move_TryGetSurfaceY(NPC.Center.X, footY + 6f, out hereY))
				return false;

			return Move_TryGetSurfaceY(NPC.Center.X + dir * 20f, footY + 10f, out aheadY);
		}

		private void Move_AdhereToGround() {
			if (!Move_UsesGroundTick() || NPC.noTileCollide || NPC.velocity.Y < -5f)
				return;

			if (!Move_TryGetSurfaceY(NPC.Center.X, Move_FootY + 6f, out float surfaceY))
				return;

			float delta = surfaceY - Move_FootY;
			if (Math.Abs(delta) <= 8f)
				NPC.position.Y += delta;
			else if (delta > 0f && delta <= 16f)
				NPC.position.Y += Math.Min(delta, 3f);
		}

		// 斜坡/台阶方向仍有路时，不因 collideX 停住。
		private bool Move_AllowHorizontalAccel(int dir) {
			if (!NPC.collideX)
				return true;

			return Move_TryCompareSurfaceAhead(dir, out float hereY, out float aheadY)
				&& Math.Abs(hereY - aheadY) >= 2f;
		}

		private void Move_TickSlopeAssist(bool wantsMoveX, float diffX) {
			if (!wantsMoveX || NPC.noTileCollide)
				return;

			int dir = diffX > 0 ? 1 : -1;
			if (Move_TryCompareSurfaceAhead(dir, out float hereSurf, out float aheadSurf)) {
				float stepUp = hereSurf - aheadSurf;
				if (stepUp > 4f && stepUp <= 22f && NPC.velocity.Y >= -1f)
					NPC.velocity.Y = -6f;
			}

			if (NPC.collideY && Math.Abs(NPC.velocity.X) < 0.6f && Math.Abs(NPC.velocity.Y) < 1.2f)
				NPC.position.Y -= 1f;
		}

		private Vector2 Move_FindOpen(Vector2 from, bool requireGround) {
			if (!Move_IsBlocked(from) && (!requireGround || Move_HasGround(from)))
				return from;

			for (int ring = 1; ring <= MoveSafeSearchRings; ring++) {
				for (int x = -ring; x <= ring; x++) {
					for (int y = -ring; y <= ring; y++) {
						if (Math.Abs(x) != ring && Math.Abs(y) != ring)
							continue;

						Vector2 p = from + new Vector2(x * 16f, y * 16f);
						if (Move_IsBlocked(p) || (requireGround && !Move_HasGround(p)))
							continue;

						return p;
					}
				}
			}

			return from;
		}

		private Vector2 Move_SnapToGround(Vector2 desired) {
			if (Move_TryGetSurfaceY(desired.X, desired.Y + NPC.height + 8f, out float surfaceY)) {
				Vector2 center = new Vector2(desired.X, surfaceY - NPC.height * 0.5f);
				if (!Move_IsBlocked(center) && Move_HasGround(center))
					return center;
			}

			for (int dy = 0; dy <= 16; dy++) {
				Vector2 lower = desired + new Vector2(0f, dy * 4f);
				if (!Move_IsBlocked(lower) && Move_HasGround(lower))
					return lower;
			}

			for (int dy = 1; dy <= 12; dy++) {
				Vector2 higher = desired - new Vector2(0f, dy * 4f);
				if (!Move_IsBlocked(higher) && Move_HasGround(higher))
					return higher;
			}

			return Move_FindOpen(desired, requireGround: false);
		}

		private Vector2 Move_FindStandableNearPlayer(Player target) {
			Vector2 best = Move_SnapToGround(target.Center + new Vector2(0f, -64f));
			float bestDist = Vector2.DistanceSquared(best, NPC.Center);

			for (int ring = 2; ring <= 16; ring++) {
				for (int x = -ring; x <= ring; x++) {
					for (int y = -2; y <= ring; y++) {
						if (Math.Abs(x) != ring && y != ring && y != -2)
							continue;

						Vector2 c = target.Center + new Vector2(x * 16f, y * 16f);
						if (Move_IsBlocked(c) || !Move_HasGround(c))
							continue;

						float d = Vector2.DistanceSquared(c, NPC.Center);
						if (d < bestDist) {
							bestDist = d;
							best = c;
						}
					}
				}
			}

			return best;
		}

		private bool Move_UsesGroundTick() {
			if (NPC.noTileCollide)
				return false;

			switch (CurrentAIState) {
				case AIState.Idle:
				case AIState.Recover:
				case AIState.Skill_1:
				case AIState.Skill_2:
				case AIState.Skill_8:
				case AIState.Skill_9:
					return true;
				default:
					return false;
			}
		}

		private void Move_ResetWatch() {
			moveStuckTicks = 0;
			moveWallBumpTicks = 0;
			moveStuckAnchor = NPC.position;
		}

		private void Move_BeginFrame() {
			if (moveUnstuckCooldown > 0)
				moveUnstuckCooldown--;
		}

		private void Move_AfterStateMachine(Player target) {
			if (!Move_UsesGroundTick())
				return;

			Move_AdhereToGround();

			if (!Move_IsWallEmbedded(NPC.Center))
				return;

			Vector2 nudge = Move_SnapToGround(Move_FindOpen(NPC.Center, requireGround: true));
			if (!Move_IsWallEmbedded(nudge)) {
				NPC.Center = nudge;
				NPC.velocity = Vector2.Zero;
				NPC.netUpdate = true;
				Move_ResetWatch();
				return;
			}

			if (moveUnstuckCooldown <= 0)
				Move_ForceUnstuck(target);
		}

		private void Move_TickIdleChase(Player target, bool wantsMoveX, float diffX) {
			Move_TickSlopeAssist(wantsMoveX, diffX);

			int moveDir = diffX > 0 ? 1 : -1;
			if (NPC.collideX && wantsMoveX && !Move_AllowHorizontalAccel(moveDir)) {
				NPC.velocity.X = 0f;
				moveWallBumpTicks++;
				float dy = target.Center.Y - NPC.Center.Y;
				if (moveWallBumpTicks >= 6 && dy > -56f && dy < 80f && NPC.velocity.Y <= 0.5f)
					NPC.velocity.Y = -6.5f;
				if (moveWallBumpTicks >= MoveWallBumpUnstuck && moveUnstuckCooldown <= 0)
					Move_ForceUnstuck(target);
			}
			else {
				moveWallBumpTicks = Math.Max(0, moveWallBumpTicks - 2);
			}

			if (!wantsMoveX || NPC.noTileCollide || moveUnstuckCooldown > 0) {
				moveStuckTicks = Math.Max(0, moveStuckTicks - 2);
				moveStuckAnchor = NPC.position;
				return;
			}

			if (Vector2.DistanceSquared(moveStuckAnchor, NPC.position) < 4f)
				moveStuckTicks++;
			else {
				moveStuckTicks = Math.Max(0, moveStuckTicks - 4);
				moveStuckAnchor = NPC.position;
			}

			if (moveStuckTicks >= MoveStuckThreshold && moveUnstuckCooldown <= 0)
				Move_ForceUnstuck(target);
		}

		private bool Move_BlockSkillPick() =>
			moveStuckTicks >= 20 || moveWallBumpTicks >= 14 || Move_IsWallEmbedded(NPC.Center);

		private void Move_ForceUnstuck(Player target) {
			if (moveUnstuckCooldown > 0)
				return;

			Vector2 escape = Move_FindStandableNearPlayer(target);
			if (Move_NeedsLanding(escape))
				escape = Move_SnapToGround(Move_FindOpen(NPC.Center, requireGround: false));

			SetPhysics(true, false);
			NPC.Center = escape;
			NPC.velocity = Vector2.Zero;
			SetPhysics(true, true);
			NPC.netUpdate = true;
			Move_ResetWatch();
			moveUnstuckCooldown = MoveUnstuckCooldown;
			CurrentAIState = AIState.Recover;
			StateTimer = 18;
		}

		private void Move_EndMelee(Player target) {
			Move_ClearSwordSlashes();
			NPC.velocity = Vector2.Zero;
			SetPhysics(true, true);

			if (Move_NeedsLanding(NPC.Center)) {
				Vector2 land = Move_SnapToGround(Move_FindOpen(NPC.Center, requireGround: true));
				if (Move_NeedsLanding(land))
					land = Move_FindStandableNearPlayer(target);
				NPC.Center = land;
				NPC.netUpdate = true;
			}

			CurrentAnimation = NPCState.Walk;
			ResetToIdle();
		}

		private void Move_ClearSwordSlashes() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int slashType = ModContent.ProjectileType<SwordSlashEffect>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.type == slashType && (int)p.ai[2] == NPC.whoAmI)
					p.Kill();
			}
		}

		private void Move_SoftenLandingOnIdle() {
			if (!Move_NeedsLanding(NPC.Center))
				return;

			Vector2 land = Move_SnapToGround(Move_FindOpen(NPC.Center, requireGround: true));
			if (!Move_IsWallEmbedded(land)) {
				NPC.Center = land;
				NPC.velocity = Vector2.Zero;
				NPC.netUpdate = true;
			}
		}
	}
}
