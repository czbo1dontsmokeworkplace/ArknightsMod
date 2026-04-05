using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Systems.InstancedSpace
{
	public sealed class InstancedRoomSystem : ModSystem
	{
		private const bool AutoBuildRooms = true;
		private const int TestRoomWidth = 120;
		private const int TestRoomHeight = 80;
		private const int ProtocolArenaWidth = 140;
		private const int ProtocolArenaHeight = 70;
		private const int UnloadDelayTicks = 60 * 5;
		private const int AreaSnapshotMagic = unchecked((int)0x52444E45);
		private const byte AreaSnapshotVersion = 3;
		private const string BaseRoomTemplatePath = "Assets/Schematics/BaseRoom.nbt";
		private const string RoomRecoveryFolderName = "RoomRecovery";

		public sealed class Room
		{
			public int Id;
			public Rectangle Area;
			public HashSet<int> Players = new();
		}

		public static readonly Dictionary<int, Room> Rooms = new();
		public static readonly Dictionary<int, int> PlayerRoomMap = new();
		private static readonly HashSet<int> BuiltRooms = new();
		private static readonly Dictionary<int, byte[]> OriginalSnapshots = new();
		private static readonly Dictionary<int, byte[]> RoomSnapshots = new();
		private static readonly Dictionary<int, IList<TagCompound>> RoomChestSnapshots = new();
		private static readonly Dictionary<int, int> RoomUnloadCountdown = new();

		public static bool SuppressItemDrops { get; private set; }
		public static Rectangle? CurrentMutationArea { get; private set; }

		public static bool IsRoomBuilt(int roomId) => BuiltRooms.Contains(roomId);

		private static bool _routingBroadcast;
		private static bool _loadedInWorld;

		public override void OnWorldLoad()
		{
			// Do NOT clear persistent data here. LoadWorldData is responsible for loading Rooms/BuiltRooms/snapshots.
			// OnWorldLoad can be called after LoadWorldData, so clearing here would wipe saved state.
			PlayerRoomMap.Clear();
			RoomUnloadCountdown.Clear();
			if (Rooms.Count == 0)
				InitializeRooms();

			if (Main.netMode != NetmodeID.MultiplayerClient)
				TryRecoverRoomsFromCrash();
		}

		public override void OnWorldUnload()
		{
			Rooms.Clear();
			PlayerRoomMap.Clear();
			BuiltRooms.Clear();
			OriginalSnapshots.Clear();
			RoomSnapshots.Clear();
			RoomChestSnapshots.Clear();
			RoomUnloadCountdown.Clear();
			_loadedInWorld = false;
		}

		public override void PreSaveAndQuit()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Ensure the overworld is restored before the world file is saved.
			// If a player quits while inside a built room, the temporary room tiles could crash TileIO during save.
			foreach (var kv in Rooms)
			{
				int roomId = kv.Key;
				ForceRestoreRoomForSave(roomId);
			}
		}

		private static void GenerateProtocolArena(Rectangle area)
		{
			ushort solid = TileID.GrayBrick;
			ushort platform = TileID.Platforms;
			ushort wall = WallID.GrayBrick;
			for (int x = area.Left; x < area.Right; x++)
			{
				for (int y = area.Top; y < area.Bottom; y++)
				{
					Tile t = Main.tile[x, y];
					t.ClearEverything();
					t.WallType = wall;
					t.WallColor = 0;
				}
			}

			int left = area.Left + 1;
			int right = area.Right - 2;
			int top = area.Top + 1;
			int bottom = area.Bottom - 2;

			for (int x = left; x <= right; x++)
			{
				PlaceSolid(x, top, solid);
				PlaceSolid(x, bottom, solid);
			}
			for (int y = top; y <= bottom; y++)
			{
				PlaceSolid(left, y, solid);
				PlaceSolid(right, y, solid);
			}

			for (int x = left + 2; x <= right - 2; x++)
				PlaceSolid(x, bottom - 1, solid);
			for (int x = left + 10; x <= right - 10; x++)
				PlacePlatform(x, bottom - 6, platform);

			WorldGen.RangeFrame(area.Left, area.Top, area.Right - 1, area.Bottom - 1);
		}

		private static void PlaceSolid(int x, int y, ushort type)
		{
			if (!WorldGen.InWorld(x, y, 10))
				return;
			Tile t = Main.tile[x, y];
			t.HasTile = true;
			t.TileType = type;
			t.Slope = 0;
			t.IsHalfBlock = false;
			t.TileFrameX = 0;
			t.TileFrameY = 0;
		}

		private static void PlacePlatform(int x, int y, ushort type)
		{
			if (!WorldGen.InWorld(x, y, 10))
				return;
			Tile t = Main.tile[x, y];
			t.HasTile = true;
			t.TileType = type;
			t.Slope = 0;
			t.IsHalfBlock = false;
			t.TileFrameX = 0;
			t.TileFrameY = 0;
		}

		private static void InitializeRooms()
		{
			if (Rooms.Count > 0)
				return;
			int w1 = TestRoomWidth;
			int h1 = TestRoomHeight;
			if (TryLoadBaseRoomTemplate(out _, out _, out int tw, out int th))
			{
				if (tw > 0 && th > 0)
				{
					w1 = tw;
					h1 = th;
				}
			}
			Rectangle area1 = FindRoomArea(w1, h1);
			Rooms[1] = new Room { Id = 1, Area = area1 };

			int w2 = ProtocolArenaWidth;
			int h2 = ProtocolArenaHeight;
			Rectangle area2 = FindRoomArea(w2, h2);
			int safety = 0;
			while (area2.Intersects(area1) && safety++ < 50)
				area2 = FindRoomArea(w2, h2);
			Rooms[2] = new Room { Id = 2, Area = area2 };
		}

		private static void EnsureRequiredRooms()
		{
			if (!Rooms.ContainsKey(1))
			{
				int w1 = TestRoomWidth;
				int h1 = TestRoomHeight;
				if (TryLoadBaseRoomTemplate(out _, out _, out int tw, out int th))
				{
					if (tw > 0 && th > 0)
					{
						w1 = tw;
						h1 = th;
					}
				}
				Rectangle area1 = FindRoomArea(w1, h1);
				Rooms[1] = new Room { Id = 1, Area = area1 };
			}

			if (!Rooms.ContainsKey(2))
			{
				Rectangle area1 = Rooms.TryGetValue(1, out Room r1) ? r1.Area : Rectangle.Empty;
				int w2 = ProtocolArenaWidth;
				int h2 = ProtocolArenaHeight;
				Rectangle area2 = FindRoomArea(w2, h2);
				int safety = 0;
				while (area1 != Rectangle.Empty && area2.Intersects(area1) && safety++ < 80)
					area2 = FindRoomArea(w2, h2);
				Rooms[2] = new Room { Id = 2, Area = area2 };
			}
		}

		private static Rectangle FindRoomArea(int w, int h)
		{
			// First try: in the sky near the far west/east edges.
			// If no valid candidate is found, fall back to the dungeon-opposite edge search.
			int skyTopY = 20;
			int skyBottomY = Math.Max(skyTopY, 120);
			int yMinSky = Math.Clamp(skyTopY, 10, Math.Max(10, Main.maxTilesY - h - 10));
			int yMaxSky = Math.Clamp(skyBottomY, yMinSky, Math.Max(yMinSky, Main.maxTilesY - h - 10));
			int leftBandMin = 120;
			int leftBandMax = 520;
			int rightBandMin = Math.Max(leftBandMax + 100, Main.maxTilesX - 520 - w);
			int rightBandMax = Math.Max(rightBandMin, Main.maxTilesX - 120 - w);
			leftBandMax = Math.Min(leftBandMax, Math.Max(leftBandMin, Main.maxTilesX - w - 120));

			for (int attempt = 0; attempt < 160; attempt++)
			{
				bool useRight = Main.rand.NextBool();
				int skyXMin = useRight ? rightBandMin : leftBandMin;
				int skyXMax = useRight ? rightBandMax : leftBandMax;
				if (skyXMin > skyXMax)
					continue;
				int x = Main.rand.Next(skyXMin, skyXMax + 1);
				int y = Main.rand.Next(yMinSky, yMaxSky + 1);
				Rectangle area = new Rectangle(x, y, w, h);
				if (!WorldGen.InWorld(area.Left, area.Top, 10) || !WorldGen.InWorld(area.Right - 1, area.Bottom - 1, 10))
					continue;
				if (AreaHasChestsOrTE(area))
					continue;
				return area;
			}

			// Candidate band: near a far edge, mid -> deep underground. We snapshot+restore, so it doesn't have to be empty.
			// Prefer the opposite side of the dungeon to avoid interfering with vanilla progression.
			int edgePadding = 420;
			int dungeonX = Main.dungeonX;
			bool preferRight = dungeonX < Main.maxTilesX / 2;
			int xMin;
			int xMax;
			if (preferRight)
			{
				xMin = Math.Max(200, Main.maxTilesX - edgePadding - w - 900);
				xMax = Math.Max(200, Main.maxTilesX - edgePadding - w);
			}
			else
			{
				xMin = 200;
				xMax = Math.Max(200, 200 + 900);
			}
			int yMin = Math.Max(200, Main.maxTilesY / 2);
			int yMax = Math.Max(200, Main.maxTilesY - 600 - h);
			int dungeonAvoidRadius = 900;

			// Safety for small worlds or odd sizes where ranges might invert.
			if (xMin > xMax || yMin > yMax)
			{
				int safeFallbackX = Math.Clamp(Main.maxTilesX - edgePadding - w, 200, Math.Max(200, Main.maxTilesX - w - 200));
				int safeFallbackY = Math.Clamp(Main.maxTilesY - 600 - h, 200, Math.Max(200, Main.maxTilesY - h - 200));
				return new Rectangle(safeFallbackX, safeFallbackY, w, h);
			}

			for (int attempt = 0; attempt < 200; attempt++)
			{
				int x = Main.rand.Next(xMin, xMax + 1);
				int y = Main.rand.Next(yMin, yMax + 1);
				Rectangle area = new Rectangle(x, y, w, h);
				int areaCenterX = area.X + area.Width / 2;
				if (Math.Abs(areaCenterX - dungeonX) < dungeonAvoidRadius)
					continue;
				if (!WorldGen.InWorld(area.Left, area.Top, 10) || !WorldGen.InWorld(area.Right - 1, area.Bottom - 1, 10))
					continue;
				if (AreaHasChestsOrTE(area))
					continue;
				return area;
			}

			int fallbackX = preferRight ? Math.Max(200, Main.maxTilesX - edgePadding - w) : 200;
			int fallbackY = Math.Max(200, Main.maxTilesY - 600 - h);
			return new Rectangle(fallbackX, fallbackY, w, h);
		}

		private static bool AreaHasChestsOrTE(Rectangle area)
		{
			for (int c = 0; c < Main.maxChests; c++)
			{
				Chest chest = Main.chest[c];
				if (chest == null)
					continue;
				if (area.Contains(chest.x, chest.y))
					return true;
			}

			foreach (var kv in TileEntity.ByID)
			{
				TileEntity te = kv.Value;
				if (te == null)
					continue;
				if (area.Contains(te.Position.X, te.Position.Y))
					return true;
			}

			return false;
		}

		public static int GetRoomIdForPlayer(Player player)
		{
			if (player == null)
				return 0;
			return PlayerRoomMap.TryGetValue(player.whoAmI, out int id) ? id : 0;
		}

		public static int GetRoomIdAtTile(int x, int y)
		{
			foreach (var kv in Rooms)
			{
				if (kv.Value.Area.Contains(x, y))
					return kv.Key;
			}
			return 0;
		}

		public static int GetRoomIdAtWorldPosition(Vector2 worldPosition)
		{
			int x = (int)(worldPosition.X / 16f);
			int y = (int)(worldPosition.Y / 16f);
			return GetRoomIdAtTile(x, y);
		}

		public static byte[] CaptureAreaSnapshot(Rectangle area) => CaptureArea(area);

		public static IList<TagCompound> CaptureChestSnapshot(Rectangle area) => CaptureChests(area);

		public static bool IsTileInPlayerRoom(int x, int y, Player player)
		{
			int roomId = GetRoomIdForPlayer(player);
			if (roomId == 0)
				return false;
			return Rooms.TryGetValue(roomId, out Room room) && room.Area.Contains(x, y);
		}

		public static Vector2 GetRoomSpawnWorld(int roomId)
		{
			if (!Rooms.TryGetValue(roomId, out Room room))
				return new Vector2(Main.spawnTileX * 16, Main.spawnTileY * 16);

			int x = room.Area.X + room.Area.Width / 2;
			int y = room.Area.Y + 6;
			return new Vector2(x * 16 + 8, y * 16 + 8);
		}

		public static bool TryEnterRoom(Player player, int roomId)
		{
			if (player == null)
				return false;
			if (!Rooms.ContainsKey(roomId))
				return false;

			if (Main.netMode != NetmodeID.MultiplayerClient)
				LoadRoomIntoWorldIfNeeded(roomId);
			if (AutoBuildRooms)
				EnsureRoomBuilt(roomId);
			if (!BuiltRooms.Contains(roomId))
				return false;

			AssignPlayer(player, roomId);
			TeleportAndSync(player, GetRoomSpawnWorld(roomId));
			if (Main.netMode == NetmodeID.Server)
				SyncRoomAreaToPlayer(roomId, player.whoAmI);
			return true;
		}

		private static void LoadRoomIntoWorldIfNeeded(int roomId)
		{
			if (!Rooms.TryGetValue(roomId, out Room room))
				return;

			// Capture overworld state only when the room is NOT currently built.
			// If we re-capture while the room is built, we'd accidentally snapshot the room tiles as "overworld",
			// causing Exit/Unload to restore to the room instead of the original terrain.
			if (!BuiltRooms.Contains(roomId) && !OriginalSnapshots.ContainsKey(roomId))
			{
				byte[] original = CaptureArea(room.Area);
				OriginalSnapshots[roomId] = original;
				TryWriteRecoveryFile(roomId, room.Area, original);
			}

			if (!RoomSnapshots.ContainsKey(roomId))
			{
				if (roomId == 2)
				{
					BeginAreaMutation(room.Area);
					try
					{
						GenerateProtocolArena(room.Area);
						RoomSnapshots[roomId] = CaptureArea(room.Area);
						RoomChestSnapshots[roomId] = new List<TagCompound>();
					}
					finally
					{
						EndAreaMutation(room.Area);
					}
				}
				else if (TryLoadBaseRoomTemplate(out byte[] baseSnap, out IList<TagCompound> baseChests, out _, out _)
					&& baseSnap != null && baseSnap.Length > 0)
				{
					RoomSnapshots[roomId] = baseSnap;
					if (baseChests != null)
						RoomChestSnapshots[roomId] = baseChests;
				}
			}
			if (!RoomSnapshots.TryGetValue(roomId, out byte[] snapCheck) || snapCheck == null || snapCheck.Length == 0)
				return;

			BeginAreaMutation(room.Area);
			try
			{
				ApplyArea(room.Area, snapCheck);
				RestoreChests(roomId, room.Area);
				BuiltRooms.Add(roomId);
			}
			finally
			{
				EndAreaMutation(room.Area);
			}
		}

		private static bool TryLoadBaseRoomTemplate(out byte[] snap, out IList<TagCompound> chests, out int w, out int h)
			=> TryLoadBaseRoomTemplate(out snap, out chests, out w, out h, out _);

		private static bool TryLoadBaseRoomTemplate(out byte[] snap, out IList<TagCompound> chests, out int w, out int h, out int bytesLen)
		{
			snap = null;
			chests = null;
			w = 0;
			h = 0;
			bytesLen = 0;
			try
			{
				byte[] bytes = ModContent.GetFileBytes("ArknightsMod/" + BaseRoomTemplatePath);
				if (bytes == null || bytes.Length == 0)
					return false;
				bytesLen = bytes.Length;
				using var ms = new MemoryStream(bytes);
				TagCompound tag = TagIO.FromStream(ms, true);
				if (tag == null)
					return false;
				if (tag.ContainsKey("w"))
					w = tag.GetInt("w");
				if (tag.ContainsKey("h"))
					h = tag.GetInt("h");
				if (tag.ContainsKey("data"))
					snap = tag.GetByteArray("data");
				if (tag.ContainsKey("chests"))
					chests = tag.GetList<TagCompound>("chests");
				return snap != null && snap.Length > 0;
			}
			catch
			{
				return false;
			}
		}

		public static void ResetRooms()
		{
			Rooms.Clear();
			BuiltRooms.Clear();
			PlayerRoomMap.Clear();
			OriginalSnapshots.Clear();
			RoomSnapshots.Clear();
			RoomChestSnapshots.Clear();
			RoomUnloadCountdown.Clear();
			InitializeRooms();
		}

		public static bool ResetRoomToBaseTemplate(int roomId, out string details)
		{
			details = string.Empty;
			if (!Rooms.TryGetValue(roomId, out Room room))
			{
				details = "unknown roomId";
				return false;
			}
			if (!TryLoadBaseRoomTemplate(out byte[] baseSnap, out IList<TagCompound> baseChests, out int w, out int h, out int bytesLen))
			{
				details = $"template load failed (bytes={bytesLen})";
				return false;
			}
			RoomSnapshots[roomId] = baseSnap;
			RoomChestSnapshots[roomId] = baseChests ?? new List<TagCompound>();
			BuiltRooms.Remove(roomId);
			details = $"template ok (bytes={bytesLen}, w={w}, h={h}, chests={RoomChestSnapshots[roomId].Count})";
			if (Main.netMode != NetmodeID.MultiplayerClient)
				EnsureRoomBuilt(roomId);
			return true;
		}

		public static bool RebuildRoom(int roomId)
		{
			if (!Rooms.ContainsKey(roomId))
				return false;
			BuiltRooms.Remove(roomId);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				LoadRoomIntoWorldIfNeeded(roomId);
			return true;
		}

		public static Rectangle? GetRoomArea(int roomId)
		{
			return Rooms.TryGetValue(roomId, out Room room) ? room.Area : null;
		}

		private static void EnsureRoomBuilt(int roomId)
		{
			if (BuiltRooms.Contains(roomId))
				return;
			if (!Rooms.TryGetValue(roomId, out Room room))
				return;
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// No more gray-brick "test room" generation. Rooms can only be built from:
			// - Saved snapshot in RoomSnapshots
			// - Embedded base template which will initialize RoomSnapshots when missing
			LoadRoomIntoWorldIfNeeded(roomId);
		}

		private static void SyncRoomAreaToPlayer(int roomId, int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			if (!Rooms.TryGetValue(roomId, out Room room))
				return;
			if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;

			int chunkW = 50;
			int chunkH = 50;
			for (int x = room.Area.X; x < room.Area.Right; x += chunkW)
			{
				for (int y = room.Area.Y; y < room.Area.Bottom; y += chunkH)
				{
					int w = Math.Min(chunkW, room.Area.Right - x);
					int h = Math.Min(chunkH, room.Area.Bottom - y);
					NetMessage.SendTileSquare(whoAmI, x, y, w, h);
				}
			}
		}

		public static void ExitRoom(Player player) => ExitRoom(player, false);

		public static void ExitRoom(Player player, bool force)
		{
			if (player == null)
				return;

			if (!force && ProtocolSpaceEventSystem.IsExitLocked(player))
			{
				if (Main.netMode != NetmodeID.Server)
					Main.NewText(Language.GetTextValue("Mods.ArknightsMod.ProtocolSpace.ExitLocked"), Color.MediumPurple);
				return;
			}

			int roomId = GetRoomIdForPlayer(player);
			if (roomId != 0 && Rooms.TryGetValue(roomId, out Room room))
				room.Players.Remove(player.whoAmI);

			PlayerRoomMap.Remove(player.whoAmI);

			TeleportAndSync(player, new Vector2(Main.spawnTileX * 16, Main.spawnTileY * 16));

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				RoomUnloadCountdown.Remove(roomId);
				if (roomId != 0 && Rooms.TryGetValue(roomId, out Room r) && r.Players.Count == 0)
					UnloadRoom(roomId);
				else
					ArmUnloadCountdown(roomId);
			}
		}

		private static void ArmUnloadCountdown(int roomId)
		{
			if (roomId == 0)
				return;
			if (!Rooms.TryGetValue(roomId, out Room room))
				return;
			if (room.Players.Count > 0)
				return;
			RoomUnloadCountdown[roomId] = UnloadDelayTicks;
		}

		private static void UnloadRoom(int roomId)
		{
			if (!Rooms.TryGetValue(roomId, out Room room))
				return;
			if (room.Players.Count > 0)
				return;
			if (!OriginalSnapshots.TryGetValue(roomId, out byte[] original) || original == null || original.Length == 0)
				return;

			RoomSnapshots[roomId] = CaptureArea(room.Area);
			RoomChestSnapshots[roomId] = CaptureChests(room.Area);

			BeginAreaMutation(room.Area);
			try
			{
				ApplyArea(room.Area, original);
				ClearChestsInArea(room.Area);
				ClearTileEntitiesInArea(room.Area);
				BuiltRooms.Remove(roomId);
				OriginalSnapshots.Remove(roomId);
				TryDeleteRecoveryFile(roomId);
				WorldGen.RangeFrame(room.Area.X, room.Area.Y, room.Area.Right - 1, room.Area.Bottom - 1);
			}
			finally
			{
				EndAreaMutation(room.Area);
			}

			BroadcastAreaTilesToAllClients(room.Area);
		}

		private static void ForceRestoreRoomForSave(int roomId)
		{
			if (!Rooms.TryGetValue(roomId, out Room room))
				return;
			if (!OriginalSnapshots.TryGetValue(roomId, out byte[] original) || original == null || original.Length == 0)
				return;
			if (!BuiltRooms.Contains(roomId))
				return;

			// Save the current room edits into persistent snapshots, then restore overworld tiles.
			RoomSnapshots[roomId] = CaptureArea(room.Area);
			RoomChestSnapshots[roomId] = CaptureChests(room.Area);

			BeginAreaMutation(room.Area);
			try
			{
				ApplyArea(room.Area, original);
				ClearChestsInArea(room.Area);
				ClearTileEntitiesInArea(room.Area);
				BuiltRooms.Remove(roomId);
				RoomUnloadCountdown.Remove(roomId);
				room.Players.Clear();
				OriginalSnapshots.Remove(roomId);
				TryDeleteRecoveryFile(roomId);
				WorldGen.RangeFrame(room.Area.X, room.Area.Y, room.Area.Right - 1, room.Area.Bottom - 1);
			}
			finally
			{
				EndAreaMutation(room.Area);
			}

			BroadcastAreaTilesToAllClients(room.Area);

			// Also clear any player->room mappings so Save/Load doesn't preserve a stale in-room state.
			if (PlayerRoomMap.Count > 0)
			{
				var keys = new List<int>(PlayerRoomMap.Keys);
				foreach (int who in keys)
				{
					if (PlayerRoomMap.TryGetValue(who, out int rid) && rid == roomId)
						PlayerRoomMap.Remove(who);
				}
			}
		}

		private static void BeginAreaMutation(Rectangle area)
		{
			SuppressItemDrops = true;
			CurrentMutationArea = area;
			ClearItemsInArea(area);
		}

		private static string GetWorldRecoveryPrefix()
		{
			try
			{
				string unique = Main.ActiveWorldFileData?.UniqueId.ToString() ?? "unknown";
				return unique;
			}
			catch
			{
				return "unknown";
			}
		}

		private static string GetRecoveryDir()
		{
			return Path.Combine(Main.SavePath, "ModSaves", ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().Name, RoomRecoveryFolderName);
		}

		private static string GetRecoveryFilePath(int roomId)
		{
			string prefix = GetWorldRecoveryPrefix();
			return Path.Combine(GetRecoveryDir(), $"{prefix}_room{roomId}.dat");
		}

		private static void TryWriteRecoveryFile(int roomId, Rectangle area, byte[] originalSnapshot)
		{
			try
			{
				string dir = GetRecoveryDir();
				Directory.CreateDirectory(dir);
				string path = GetRecoveryFilePath(roomId);
				var tag = new TagCompound
				{
					["roomId"] = roomId,
					["x"] = area.X,
					["y"] = area.Y,
					["w"] = area.Width,
					["h"] = area.Height,
					["data"] = originalSnapshot,
				};
				using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
				TagIO.ToStream(tag, fs, true);
			}
			catch
			{
				// Best-effort recovery. If this fails, the system still works, just can't auto-clean after a crash.
			}
		}

		private static void TryDeleteRecoveryFile(int roomId)
		{
			try
			{
				string path = GetRecoveryFilePath(roomId);
				if (File.Exists(path))
					File.Delete(path);
			}
			catch
			{
			}
		}

		private static void TryRecoverRoomsFromCrash()
		{
			try
			{
				string dir = GetRecoveryDir();
				if (!Directory.Exists(dir))
					return;
				string prefix = GetWorldRecoveryPrefix() + "_room";
				foreach (string path in Directory.EnumerateFiles(dir, prefix + "*.dat", SearchOption.TopDirectoryOnly))
				{
					TagCompound tag;
					using (var fs = File.OpenRead(path))	
						tag = TagIO.FromStream(fs, true);
					if (tag == null)
						continue;
					int roomId = tag.GetInt("roomId");
					int x = tag.GetInt("x");
					int y = tag.GetInt("y");
					int w = tag.GetInt("w");
					int h = tag.GetInt("h");
					if (w <= 0 || h <= 0)
						continue;
					var area = new Rectangle(x, y, w, h);
					byte[] data = tag.GetByteArray("data");
					if (data == null || data.Length == 0)
						continue;

					// Ensure the room area is stable after crash recovery.
					// If the room list failed to save due to the crash, re-create it using the recovered area.
					Rooms[roomId] = new Room { Id = roomId, Area = area };
					BuiltRooms.Remove(roomId);
					OriginalSnapshots.Remove(roomId);
					RoomUnloadCountdown.Remove(roomId);
					Rooms[roomId].Players.Clear();
					if (PlayerRoomMap.Count > 0)
					{
						var keys = new List<int>(PlayerRoomMap.Keys);
						foreach (int who in keys)
						{
							if (PlayerRoomMap.TryGetValue(who, out int rid) && rid == roomId)
								PlayerRoomMap.Remove(who);
						}
					}

					BeginAreaMutation(area);
					try
					{
						ApplyArea(area, data);
						ClearChestsInArea(area);
						ClearTileEntitiesInArea(area);
						WorldGen.RangeFrame(area.X, area.Y, area.Right - 1, area.Bottom - 1);
					}
					finally
					{
						EndAreaMutation(area);
					}

					BroadcastAreaTilesToAllClients(area);

					try { File.Delete(path); } catch { }
				}
			}
			catch
			{
			}
		}

		private static void EndAreaMutation(Rectangle area)
		{
			ClearItemsInArea(area);
			CurrentMutationArea = null;
			SuppressItemDrops = false;
		}

		private static void ClearItemsInArea(Rectangle area)
		{
			for (int i = 0; i < Main.maxItems; i++)
			{
				Item it = Main.item[i];
				if (it == null || !it.active)
					continue;
				int x = (int)(it.position.X / 16f);
				int y = (int)(it.position.Y / 16f);
				if (!area.Contains(x, y))
					continue;
				it.TurnToAir();
				it.active = false;
			}
		}

		private static byte[] CaptureArea(Rectangle area)
		{
			using var ms = new MemoryStream();
			using (var ds = new DeflateStream(ms, CompressionLevel.Fastest, true))
			using (var bw = new BinaryWriter(ds))
			{
				bw.Write(area.Width);
				bw.Write(area.Height);
				bw.Write(AreaSnapshotMagic);
				bw.Write(AreaSnapshotVersion);
				for (int x = area.Left; x < area.Right; x++)
				{
					for (int y = area.Top; y < area.Bottom; y++)
					{
						Tile t = Main.tile[x, y];
						bw.Write(t.HasTile);
						if (t.HasTile)
						{
							bw.Write(t.TileType);
							bw.Write(t.TileFrameX);
							bw.Write(t.TileFrameY);
							bw.Write((byte)t.Slope);
							bw.Write(t.IsHalfBlock);
							bw.Write(t.IsActuated);
							bw.Write(t.HasActuator);
							bw.Write(t.TileColor);
						}
						bw.Write(t.WallType);
						bw.Write(t.WallColor);
						bw.Write(t.RedWire);
						bw.Write(t.BlueWire);
						bw.Write(t.GreenWire);
						bw.Write(t.YellowWire);
						bw.Write(t.LiquidAmount);
						bw.Write((byte)t.LiquidType);
					}
				}
			}
			return ms.ToArray();
		}

		private static void ApplyArea(Rectangle area, byte[] data)
		{
			// IMPORTANT: Do not use the compressed stream position/length to decide if more bytes exist.
			// That can corrupt reads and write invalid tiles, potentially deadlocking the lighting engine.
			byte[] raw;
			using (var ms = new MemoryStream(data))
			using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
			using (var outMs = new MemoryStream())
			{
				ds.CopyTo(outMs);
				raw = outMs.ToArray();
			}

			using var rawMs = new MemoryStream(raw);
			using var br = new BinaryReader(rawMs);
			bool CanRead(int count) => (rawMs.Length - rawMs.Position) >= count;
			if (!CanRead(8))
				return;
			int w = br.ReadInt32();
			int h = br.ReadInt32();
			if (w != area.Width || h != area.Height)
				return;

			byte version = 1;
			if (CanRead(5))
			{
				long pos = rawMs.Position;
				int magic = br.ReadInt32();
				if (magic == AreaSnapshotMagic)
					version = br.ReadByte();
				else
					rawMs.Position = pos;
			}

			// Validate bytes before mutating tiles to avoid partially-corrupted world state.
			long payloadPos = rawMs.Position;
			for (int x = area.Left; x < area.Right; x++)
			{
				for (int y = area.Top; y < area.Bottom; y++)
				{
					if (!CanRead(1))
						return;
					bool hasTile = br.ReadBoolean();
					if (hasTile)
					{
						int tileBytes = version >= 2 ? 11 : 6;
						if (!CanRead(tileBytes))
							return;
						br.BaseStream.Position += tileBytes;
					}
					int restBytes = version >= 3 ? 9 : (version >= 2 ? 15 : 4);
					if (!CanRead(restBytes))
						return;
					br.BaseStream.Position += restBytes;
				}
			}
			rawMs.Position = payloadPos;

			for (int x = area.Left; x < area.Right; x++)
			{
				for (int y = area.Top; y < area.Bottom; y++)
				{
					if (!CanRead(1))
						return;
					bool hasTile = br.ReadBoolean();
					Tile t = Main.tile[x, y];
					t.ClearEverything();
					t.HasTile = hasTile;
					if (hasTile)
					{
						if (version >= 2)
						{
							if (!CanRead(11))
								return;
							ushort tileType = br.ReadUInt16();
							t.TileFrameX = br.ReadInt16();
							t.TileFrameY = br.ReadInt16();
							t.Slope = (SlopeType)br.ReadByte();
							t.IsHalfBlock = br.ReadBoolean();
							t.IsActuated = br.ReadBoolean();
							t.HasActuator = br.ReadBoolean();
							t.TileColor = br.ReadByte();
							if (tileType >= TileLoader.TileCount)
								tileType = 0;
							t.TileType = tileType;
						}
						else
						{
							if (!CanRead(6))
								return;
							ushort tileType = br.ReadUInt16();
							t.TileFrameX = br.ReadInt16();
							t.TileFrameY = br.ReadInt16();
							if (tileType >= TileLoader.TileCount)
								tileType = 0;
							t.TileType = tileType;
						}
					}
					if (version >= 3)
					{
						if (!CanRead(9))
							return;
						ushort wallType = br.ReadUInt16();
						t.WallColor = br.ReadByte();
						t.RedWire = br.ReadBoolean();
						t.BlueWire = br.ReadBoolean();
						t.GreenWire = br.ReadBoolean();
						t.YellowWire = br.ReadBoolean();
						byte liquidAmount = br.ReadByte();
						byte liquidType = br.ReadByte();
						if (wallType >= WallLoader.WallCount)
							wallType = 0;
						if (liquidType > 2)
							liquidType = 0;
						t.WallType = wallType;
						t.LiquidAmount = liquidAmount;
						t.LiquidType = liquidType;
					}
					else if (version >= 2)
					{
						if (!CanRead(15))
							return;
						ushort wallType = br.ReadUInt16();
						t.WallColor = br.ReadByte();
						t.RedWire = br.ReadBoolean();
						t.BlueWire = br.ReadBoolean();
						t.GreenWire = br.ReadBoolean();
						t.YellowWire = br.ReadBoolean();
						// Legacy v2 stored tile flags again here. Only apply if there is a tile.
						bool isActuated2 = br.ReadBoolean();
						bool hasActuator2 = br.ReadBoolean();
						bool isHalfBlock2 = br.ReadBoolean();
						SlopeType slope2 = (SlopeType)br.ReadByte();
						byte liquidAmount = br.ReadByte();
						byte liquidType = br.ReadByte();
						if (wallType >= WallLoader.WallCount)
							wallType = 0;
						if (liquidType > 2)
							liquidType = 0;
						t.WallType = wallType;
						t.LiquidAmount = liquidAmount;
						t.LiquidType = liquidType;
						if (hasTile)
						{
							t.IsActuated = isActuated2;
							t.HasActuator = hasActuator2;
							t.IsHalfBlock = isHalfBlock2;
							t.Slope = slope2;
						}
					}
					else
					{
						if (!CanRead(4))
							return;
						ushort wallType = br.ReadUInt16();
						byte liquidAmount = br.ReadByte();
						byte liquidType = br.ReadByte();
						if (wallType >= WallLoader.WallCount)
							wallType = 0;
						if (liquidType > 2)
							liquidType = 0;
						t.WallType = wallType;
						t.LiquidAmount = liquidAmount;
						t.LiquidType = liquidType;
					}
				}
			}
			WorldGen.RangeFrame(area.Left, area.Top, area.Width, area.Height);
			Rectangle frameArea = area;
			frameArea.Inflate(1, 1);
			for (int x = frameArea.Left; x < frameArea.Right; x++)
			{
				for (int y = frameArea.Top; y < frameArea.Bottom; y++)
				{
					if (!WorldGen.InWorld(x, y, 1))
						continue;
					WorldGen.SquareWallFrame(x, y, true);
					WorldGen.SquareTileFrame(x, y, true);
				}
			}
		}

		private static IList<TagCompound> CaptureChests(Rectangle area)
		{
			var list = new List<TagCompound>();
			for (int c = 0; c < Main.maxChests; c++)
			{
				Chest chest = Main.chest[c];
				if (chest == null)
					continue;
				if (!area.Contains(chest.x, chest.y))
					continue;

				var items = new List<TagCompound>();
				for (int s = 0; s < Chest.maxItems; s++)					
					items.Add(ItemIO.Save(chest.item[s]));

				list.Add(new TagCompound
				{
					["x"] = chest.x,
					["y"] = chest.y,
					["name"] = chest.name ?? string.Empty,
					["items"] = items,
				});
			}
			return list;
		}

		private static void RestoreChests(int roomId, Rectangle area)
		{
			ClearChestsInArea(area);
			if (!RoomChestSnapshots.TryGetValue(roomId, out var list) || list == null)
				return;

			foreach (var tag in list)
			{
				int x = tag.GetInt("x");
				int y = tag.GetInt("y");
				string name = tag.GetString("name");
				var items = tag.GetList<TagCompound>("items");

				int idx = Chest.FindChest(x, y);
				if (idx < 0)
					idx = Chest.CreateChest(x, y);
				if (idx < 0)
					continue;
				Chest chest = Main.chest[idx];
				chest.name = name;
				for (int s = 0; s < Chest.maxItems && s < items.Count; s++)
					chest.item[s] = ItemIO.Load(items[s]);
			}
		}

		private static void ClearChestsInArea(Rectangle area)
		{
			for (int c = 0; c < Main.maxChests; c++)
			{
				Chest chest = Main.chest[c];
				if (chest == null)
					continue;
				if (!area.Contains(chest.x, chest.y))
					continue;
				Main.chest[c] = null;
			}
		}

		private static void ClearTileEntitiesInArea(Rectangle area)
		{
			if (TileEntity.ByID.Count == 0)
				return;
			var toRemove = new List<int>();
			foreach (var kv in TileEntity.ByID)
			{
				TileEntity te = kv.Value;
				if (te == null)
					continue;
				if (area.Contains(te.Position.X, te.Position.Y))
					toRemove.Add(kv.Key);
			}
			foreach (int id in toRemove)
			{
				if (!TileEntity.ByID.TryGetValue(id, out TileEntity te) || te == null)
					continue;
				TileEntity.ByID.Remove(id);
				TileEntity.ByPosition.Remove(te.Position);
			}
		}

		private static void BroadcastAreaTilesToAllClients(Rectangle area)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			int chunkW = 50;
			int chunkH = 50;
			for (int c = 0; c < Main.maxPlayers; c++)
			{
				if (!Netplay.Clients[c].IsActive)
					continue;
				for (int x = area.X; x < area.Right; x += chunkW)
				{
					for (int y = area.Y; y < area.Bottom; y += chunkH)
					{
						int w = Math.Min(chunkW, area.Right - x);
						int h = Math.Min(chunkH, area.Bottom - y);
						NetMessage.SendTileSquare(c, x, y, w, h);
					}
				}
			}
		}

		private static void AssignPlayer(Player player, int roomId)
		{
			int old = GetRoomIdForPlayer(player);
			if (old != 0 && Rooms.TryGetValue(old, out Room oldRoom))
				oldRoom.Players.Remove(player.whoAmI);

			PlayerRoomMap[player.whoAmI] = roomId;
			Rooms[roomId].Players.Add(player.whoAmI);
		}

		private static void TeleportAndSync(Player player, Vector2 worldPosition)
		{
			player.Teleport(worldPosition, 1);
			player.velocity = Vector2.Zero;

			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, worldPosition.X, worldPosition.Y, 1);
				NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
				NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);
			}
		}

		private static bool ShouldSendPlayerToClient(int playerIndex, int remoteClient)
		{
			if (remoteClient < 0 || remoteClient >= Main.maxPlayers)
				return true;
			Player from = Main.player[playerIndex];
			Player to = Main.player[remoteClient];
			if (from == null || !from.active || to == null || !to.active)
				return true;
			return GetRoomIdForPlayer(from) == GetRoomIdForPlayer(to);
		}

		private static bool ShouldSendEntityToClient(int entityRoomId, int remoteClient)
		{
			if (remoteClient < 0 || remoteClient >= Main.maxPlayers)
				return true;
			Player to = Main.player[remoteClient];
			if (to == null || !to.active)
				return true;
			return entityRoomId == GetRoomIdForPlayer(to);
		}

		private static bool ShouldSendItemToClient(int itemIndex, int remoteClient)
		{
			if (itemIndex < 0 || itemIndex >= Main.maxItems)
				return true;
			Item item = Main.item[itemIndex];
			if (item == null || !item.active)
				return false;
			int roomId = GetRoomIdAtWorldPosition(item.Center);
			return ShouldSendEntityToClient(roomId, remoteClient);
		}

		public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
		{
			if (Main.netMode != NetmodeID.Server)
				return false;
			if (_routingBroadcast)
				return false;

			if (remoteClient == -1)
			{
				bool needsRouting = msgType == MessageID.SyncPlayer
					|| msgType == MessageID.PlayerControls
					|| msgType == MessageID.PlayerLifeMana
					|| msgType == MessageID.PlayerMana
					|| msgType == MessageID.PlayerTeam
					|| msgType == MessageID.PlayerBuffs
					|| msgType == MessageID.SyncItem
					|| msgType == MessageID.ItemOwner
					|| msgType == MessageID.SyncNPC
					|| msgType == MessageID.SyncProjectile
					|| msgType == MessageID.TileManipulation;

				if (needsRouting)
				{
					try
					{
						_routingBroadcast = true;
						for (int c = 0; c < Main.maxPlayers; c++)
						{
							if (c == ignoreClient)
								continue;
							if (!Netplay.Clients[c].IsActive)
								continue;
							NetMessage.SendData(msgType, c, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
						}
					}
					finally
					{
						_routingBroadcast = false;
					}
					return true;
				}
			}

			if (remoteClient < 0)
				return false;

			switch (msgType)
			{
				case MessageID.SyncPlayer:
				case MessageID.PlayerControls:
				case MessageID.PlayerLifeMana:
				case MessageID.PlayerMana:
				case MessageID.PlayerTeam:
				case MessageID.PlayerBuffs:
					return !ShouldSendPlayerToClient(number, remoteClient);

				case MessageID.SyncItem:
				case MessageID.ItemOwner:
					return !ShouldSendItemToClient(number, remoteClient);

				case MessageID.SyncNPC:
					{
						if (number < 0 || number >= Main.maxNPCs)
							return false;
						NPC npc = Main.npc[number];
						if (npc == null || !npc.active)
							return false;
						int roomId = GetRoomIdAtWorldPosition(npc.Center);
						return !ShouldSendEntityToClient(roomId, remoteClient);
					}

				case MessageID.SyncProjectile:
					{
						if (number < 0 || number >= Main.maxProjectiles)
							return false;
						Projectile proj = Main.projectile[number];
						if (proj == null || !proj.active)
							return false;
						int roomId = GetRoomIdAtWorldPosition(proj.Center);
						return !ShouldSendEntityToClient(roomId, remoteClient);
					}

				case MessageID.TileManipulation:
					{
						int x = (int)number2;
						int y = (int)number3;
						int roomId = GetRoomIdAtTile(x, y);
						return !ShouldSendEntityToClient(roomId, remoteClient);
					}
			}

			return false;
		}

		public override void SaveWorldData(TagCompound tag)
		{
			tag["roomIsolationVersion"] = 1;
			var roomsTag = new List<TagCompound>();
			foreach (var kv in Rooms)
			{
				Room r = kv.Value;
				roomsTag.Add(new TagCompound
				{
					["id"] = r.Id,
					["x"] = r.Area.X,
					["y"] = r.Area.Y,
					["w"] = r.Area.Width,
					["h"] = r.Area.Height,
				});
			}
			tag["rooms"] = roomsTag;
			var snapsTag = new List<TagCompound>();
			foreach (var kv in RoomSnapshots)
			{
				if (kv.Value == null)
					continue;
				snapsTag.Add(new TagCompound
				{
					["id"] = kv.Key,
					["data"] = kv.Value,
				});
			}
			tag["roomSnaps"] = snapsTag;

			var chestTag = new List<TagCompound>();
			foreach (var kv in RoomChestSnapshots)
			{
				if (kv.Value == null)
					continue;
				chestTag.Add(new TagCompound
				{
					["id"] = kv.Key,
					["chests"] = new List<TagCompound>(kv.Value),
				});
			}
			tag["roomChestSnaps"] = chestTag;

			// Backward compatibility keys
			if (RoomSnapshots.TryGetValue(1, out var snap) && snap != null)
				tag["roomSnap1"] = snap;
			if (RoomChestSnapshots.TryGetValue(1, out var chests) && chests != null)
				tag["roomChests1"] = new List<TagCompound>(chests);
			if (RoomSnapshots.TryGetValue(2, out var snap2) && snap2 != null)
				tag["roomSnap2"] = snap2;
			if (RoomChestSnapshots.TryGetValue(2, out var chests2) && chests2 != null)
				tag["roomChests2"] = new List<TagCompound>(chests2);
		}

		public override void LoadWorldData(TagCompound tag)
		{
			Rooms.Clear();
			BuiltRooms.Clear();
			OriginalSnapshots.Clear();
			RoomSnapshots.Clear();
			RoomChestSnapshots.Clear();
			RoomUnloadCountdown.Clear();
			if (tag.ContainsKey("rooms"))
			{
				var roomsTag = tag.GetList<TagCompound>("rooms");
				foreach (var rTag in roomsTag)
				{
					int id = rTag.GetInt("id");
					int x = rTag.GetInt("x");
					int y = rTag.GetInt("y");
					int w = rTag.GetInt("w");
					int h = rTag.GetInt("h");
					if (w <= 0 || h <= 0)
						continue;
					int safeX = Math.Clamp(x, 10, Math.Max(10, Main.maxTilesX - w - 10));
					int safeY = Math.Clamp(y, 10, Math.Max(10, Main.maxTilesY - h - 10));
					Rooms[id] = new Room { Id = id, Area = new Rectangle(safeX, safeY, w, h) };
				}
			}
			if (Rooms.Count == 0)
				InitializeRooms();
			EnsureRequiredRooms();

			if (tag.ContainsKey("roomSnaps"))
			{
				var snapsTag = tag.GetList<TagCompound>("roomSnaps");
				foreach (var sTag in snapsTag)
				{
					int id = sTag.GetInt("id");
					if (sTag.ContainsKey("data"))
						RoomSnapshots[id] = sTag.GetByteArray("data");
				}
			}
			if (tag.ContainsKey("roomChestSnaps"))
			{
				var chestTag = tag.GetList<TagCompound>("roomChestSnaps");
				foreach (var cTag in chestTag)
				{
					int id = cTag.GetInt("id");
					if (cTag.ContainsKey("chests"))
						RoomChestSnapshots[id] = cTag.GetList<TagCompound>("chests");
				}
			}

			// Backward compatibility fallback
			if (tag.ContainsKey("roomSnap1"))
				RoomSnapshots[1] = tag.GetByteArray("roomSnap1");
			if (tag.ContainsKey("roomChests1"))
				RoomChestSnapshots[1] = tag.GetList<TagCompound>("roomChests1");
			if (tag.ContainsKey("roomSnap2"))
				RoomSnapshots[2] = tag.GetByteArray("roomSnap2");
			if (tag.ContainsKey("roomChests2"))
				RoomChestSnapshots[2] = tag.GetList<TagCompound>("roomChests2");
			_loadedInWorld = true;

			PlayerRoomMap.Clear();
			foreach (var kv in Rooms)
				kv.Value.Players.Clear();
		}

		public override void PostUpdatePlayers()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (RoomUnloadCountdown.Count > 0)
			{
				var keys = new List<int>(RoomUnloadCountdown.Keys);
				foreach (int roomId in keys)
				{
					int ticks = RoomUnloadCountdown[roomId];
					if (ticks <= 0)
					{
						RoomUnloadCountdown.Remove(roomId);
						continue;
					}
					ticks--;
					if (ticks <= 0)
					{
						RoomUnloadCountdown.Remove(roomId);
						UnloadRoom(roomId);
					}
					else
					{
						RoomUnloadCountdown[roomId] = ticks;
					}
				}
			}

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (p == null || !p.active)
					continue;
				int roomId = GetRoomIdForPlayer(p);
				if (roomId == 0)
					continue;
				RoomUnloadCountdown.Remove(roomId);
				if (Rooms.TryGetValue(roomId, out Room room) && !room.Area.Contains((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f)))
				{
					TeleportAndSync(p, GetRoomSpawnWorld(roomId));
					if (Main.netMode == NetmodeID.Server)
						ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral("已被拉回舱室"), Color.OrangeRed, i);
					else
						Main.NewText("已被拉回舱室", Color.OrangeRed);
				}
			}
		}
	}
}
