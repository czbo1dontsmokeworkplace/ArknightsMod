using System;
using System.Collections.Generic;
using ArknightsMod.Content.Tiles;
using ArknightsMod.Content.Tiles.Natural;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Systems
{
	// 地表小型植物的“自然生长”注册表：被动生长（血蕈随饱食buff蔓延、霜晶树的低概率生长）
	// 和 /plant 调试指令共用同一套判定逻辑。以后再加新的地表植物，只需要在 PostSetupContent 里补一条 Entry。
	public class NaturalGrowthSystem : ModSystem
	{
		// extraCondition 用于放置位置的额外校验（比如“必须在城镇范围内”），参数是放置坐标 (x, y)；
		// forced 模式（目前只有血月刷霜晶树用）会跳过 ValidGroundTiles 和 extraCondition，只要求下面有地面。
		public readonly struct Entry(int tileType, int width, int height, int[] validGroundTiles, Func<int, int, bool> extraCondition = null)
		{
			public readonly int TileType = tileType;
			public readonly int Width = width;
			public readonly int Height = height;
			public readonly int[] ValidGroundTiles = validGroundTiles;
			public readonly Func<int, int, bool> ExtraCondition = extraCondition;
		}

		private static readonly List<Entry> entries = [];
		private static bool wasBloodMoon;

		public override void PostSetupContent() {
			entries.Clear();
			entries.Add(new Entry(ModContent.TileType<BloodMushroom>(), 2, 2, [
				TileID.Grass, TileID.BlueMoss, TileID.GreenMoss, TileID.PurpleMoss, TileID.RedMoss, TileID.BrownMoss
			]));
			entries.Add(new Entry(ModContent.TileType<FrostCrystalTree>(), 3, 4, [TileID.IceBlock, TileID.SnowBlock]));
			entries.Add(new Entry(ModContent.TileType<EchoCorn>(), 2, 4, [TileID.Grass], IsNearTownNPC));
		}

		// “城镇环境”：附近有已安家的镇民。
		private static bool IsNearTownNPC(int x, int y) {
			Vector2 pos = new Vector2(x, y) * 16f;
			const float radius = 50 * 16f;

			for (int k = 0; k < Main.maxNPCs; k++) {
				NPC npc = Main.npc[k];
				if (npc.active && npc.townNPC && Vector2.Distance(npc.Center, pos) < radius)
					return true;
			}
			return false;
		}

		// 在玩家周围随机试几个坐标，成功种下一株就返回 true（供被动生长使用，配合外层的低概率判定）。
		// specificTileType 传 -1 表示从全部注册条目里随机挑一个尝试，否则只尝试指定的那一种。
		public static bool TryGrowRandomNear(Player player, int radiusTiles, int specificTileType = -1) {
			if (Main.netMode == NetmodeID.MultiplayerClient || entries.Count == 0) return false;

			List<Entry> candidates = specificTileType < 0 ? entries : entries.FindAll(e => e.TileType == specificTileType);
			if (candidates.Count == 0) return false;

			Entry entry = candidates[Main.rand.Next(candidates.Count)];
			int px = (int)(player.Center.X / 16f);
			int py = (int)(player.Center.Y / 16f);

			for (int attempt = 0; attempt < 20; attempt++) {
				int x = px + Main.rand.Next(-radiusTiles, radiusTiles + 1);
				int y = py + Main.rand.Next(-radiusTiles, radiusTiles + 1);
				if (TryPlaceAt(entry, x, y, forced: false)) return true;
			}
			return false;
		}

		// 遍历玩家周围整个矩形区域，把所有满足条件的位置一次性种满（供 /plant 使用）。返回种下的数量。
		public static int GrowAllNear(Player player, int radiusTiles) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return 0;

			int px = (int)(player.Center.X / 16f);
			int py = (int)(player.Center.Y / 16f);
			int planted = 0;

			for (int x = px - radiusTiles; x <= px + radiusTiles; x++) {
				for (int y = py - radiusTiles; y <= py + radiusTiles; y++) {
					foreach (Entry entry in entries) {
						if (TryPlaceAt(entry, x, y, forced: false)) {
							planted++;
							break;
						}
					}
				}
			}
			return planted;
		}

		// 无视地面种类强制种下，目前只给血月的霜晶树保底生成用。
		public static bool TryForceGrowNear(Player player, int tileType, int radiusTiles) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return false;

			Entry match = default;
			bool found = false;
			foreach (Entry e in entries) {
				if (e.TileType == tileType) { match = e; found = true; break; }
			}
			if (!found) return false;

			int px = (int)(player.Center.X / 16f);
			int py = (int)(player.Center.Y / 16f);

			for (int attempt = 0; attempt < 30; attempt++) {
				int x = px + Main.rand.Next(-radiusTiles, radiusTiles + 1);
				int y = py + Main.rand.Next(-radiusTiles, radiusTiles + 1);
				if (TryPlaceAt(match, x, y, forced: true)) return true;
			}
			return false;
		}

		public override void PostUpdateWorld() {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;

			bool bloodMoonNow = Main.bloodMoon;
			if (bloodMoonNow && !wasBloodMoon) {
				int frostType = ModContent.TileType<FrostCrystalTree>();
				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];
					if (player.active)
						TryForceGrowNear(player, frostType, 20);
				}
			}
			wasBloodMoon = bloodMoonNow;
		}

		private static bool TryPlaceAt(Entry entry, int x, int y, bool forced) {
			if (!WorldGen.InWorld(x, y, 1) || Main.tile[x, y].HasTile) return false;

			if (forced) {
				Tile ground = Main.tile[x, y + entry.Height];
				if (!ground.HasTile) return false; // 无视具体种类，但至少要有地面
			}
			else if (entry.ExtraCondition != null && !entry.ExtraCondition(x, y)) {
				return false;
			}

			bool placed = WorldGen.PlaceTile(x, y, entry.TileType, mute: true, forced: forced);
			if (placed)
				NetMessage.SendTileSquare(-1, x, y, entry.Width + 1, entry.Height + 1);

			return placed;
		}
	}
}
