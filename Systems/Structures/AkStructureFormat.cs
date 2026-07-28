using Terraria.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Structures
{
	// 本模组自定义的建筑结构二进制格式（扩展名 .akstruct）。
	// 由「开发测试扳手」（ArknightsMod.Content.Items.Developer.DevTestWrench）
	// 框选并导出，由「折叠式建筑」（FoldableBuildingItem）读取并还原。
	//
	// 只保存"方块数据"：Tile（类型/帧/坡面/染色）、Wall（类型/染色）、
	// 液体、四色导线。不保存物块实体（箱子内容物、告示牌文字、
	// 训练假人姿态等 TileEntity 数据）——原始需求只提到"方块数据"，
	// 没有要求实体，实体的序列化涉及更复杂的每类型专属逻辑，
	// 目前不在范围内。如果后续要支持箱子等，需要在这个格式里
	// 单独加一段 TileEntity 区块，并对应扩展 Save/Load。
	public static class AkStructureFormat
	{
		// 文件头魔数，用来快速识别文件类型 / 避免读到无关文件。
		public const string Magic = "AKST";

		// 格式版本号。以后如果改变二进制布局（比如加入 TileEntity 区块），
		// 递增这个数字，并在 Load 里按版本号分支兼容旧文件，
		// 不要直接改布局又不加版本判断——那样旧文件会读出乱码而不是明确报错。
		//
		// 版本 2：HasWall 分支新增 WallFrameX/WallFrameY（供预览用真实贴图帧），
		// 版本 1 的旧文件没有这两个字段，读取时按 0/0 补齐（真正落地时墙的帧
		// 反正会被 SquareWallFrame 重新算过，这两个字段只影响预览像不像）。
		public const ushort FormatVersion = 2;

		public const string FileExtension = ".akstruct";

		// 单元格标志位（写在每个格子最前面的一个 byte）。
		// 用位标志而不是每项都单独写一个 bool，是因为空气格/无液体格非常多，
		// 这样可以让"什么都没有"的格子只占 1 字节，而不是好几个字段各占 1 字节。
		[Flags]
		private enum CellFlags : byte
		{
			None = 0,
			HasTile = 1 << 0,
			HasWall = 1 << 1,
			HasLiquid = 1 << 2,
			IsHalfBlock = 1 << 3,
			HasActuator = 1 << 4,
			IsActuated = 1 << 5,
			HasWire = 1 << 6,
		}

		[Flags]
		private enum WireFlags : byte
		{
			None = 0,
			Red = 1 << 0,
			Blue = 1 << 1,
			Green = 1 << 2,
			Yellow = 1 << 3,
		}

		// 把世界坐标矩形区域（tile 坐标，闭区间）内的方块数据写入 .akstruct 文件。
		// topLeft：框选区域左上角（tile 坐标）。
		// bottomRight：框选区域右下角（tile 坐标，含）。
		// filePath：完整输出路径，调用方负责决定文件名/目录已存在。
		public static void Save(Point16 topLeft, Point16 bottomRight, string filePath) {
			int minX = Math.Min(topLeft.X, bottomRight.X);
			int maxX = Math.Max(topLeft.X, bottomRight.X);
			int minY = Math.Min(topLeft.Y, bottomRight.Y);
			int maxY = Math.Max(topLeft.Y, bottomRight.Y);

			int width = maxX - minX + 1;
			int height = maxY - minY + 1;

			if (width <= 0 || height <= 0)
				throw new ArgumentException("选区宽高必须大于 0。");
			if ((long)width * height > 4_000_000)
				throw new ArgumentException($"选区过大（{width}x{height}），为避免生成超大文件已中止，请缩小选区。");

			using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
			using BinaryWriter w = new(fs);

			w.Write(Magic.ToCharArray());
			w.Write(FormatVersion);
			w.Write((ushort)width);
			w.Write((ushort)height);

			for (int y = minY; y <= maxY; y++) {
				for (int x = minX; x <= maxX; x++) {
					WriteCell(w, Main.tile[x, y]);
				}
			}
		}

		private static void WriteCell(BinaryWriter w, Tile tile) {
			CellFlags flags = CellFlags.None;
			if (tile.HasTile) flags |= CellFlags.HasTile;
			if (tile.WallType != WallID.None) flags |= CellFlags.HasWall;
			if (tile.LiquidAmount > 0) flags |= CellFlags.HasLiquid;
			if (tile.IsHalfBlock) flags |= CellFlags.IsHalfBlock;
			if (tile.HasActuator) flags |= CellFlags.HasActuator;
			if (tile.IsActuated) flags |= CellFlags.IsActuated;

			WireFlags wire = WireFlags.None;
			if (tile.RedWire) wire |= WireFlags.Red;
			if (tile.BlueWire) wire |= WireFlags.Blue;
			if (tile.GreenWire) wire |= WireFlags.Green;
			if (tile.YellowWire) wire |= WireFlags.Yellow;
			if (wire != WireFlags.None) flags |= CellFlags.HasWire;

			w.Write((byte)flags);

			if (flags.HasFlag(CellFlags.HasTile)) {
				w.Write(tile.TileType);
				w.Write(tile.TileFrameX);
				w.Write(tile.TileFrameY);
				w.Write(tile.TileColor);
				w.Write((byte)tile.Slope);
			}

			if (flags.HasFlag(CellFlags.HasWall)) {
				w.Write(tile.WallType);
				w.Write(tile.WallColor);
				w.Write((short)tile.WallFrameX);
				w.Write((short)tile.WallFrameY);
			}

			if (flags.HasFlag(CellFlags.HasLiquid)) {
				w.Write((byte)tile.LiquidType);
				w.Write(tile.LiquidAmount);
			}

			if (flags.HasFlag(CellFlags.HasWire))
				w.Write((byte)wire);
		}

		// 读取一个本机磁盘上的 .akstruct 文件（比如开发测试扳手刚导出、还躺在
		// GetDefaultDirectory 里的那种）。只对导出者本机有效——
		// 要做成能发给所有玩家使用的成品建筑，应该把文件复制进模组源码目录，
		// 走下面的 LoadFromMod，让它随 .tmod 一起打包分发。
		public static StructureData Load(string filePath) {
			using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
			return Load(fs, filePath);
		}

		// 从模组自带资源里读取一份 .akstruct（随 .tmod 一起打包，每个装了这个模组的
		// 玩家、包括联机时的其他客户端，读到的都是同一份数据）。
		// mod：通常传 Mod 属性（ModItem/ModSystem 上都有）。
		// assetPath：相对模组根目录的路径，例如
		// "Assets/Structures/PortableRhodesIslandSafehouse.akstruct"，
		// 大小写、斜杠方向要跟磁盘上实际文件一致。
		public static StructureData LoadFromMod(Mod mod, string assetPath) {
			using Stream s = mod.GetFileStream(assetPath);
			return Load(s, assetPath);
		}

		private static StructureData Load(Stream stream, string sourceDescription) {
			using BinaryReader r = new(stream);

			char[] magic = r.ReadChars(4);
			if (new string(magic) != Magic)
				throw new InvalidDataException($"不是有效的 .akstruct 文件（文件头不匹配）：{sourceDescription}");

			ushort version = r.ReadUInt16();
			if (version != 1 && version != FormatVersion) {
				throw new InvalidDataException(
					$"不支持的 .akstruct 版本号 {version}（当前代码只认识版本 1 和 {FormatVersion}）：{sourceDescription}");
			}

			ushort width = r.ReadUInt16();
			ushort height = r.ReadUInt16();

			var cells = new StructureCell[width, height];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					cells[x, y] = ReadCell(r, version);
				}
			}

			return new StructureData(width, height, cells);
		}

		private static StructureCell ReadCell(BinaryReader r, ushort version) {
			var flags = (CellFlags)r.ReadByte();
			StructureCell cell = default;

			if (flags.HasFlag(CellFlags.HasTile)) {
				cell.HasTile = true;
				cell.TileType = r.ReadUInt16();
				cell.TileFrameX = r.ReadInt16();
				cell.TileFrameY = r.ReadInt16();
				cell.TileColor = r.ReadByte();
				cell.Slope = r.ReadByte();
			}

			if (flags.HasFlag(CellFlags.HasWall)) {
				cell.HasWall = true;
				cell.WallType = r.ReadUInt16();
				cell.WallColor = r.ReadByte();
				if (version >= 2) {
					cell.WallFrameX = r.ReadInt16();
					cell.WallFrameY = r.ReadInt16();
				}
			}

			if (flags.HasFlag(CellFlags.HasLiquid)) {
				cell.LiquidType = r.ReadByte();
				cell.LiquidAmount = r.ReadByte();
			}

			if (flags.HasFlag(CellFlags.HasWire)) {
				var wire = (WireFlags)r.ReadByte();
				cell.RedWire = wire.HasFlag(WireFlags.Red);
				cell.BlueWire = wire.HasFlag(WireFlags.Blue);
				cell.GreenWire = wire.HasFlag(WireFlags.Green);
				cell.YellowWire = wire.HasFlag(WireFlags.Yellow);
			}

			cell.IsHalfBlock = flags.HasFlag(CellFlags.IsHalfBlock);
			cell.HasActuator = flags.HasFlag(CellFlags.HasActuator);
			cell.IsActuated = flags.HasFlag(CellFlags.IsActuated);

			return cell;
		}

		// 结构数据存放的默认目录：%UserProfile%/Documents/My Games/Terraria/tModLoader/ArknightsModStructures/
		// （即 Main.SavePath 下的子目录，和存档同一个位置，本机不同用户互不干扰）。
		// 目录不存在会自动创建。
		public static string GetDefaultDirectory() {
			string dir = Path.Combine(Main.SavePath, "ArknightsModStructures");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	// 单个格子的方块数据。struct 是为了让 StructureCell[,] 数组不额外产生几十万个对象。
	public struct StructureCell
	{
		public bool HasTile;
		public ushort TileType;
		public short TileFrameX;
		public short TileFrameY;
		public byte TileColor;
		public byte Slope;

		public bool HasWall;
		public ushort WallType;
		public byte WallColor;
		public short WallFrameX;
		public short WallFrameY;

		public byte LiquidType;
		public byte LiquidAmount;

		public bool IsHalfBlock;
		public bool HasActuator;
		public bool IsActuated;

		public bool RedWire, BlueWire, GreenWire, YellowWire;
	}

	// 内存中的一份已加载结构，提供预览取色与放置逻辑。
	public class StructureData
	{
		public readonly int Width;
		public readonly int Height;
		private readonly StructureCell[,] _cells;

		public StructureData(int width, int height, StructureCell[,] cells) {
			Width = width;
			Height = height;
			_cells = cells;
		}

		public StructureCell this[int x, int y] => _cells[x, y];

		// 把结构放置到世界里，worldTopLeft 是结构左上角要落在的 tile 坐标。
		// ⚠ 目前是直接覆盖式放置（逐格 WorldGen.PlaceTile / 清墙/补液体），
		// 没有做"目标位置是否已被占用"的碰撞检查、
		// 没有处理需要 WorldGen.PlaceObject 的大型多格家具
		// （床、桌子这类"帧重要"物块如果只靠逐格 PlaceTile 摆放，
		// 锚点/朝向大概率会错，需要额外按物块的 origin 数据整体摆放），
		// 也没有处理 TileEntity。这些都要等真正拿到第一份 .akstruct
		// 测试文件、看清楚实际存的是什么内容之后再补——现在这些代码
		// 是按用户要求"提前部署"的骨架，还不能直接拿来对着正式建筑用。
		public void PlaceAt(Point16 worldTopLeft) {
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					int wx = worldTopLeft.X + x;
					int wy = worldTopLeft.Y + y;
					if (!WorldGen.InWorld(wx, wy, 5))
						continue;

					PlaceCell(wx, wy, _cells[x, y]);
				}
			}

			// 批量改动后需要通知客户端/网络重新生成对应区块的帧信息，
			// 否则视觉上可能不会立刻刷新。
			WorldGen.RangeFrame(worldTopLeft.X, worldTopLeft.Y, worldTopLeft.X + Width, worldTopLeft.Y + Height);

			// ⚠ RangeFrame 不会正确重算墙的帧——本格式没有保存墙的 FrameX/FrameY
			// （StructureCell 只存了 WallType/WallColor），墙格子摆上去之后帧数据是
			// 默认的 0，图会错位。要单独按格调用 SquareWallFrame 才能让墙正确拼接，
			// 这是照抄 PortableSafehouseSystem.FrameWalls 里已经验证过的做法，
			// 不要指望 RangeFrame 替你把这一步也做了。
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					int wx = worldTopLeft.X + x;
					int wy = worldTopLeft.Y + y;
					if (WorldGen.InWorld(wx, wy, 5) && Main.tile[wx, wy].WallType != WallID.None)
						WorldGen.SquareWallFrame(wx, wy, true);
				}
			}
		}

		private static void PlaceCell(int x, int y, StructureCell cell) {
			Tile tile = Main.tile[x, y];

			if (cell.HasTile) {
				tile.HasTile = true;
				tile.TileType = cell.TileType;
				tile.TileFrameX = cell.TileFrameX;
				tile.TileFrameY = cell.TileFrameY;
				tile.TileColor = cell.TileColor;
				tile.Slope = (SlopeType)cell.Slope;
				tile.IsHalfBlock = cell.IsHalfBlock;
				tile.HasActuator = cell.HasActuator;
				tile.IsActuated = cell.IsActuated;
			}
			else {
				tile.HasTile = false;
			}

			if (cell.HasWall) {
				tile.WallType = cell.WallType;
				tile.WallColor = cell.WallColor;
			}
			else {
				tile.WallType = WallID.None;
			}

			tile.LiquidType = cell.LiquidType;
			tile.LiquidAmount = cell.LiquidAmount;

			tile.RedWire = cell.RedWire;
			tile.BlueWire = cell.BlueWire;
			tile.GreenWire = cell.GreenWire;
			tile.YellowWire = cell.YellowWire;
		}
	}
}
