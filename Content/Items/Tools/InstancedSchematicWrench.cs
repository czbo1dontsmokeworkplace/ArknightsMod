using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ArknightsMod.Content.Players.InstancedSpace;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Items.Tools
{
	public class InstancedSchematicWrench : ModItem
	{
		public override string Texture => "Terraria/Images/Item_4";

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 10;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = false;
			Item.useTurn = false;
			Item.noMelee = true;
			Item.damage = 0;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(silver: 50);
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;

			var mp = player.GetModPlayer<InstancedSchematicWrenchPlayer>();
			Point tile = Main.MouseWorld.ToTileCoordinates();
			if (!WorldGen.InWorld(tile.X, tile.Y, 1))
				return true;

			if (player.altFunctionUse != 2)
			{
				mp.First = tile;
				Main.NewText($"已选择第一个点: {tile.X},{tile.Y}");
				return true;
			}

			if (!mp.First.HasValue)
			{
				Main.NewText("请先左键选择第一个点", Color.OrangeRed);
				return true;
			}

			Point a = mp.First.Value;
			Point b = tile;
			mp.First = null;

			int left = Math.Min(a.X, b.X);
			int right = Math.Max(a.X, b.X);
			int top = Math.Min(a.Y, b.Y);
			int bottom = Math.Max(a.Y, b.Y);
			int w = right - left + 1;
			int h = bottom - top + 1;

			const int MaxSide = 800;
			if (w <= 0 || h <= 0 || w > MaxSide || h > MaxSide)
			{
				Main.NewText($"区域大小不合法: {w}x{h}", Color.OrangeRed);
				return true;
			}

			Rectangle area = new Rectangle(left, top, w, h);

			byte[] snap = InstancedRoomSystem.CaptureAreaSnapshot(area);
			var chests = InstancedRoomSystem.CaptureChestSnapshot(area);

			var tag = new TagCompound
			{
				["w"] = w,
				["h"] = h,
				["data"] = snap,
				["chests"] = chests,
				["time"] = DateTime.UtcNow.ToString("O"),
			};

			string dir = Path.Combine(Main.SavePath, "ModSaves", Mod.Name, "Schematics");
			Directory.CreateDirectory(dir);

			string fileName = SanitizeFileName($"schem_{Main.worldName}_{left}_{top}_{w}x{h}.nbt");
			string path = Path.Combine(dir, fileName);
			TagIO.ToFile(tag, path, true);

			Main.NewText($"已导出: {fileName}", Color.LightGreen);
			return true;
		}

		private static string SanitizeFileName(string s)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
				s = s.Replace(c, '_');
			return s;
		}
	}
}
