using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.Players.InstancedSpace;
using ArknightsMod.Content.Tiles.Infrastructure.InstancedSpace;

namespace ArknightsMod.Systems.InstancedSpace
{
	public sealed class ProtocolSpaceEventSystem : ModSystem
	{
		private const int ProtocolRoomId = 2;
		private const int RequiredKills = 10;
		private const int SpawnIntervalTicks = 60;
		private const int ExitCountdownTicks = 60;
		private const int CompletedUiTicks = 180;

		internal static bool IsEventActive { get; private set; }
		internal static bool IsEventCompleted { get; private set; }
		internal static int KillCount { get; private set; }
		internal static Point16 ExitPortalTilePos { get; private set; }
		internal static bool ExitPortalPlaced { get; private set; }

		private static int _spawnTicker;
		private static int _originiumSlugType;
		private static int _completedUiTicks;

		public override void OnWorldUnload()
		{
			IsEventActive = false;
			IsEventCompleted = false;
			KillCount = 0;
			ExitPortalPlaced = false;
			ExitPortalTilePos = Point16.NegativeOne;
			_spawnTicker = 0;
			_completedUiTicks = 0;
		}

		public override void PostUpdateWorld()
		{
			if (_completedUiTicks > 0)
				_completedUiTicks--;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (!IsEventActive)
				return;

			if (!IsEventCompleted)
			{
				_spawnTicker++;
				if (_spawnTicker >= SpawnIntervalTicks)
				{
					_spawnTicker = 0;
					TrySpawnSlugs();
				}

				if (KillCount >= RequiredKills)
					CompleteEvent();
			}
		}

