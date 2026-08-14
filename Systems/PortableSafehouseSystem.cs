using ArknightsMod.Content.Items.Consumables.PortableSafehouse;
using ArknightsMod.Content.Tiles.Infrastructure.ReceptionRoom;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Systems
{
	public sealed partial class PortableSafehouseSystem : ModSystem
	{
		public const int Width = 29;
		public const int Height = 11;

		// 物块存储逻辑
		private readonly record struct CellPlacement(int X, int Y, bool HasTile, ushort TileType, string TileName, short FrameX, short FrameY, byte TileColor, ushort WallType, string WallName, byte WallColor, byte Slope, bool IsHalfBlock, bool HasActuator, bool IsActuated, byte LiquidAmount, byte LiquidType, bool RedWire, bool BlueWire, bool GreenWire, bool YellowWire, bool IsTileInvisible, bool IsTileFullbright, bool IsWallInvisible, bool IsWallFullbright);

		public static Point GetDeploymentTopLeft() => Main.MouseWorld.ToTileCoordinates();

		public override void PostDrawTiles()
		{
			if (Main.dedServ || Main.gameMenu)
				return;

			Player player = Main.LocalPlayer;
			if (player == null || !player.active || player.HeldItem.type != ModContent.ItemType<PortableSafehouseDeploymentUnit>())
				return;

			Point topLeft = GetDeploymentTopLeft();
			Rectangle area = new Rectangle(topLeft.X, topLeft.Y, Width, Height);
			bool inWorld = WorldGen.InWorld(area.Left, area.Top, 10) && WorldGen.InWorld(area.Right - 1, area.Bottom - 1, 10);
			Color color = inWorld ? new Color(255, 220, 80, 180) : new Color(255, 80, 80, 180);
			Rectangle screenArea = new Rectangle(
				area.Left * 16 - (int)Main.screenPosition.X,
				area.Top * 16 - (int)Main.screenPosition.Y,
				(int)(area.Width * 16 * Main.GameViewMatrix.Zoom.X),
				(int)(area.Height * 16 * Main.GameViewMatrix.Zoom.Y));

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
			try
			{
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				Main.spriteBatch.Draw(pixel, screenArea, color * 0.12f);
				const int border = 2;
				Main.spriteBatch.Draw(pixel, new Rectangle(screenArea.X, screenArea.Y, screenArea.Width, border), color);
				Main.spriteBatch.Draw(pixel, new Rectangle(screenArea.X, screenArea.Bottom - border, screenArea.Width, border), color);
				Main.spriteBatch.Draw(pixel, new Rectangle(screenArea.X, screenArea.Y, border, screenArea.Height), color);
				Main.spriteBatch.Draw(pixel, new Rectangle(screenArea.Right - border, screenArea.Y, border, screenArea.Height), color);
			}
			finally
			{
				Main.spriteBatch.End();
			}
		}

		public static bool TryDeploy(Point topLeft, out Rectangle area, out string failure)
		{
			area = new Rectangle(topLeft.X, topLeft.Y, Width, Height);
			failure = string.Empty;
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				failure = PortableSafehouseDeploymentUnit.DeployClientFailureKey;
				return false;
			}

			if (!WorldGen.InWorld(area.Left, area.Top, 10) || !WorldGen.InWorld(area.Right - 1, area.Bottom - 1, 10))
			{
				failure = PortableSafehouseDeploymentUnit.DeployOutOfBoundsFailureKey;
				return false;
			}

			if (!CanClear(area, out failure))
				return false;

			ClearArea(area);
			PlaceLayout(topLeft);
			FrameWalls(area);
			PlaceDecor(topLeft, new Point(5, 9), new Point(3, 6), ReceptionRoomDecorSystem.DecorKind.ComputerDesk, 1, 0);
			PlaceDecor(topLeft, new Point(3, 9), new Point(2, 7), ReceptionRoomDecorSystem.DecorKind.OfficeChair, 1, 0);
			PlaceDisplayItem(topLeft, new Point(16, 1), ItemID.Nanites);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, area.X + (Width / 2), area.Y + (Height / 2), Width);

			return true;
		}

		private static bool CanClear(Rectangle area, out string failure)
		{
			for (int index = 0; index < Main.maxChests; index++)
			{
				Chest chest = Main.chest[index];
				if (chest != null && area.Contains(chest.x, chest.y))
				{
					failure = PortableSafehouseDeploymentUnit.DeployChestFailureKey;
					return false;
				}
			}

			foreach (TileEntity entity in TileEntity.ByID.Values)
			{
				if (entity != null && area.Contains(entity.Position.X, entity.Position.Y))
				{
					failure = PortableSafehouseDeploymentUnit.DeployTileEntityFailureKey;
					return false;
				}
			}

			for (int x = area.Left; x < area.Right; x++)
			{
				for (int y = area.Top; y < area.Bottom; y++)
				{
					Tile tile = Main.tile[x, y];
					if (tile.HasTile && tile.TileType < TileID.Sets.Ore.Length && TileID.Sets.Ore[tile.TileType])
					{
						failure = PortableSafehouseDeploymentUnit.DeployOreFailureKey;
						return false;
					}
				}
			}

			failure = string.Empty;
			return true;
		}

		private static void ClearArea(Rectangle area)
		{
			for (int x = area.Left; x < area.Right; x++)
				for (int y = area.Top; y < area.Bottom; y++)
					Main.tile[x, y].ClearEverything();
		}

		private static void PlaceLayout(Point topLeft)
		{
			// 部署时写入图块数据
			foreach (CellPlacement placement in Layout)
			{
				Tile tile = Main.tile[topLeft.X + placement.X, topLeft.Y + placement.Y];
				tile.HasTile = placement.HasTile;
				tile.TileType = ResolveTileType(placement.TileType, placement.TileName);
				tile.TileFrameX = placement.FrameX;
				tile.TileFrameY = placement.FrameY;
				tile.TileColor = placement.TileColor;
				tile.WallType = ResolveWallType(placement.WallType, placement.WallName);
				tile.WallColor = placement.WallColor;
				tile.Slope = (SlopeType)placement.Slope;
				tile.IsHalfBlock = placement.IsHalfBlock;
				tile.HasActuator = placement.HasActuator;
				tile.IsActuated = placement.IsActuated;
				tile.LiquidAmount = placement.LiquidAmount;
				tile.LiquidType = placement.LiquidType;
				tile.RedWire = placement.RedWire;
				tile.BlueWire = placement.BlueWire;
				tile.GreenWire = placement.GreenWire;
				tile.YellowWire = placement.YellowWire;
				tile.IsTileInvisible = placement.IsTileInvisible;
				tile.IsTileFullbright = placement.IsTileFullbright;
				tile.IsWallInvisible = placement.IsWallInvisible;
				tile.IsWallFullbright = placement.IsWallFullbright;
			}
		}

		private static void PlaceDecor(Point topLeft, Point anchorOffset, Point decorOffset, ReceptionRoomDecorSystem.DecorKind kind, sbyte direction, byte variant)
		{
			Point anchor = topLeft + anchorOffset;
			Tile tile = Main.tile[anchor.X, anchor.Y];
			tile.HasTile = true;
			tile.TileType = (ushort)ModContent.TileType<ReceptionRoomDecorAnchorTile>();
			tile.TileFrameX = 0;
			tile.TileFrameY = 0;

			int id = ModContent.GetInstance<ReceptionRoomDecorAnchorTE>().Place(anchor.X, anchor.Y);
			if (id < 0 || TileEntity.ByID[id] is not ReceptionRoomDecorAnchorTE decor)
				return;

			ReceptionRoomDecorAnchorTE.AnchorByPosition[new Point16(anchor.X, anchor.Y)] = decor;
			decor.Instances.Add(new ReceptionRoomDecorSystem.DecorInstance
			{
				Kind = kind,
				TopLeft = new Point16(topLeft.X + decorOffset.X, topLeft.Y + decorOffset.Y),
				Direction = direction,
				Variant = variant,
			});

			if (Main.netMode != NetmodeID.SinglePlayer)
				decor.SendSync();
		}

		private static void FrameWalls(Rectangle area)
		{
			for (int x = area.Left; x < area.Right; x++)
				for (int y = area.Top; y < area.Bottom; y++)
					if (Main.tile[x, y].WallType != WallID.None)
						WorldGen.SquareWallFrame(x, y, true);
		}

		private static void PlaceDisplayItem(Point topLeft, Point displayOffset, int itemType)
		{
			Point display = topLeft + displayOffset;
			if (Main.tile[display.X, display.Y].TileType != TileID.ItemFrame)
				return;

			int id = TEItemFrame.Place(display.X, display.Y);
			if (TileEntity.ByID[id] is not TEItemFrame itemFrame)
				return;

			itemFrame.item.SetDefaults(itemType);
			itemFrame.item.stack = 1;
			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.TileEntitySharing, number: itemFrame.ID, number2: display.X, number3: display.Y);
		}

		private static ushort ResolveTileType(ushort savedType, string fullName)
		{
			if (TryResolveTile(fullName, out int type))
				return (ushort)type;
			return savedType;
		}

		private static ushort ResolveWallType(ushort savedType, string fullName)
		{
			if (TryResolveWall(fullName, out int type))
				return (ushort)type;
			return savedType;
		}

		private static bool TryResolveTile(string fullName, out int type)
		{
			type = 0;
			return TrySplitName(fullName, out string modName, out string name) && ModContent.TryFind(modName, name, out ModTile tile) && (type = tile.Type) >= 0;
		}

		private static bool TryResolveWall(string fullName, out int type)
		{
			type = 0;
			return TrySplitName(fullName, out string modName, out string name) && ModContent.TryFind(modName, name, out ModWall wall) && (type = wall.Type) >= 0;
		}

		private static bool TrySplitName(string fullName, out string modName, out string name)
		{
			int separator = fullName.IndexOf('/');
			if (separator <= 0 || separator >= fullName.Length - 1)
			{
				modName = string.Empty;
				name = string.Empty;
				return false;
			}

			modName = fullName[..separator];
			name = fullName[(separator + 1)..];
			return true;
		}
	}
}