		public override void PostDrawInterface(SpriteBatch spriteBatch)
		{
			if (Main.dedServ)
				return;
			if (!IsEventActive)
				return;
			if (IsEventCompleted && _completedUiTicks <= 0)
				return;

			float progress = IsEventCompleted ? 1f : MathHelper.Clamp(KillCount / (float)RequiredKills, 0f, 1f);
			const string nameKey = "Mods.ArknightsMod.ProtocolSpace.Name";
			string eventName = Language.Exists(nameKey) ? Language.GetTextValue(nameKey) : "\u6D4B\u8BD5\u534F\u8BAE\u7A7A\u95F4";
			string label = IsEventCompleted ? $"{eventName}: \u5B8C\u6210" : $"{eventName}: {KillCount}/{RequiredKills}";

			Vector2 size = new Vector2(220f, 16f);
			Vector2 pos = new Vector2(Main.screenWidth - size.X - 24f, Main.screenHeight - size.Y - 64f);

			Texture2D pix = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pix, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), new Color(20, 20, 20, 220));
			spriteBatch.Draw(pix, new Rectangle((int)pos.X + 2, (int)pos.Y + 2, (int)((size.X - 4) * progress), (int)size.Y - 4), new Color(160, 90, 255, 220));
			Utils.DrawBorderString(spriteBatch, label, pos + new Vector2(0, -20f), Color.White);
		}

		internal static void RequestStartFromClient(int i, int j)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket p = ModContent.GetInstance<ArknightsMod>().GetPacket();
			p.Write((short)ArknightsMod.ArkMessageID.ProtocolSpaceRequestStart);
			p.Write((short)i);
			p.Write((short)j);
			p.Send();
		}

		internal static void RequestExitFromClient(int i, int j)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket p = ModContent.GetInstance<ArknightsMod>().GetPacket();
			p.Write((short)ArknightsMod.ArkMessageID.ProtocolSpaceRequestExitInteract);
			p.Write((short)i);
			p.Write((short)j);
			p.Send();
		}

		internal static void RequestExitCountdownFromClient()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket p = ModContent.GetInstance<ArknightsMod>().GetPacket();
			p.Write((short)ArknightsMod.ArkMessageID.ProtocolSpaceRequestExitCountdown);
			p.Send();
		}

		internal static void ReceivePacket(BinaryReader reader, int whoAmI, ArknightsMod.ArkMessageID type)
		{
			switch (type)
			{
				case ArknightsMod.ArkMessageID.ProtocolSpaceRequestStart:
					ReceiveStartRequest(reader, whoAmI);
					break;
				case ArknightsMod.ArkMessageID.ProtocolSpaceRequestExitInteract:
					ReceiveExitInteract(reader, whoAmI);
					break;
				case ArknightsMod.ArkMessageID.ProtocolSpaceRequestExitCountdown:
					ReceiveExitCountdownRequest(whoAmI);
					break;
				case ArknightsMod.ArkMessageID.ProtocolSpaceSyncState:
					ReceiveSync(reader);
					break;
				case ArknightsMod.ArkMessageID.ProtocolSpaceExitCountdown:
					ReceiveExitCountdown(reader);
					break;
			}
		}

		private static void ReceiveStartRequest(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			short i = reader.ReadInt16();
			short j = reader.ReadInt16();
			if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;
			Player player = Main.player[whoAmI];
			if (player == null || !player.active)
				return;
			StartForPlayer(player);
		}

		private static void ReceiveExitInteract(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			_ = reader.ReadInt16();
			_ = reader.ReadInt16();
			if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;
			Player player = Main.player[whoAmI];
			if (player == null || !player.active)
				return;
			player.GetModPlayer<ProtocolSpacePlayer>().TryRequestExit();
		}

		private static void ReceiveExitCountdownRequest(int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;
			Player player = Main.player[whoAmI];
			if (player == null || !player.active)
				return;
			StartExitCountdownForPlayer(player);
		}

		internal static void StartForPlayer(Player player)
		{
			if (player == null || !player.active)
				return;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			EnsureSlugType();

			if (!IsEventActive)
			{
				IsEventActive = true;
				IsEventCompleted = false;
				KillCount = 0;
				ExitPortalPlaced = false;
				ExitPortalTilePos = Point16.NegativeOne;
				_spawnTicker = 0;
				SyncStateToClients();
			}

			InstancedRoomSystem.TryEnterRoom(player, ProtocolRoomId);
		}

		internal static void ForceExit(Player player)
		{
			if (player == null || !player.active)
				return;
			InstancedRoomSystem.ExitRoom(player, true);
			TryEndEventIfEmpty();
		}

		internal static void StartExitCountdownForPlayer(Player player)
		{
			if (!IsEventCompleted)
				return;
			if (player == null || !player.active)
				return;
			player.GetModPlayer<ProtocolSpacePlayer>().ReceiveExitCountdown(ExitCountdownTicks);
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket p = ModContent.GetInstance<ArknightsMod>().GetPacket();
				p.Write((short)ArknightsMod.ArkMessageID.ProtocolSpaceExitCountdown);
				p.Write((byte)player.whoAmI);
				p.Write((short)ExitCountdownTicks);
				p.Send();
			}
		}

		private static void ReceiveExitCountdown(BinaryReader reader)
		{
			byte who = reader.ReadByte();
			short ticks = reader.ReadInt16();
			if (who >= Main.maxPlayers)
				return;
			Player p = Main.player[who];
			if (p == null || !p.active)
				return;
			p.GetModPlayer<ProtocolSpacePlayer>().ReceiveExitCountdown(ticks);
		}

		internal static bool IsOriginiumSlug(int npcType)
		{
			EnsureSlugType();
			return npcType == _originiumSlugType;
		}

		internal static bool IsInProtocolRoom(Vector2 worldCenter)
		{
			int x = (int)(worldCenter.X / 16f);
			int y = (int)(worldCenter.Y / 16f);
			return InstancedRoomSystem.GetRoomIdAtTile(x, y) == ProtocolRoomId;
		}

		internal static void NotifySlugKilled()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (!IsEventActive || IsEventCompleted)
				return;
			KillCount++;
			SyncStateToClients();
		}

		private static void EnsureSlugType()
		{
			if (_originiumSlugType > 0)
				return;
			_originiumSlugType = ModContent.NPCType<OriginiumSlug>();
		}

		private static void TrySpawnSlugs()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (_originiumSlugType <= 0)
				return;
			if (!InstancedRoomSystem.Rooms.TryGetValue(ProtocolRoomId, out var room))
				return;

			for (int who = 0; who < Main.maxPlayers; who++)
			{
				Player player = Main.player[who];
				if (player == null || !player.active || player.dead)
					continue;
				if (InstancedRoomSystem.GetRoomIdForPlayer(player) != ProtocolRoomId)
					continue;
				TrySpawnNearPlayer(room.Area, player);
			}
		}

		private static void TrySpawnNearPlayer(Rectangle area, Player player)
		{
			int spawnSide = Main.rand.NextBool() ? 1 : -1;
			int offsetTiles = 55;
			int px = (int)(player.Center.X / 16f);
			int py = (int)(player.Center.Y / 16f);
			int sx = px + spawnSide * offsetTiles;
			sx = Math.Clamp(sx, area.Left + 2, area.Right - 3);

			int startY = Math.Clamp(py, area.Top + 2, area.Bottom - 3);
			int groundY = -1;
			for (int y = startY; y < area.Bottom - 2; y++)
			{
				if (WorldGen.SolidTile(sx, y) && !WorldGen.SolidTile(sx, y - 1))
				{
					groundY = y;
					break;
				}
			}
			if (groundY < 0)
				return;

			Vector2 pos = new Vector2(sx * 16 + 8, (groundY - 1) * 16);
			int id = NPC.NewNPC(new EntitySource_Misc("ProtocolSpace"), (int)pos.X, (int)pos.Y, _originiumSlugType);
			if (id >= 0 && id < Main.maxNPCs)
			{
				NPC npc = Main.npc[id];
				if (npc != null)
				{
					npc.netUpdate = true;
				}
			}
		}

		private static void CompleteEvent()
		{
			if (IsEventCompleted)
				return;
			IsEventCompleted = true;
			_completedUiTicks = CompletedUiTicks;
			PlaceExitPortalNearPlayers();
			SyncStateToClients();
		}

		private static void PlaceExitPortalNearPlayers()
		{
			if (ExitPortalPlaced)
				return;
			if (!InstancedRoomSystem.Rooms.TryGetValue(ProtocolRoomId, out var room))
				return;

			Player anchorPlayer = null;
			for (int who = 0; who < Main.maxPlayers; who++)
			{
				Player p = Main.player[who];
				if (p == null || !p.active || p.dead)
					continue;
				if (InstancedRoomSystem.GetRoomIdForPlayer(p) != ProtocolRoomId)
					continue;
				anchorPlayer = p;
				break;
			}
			if (anchorPlayer == null)
				return;

			bool hasPlacement = TryFindExitPortalPlacement(room.Area, anchorPlayer, out int placeX, out int placeY);
			if (!hasPlacement)
				hasPlacement = TryForceExitPortalPlacement(room.Area, anchorPlayer, out placeX, out placeY);
			if (!hasPlacement)
			{
				if (Main.netMode != NetmodeID.Server)
					Main.NewText("ProtocolSpace: exit portal placement failed", Color.MediumPurple);
				return;
			}

			int exitTileType = ModContent.TileType<ProtocolSpaceExitPortalTile>();
			bool placed = WorldGen.PlaceObject(placeX, placeY, exitTileType);
			if (Main.netMode != NetmodeID.Server)
				Main.NewText($"ProtocolSpace: exit portal place at ({placeX},{placeY}) -> {placed}", Color.MediumPurple);

			if (!placed)
			{
				if (!TryForcePlaceExitPortalTiles(placeX, placeY, exitTileType))
					return;
				placed = true;
			}

			bool verified = VerifyExitPortalTiles(placeX, placeY, exitTileType);
			if (!verified)
			{
				if (!TryForcePlaceExitPortalTiles(placeX, placeY, exitTileType))
					return;
				verified = VerifyExitPortalTiles(placeX, placeY, exitTileType);
			}
			if (!verified)
				return;
			ForceFrameExitPortal(placeX, placeY);
			ExitPortalPlaced = true;
			ExitPortalTilePos = new Point16(placeX, placeY);

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, placeX, placeY, 3, 3);
		}

		private static void ForceFrameExitPortal(int topLeftX, int topLeftY)
		{
			for (int x = topLeftX; x < topLeftX + 3; x++)
			{
				for (int y = topLeftY; y < topLeftY + 3; y++)
				{
					if (!WorldGen.InWorld(x, y, 10))
						continue;
					WorldGen.SquareTileFrame(x, y, true);
				}
			}
			WorldGen.RangeFrame(topLeftX, topLeftY, topLeftX + 2, topLeftY + 2);
		}

		private static bool VerifyExitPortalTiles(int topLeftX, int topLeftY, int tileType)
		{
			for (int x = topLeftX; x < topLeftX + 3; x++)
			{
				for (int y = topLeftY; y < topLeftY + 3; y++)
				{
					if (!WorldGen.InWorld(x, y, 10))
						return false;
					Tile t = Main.tile[x, y];
					if (!t.HasTile || t.TileType != tileType)
						return false;
				}
			}
			return true;
		}

		private static bool TryForcePlaceExitPortalTiles(int topLeftX, int topLeftY, int tileType)
		{
			for (int x = topLeftX; x < topLeftX + 3; x++)
			{
				for (int y = topLeftY; y < topLeftY + 3; y++)
				{
					if (!WorldGen.InWorld(x, y, 10))
						return false;
					Tile t = Main.tile[x, y];
					t.HasTile = true;
					t.TileType = (ushort)tileType;
					t.Slope = 0;
					t.IsHalfBlock = false;
					t.TileFrameX = (short)((x - topLeftX) * 16);
					t.TileFrameY = (short)((y - topLeftY) * 16);
				}
			}
			ForceFrameExitPortal(topLeftX, topLeftY);
			return true;
		}

		private static bool TryForceExitPortalPlacement(Rectangle area, Player player, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;
			int px = (int)(player.Center.X / 16f);
			int baseX = Math.Clamp(px, area.Left + 3, area.Right - 4);
			int groundY = area.Bottom - 3;
			int baseY = groundY - 1;
			int tlx = baseX - 1;
			int tly = baseY - 2;
			if (tlx < area.Left + 1 || tlx + 2 >= area.Right - 1)
				return false;
			if (tly < area.Top + 1 || tly + 2 >= area.Bottom - 1)
				return false;

			BeginClear3x3(tlx, tly);
			EnsureSolidFoundation(tlx, tly + 3);
			placeX = tlx;
			placeY = tly;
			return true;
		}

		private static void EnsureSolidFoundation(int tlx, int y)
		{
			for (int x = tlx; x < tlx + 3; x++)
			{
				if (!WorldGen.InWorld(x, y, 10))
					continue;
				Tile t = Main.tile[x, y];
				if (t.HasTile && Main.tileSolid[t.TileType])
					continue;
				t.HasTile = true;
				t.TileType = TileID.GrayBrick;
				t.Slope = 0;
				t.IsHalfBlock = false;
				t.TileFrameX = 0;
				t.TileFrameY = 0;
			}
			WorldGen.RangeFrame(tlx, y, tlx + 2, y);
		}

		private static void BeginClear3x3(int tlx, int tly)
		{
			for (int ix = tlx; ix < tlx + 3; ix++)
			{
				for (int iy = tly; iy < tly + 3; iy++)
				{
					Tile t = Main.tile[ix, iy];
					t.HasTile = false;
					t.TileType = 0;
					t.TileFrameX = 0;
					t.TileFrameY = 0;
				}
			}
			WorldGen.RangeFrame(tlx, tly, tlx + 2, tly + 2);
		}

		private static bool TryFindExitPortalPlacement(Rectangle area, Player player, out int placeX, out int placeY)
		{
			placeX = 0;
			placeY = 0;

			int centerX = (int)(player.Center.X / 16f);
			int centerY = (int)(player.Center.Y / 16f);
			centerX = Math.Clamp(centerX, area.Left + 5, area.Right - 6);
			centerY = Math.Clamp(centerY, area.Top + 5, area.Bottom - 6);

			for (int radius = 3; radius <= 40; radius++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					for (int dy = -radius; dy <= radius; dy++)
					{
						if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
							continue;
						int x = centerX + dx;
						int y = centerY + dy;
						if (!area.Contains(x, y))
							continue;
						if (!FindGroundForPortal(area, x, y, out int topLeftX, out int topLeftY))
							continue;
						placeX = topLeftX;
						placeY = topLeftY;
						return true;
					}
				}
			}

			return false;
		}

		private static bool FindGroundForPortal(Rectangle area, int x, int y, out int topLeftX, out int topLeftY)
		{
			topLeftX = 0;
			topLeftY = 0;

			int groundY = -1;
			for (int j = y; j < area.Bottom - 2; j++)
			{
				if (WorldGen.SolidTile(x, j) && !WorldGen.SolidTile(x, j - 1))
				{
					groundY = j;
					break;
				}
			}
			if (groundY < 0)
				return false;

			int baseX = Math.Clamp(x, area.Left + 2, area.Right - 3);
			int baseY = groundY - 1;
			int tlx = baseX - 1;
			int tly = baseY - 2;
			if (tlx < area.Left + 1 || tlx + 2 >= area.Right - 1)
				return false;
			if (tly < area.Top + 1 || tly + 2 >= area.Bottom - 1)
				return false;

			for (int ix = tlx; ix < tlx + 3; ix++)
			{
				for (int iy = tly; iy < tly + 3; iy++)
				{
					if (!WorldGen.InWorld(ix, iy, 10))
						return false;
					if (Main.tile[ix, iy].HasTile)
						return false;
				}
			}
			for (int ix = tlx; ix < tlx + 3; ix++)
			{
				if (!WorldGen.SolidTile(ix, baseY + 1))
					return false;
			}

			topLeftX = tlx;
			topLeftY = tly;
			return true;
		}

		internal static bool IsExitLocked(Player player)
		{
			if (!IsEventActive)
				return false;
			if (IsEventCompleted)
				return false;
			if (player == null || !player.active)
				return false;
			return InstancedRoomSystem.GetRoomIdForPlayer(player) == ProtocolRoomId;
		}

		private static void TryEndEventIfEmpty()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (!IsEventActive)
				return;
			for (int who = 0; who < Main.maxPlayers; who++)
			{
				Player player = Main.player[who];
				if (player == null || !player.active || player.dead)
					continue;
				if (InstancedRoomSystem.GetRoomIdForPlayer(player) == ProtocolRoomId)
					return;
			}
			Point16 exitPos = ExitPortalTilePos;
			IsEventActive = false;
			IsEventCompleted = false;
			KillCount = 0;
			ExitPortalPlaced = false;
			ExitPortalTilePos = Point16.NegativeOne;
			_spawnTicker = 0;
			int tx = exitPos.X;
			int ty = exitPos.Y;
			if (tx > 0 && ty > 0 && WorldGen.InWorld(tx, ty, 10))
			{
				WorldGen.KillTile(tx, ty, false, false, true);
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendTileSquare(-1, tx, ty, 3, 3);
			}
			SyncStateToClients();
		}

		private static void SyncStateToClients()
		{
			if (Main.netMode != NetmodeID.Server)
				return;
			ModPacket p = ModContent.GetInstance<ArknightsMod>().GetPacket();
			p.Write((short)ArknightsMod.ArkMessageID.ProtocolSpaceSyncState);
			p.Write(IsEventActive);
			p.Write(IsEventCompleted);
			p.Write((byte)Math.Clamp(KillCount, 0, 255));
			p.Write(ExitPortalPlaced);
			p.Write((short)ExitPortalTilePos.X);
			p.Write((short)ExitPortalTilePos.Y);
			p.Send();
		}

		private static void ReceiveSync(BinaryReader reader)
		{
			bool prevCompleted = IsEventCompleted;
			IsEventActive = reader.ReadBoolean();
			IsEventCompleted = reader.ReadBoolean();
			KillCount = reader.ReadByte();
			ExitPortalPlaced = reader.ReadBoolean();
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			ExitPortalTilePos = new Point16(x, y);
			if (!prevCompleted && IsEventCompleted)
				_completedUiTicks = CompletedUiTicks;
		}
	}
}
