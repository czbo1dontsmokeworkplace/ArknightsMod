using ArknightsMod.Content.Items.Placeable.Infrastructure.HROffice;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.WorldBuilding;

namespace ArknightsMod.Content.Tiles.Infrastructure
{
	public class LightTubeTile : ModTile
	{
		public override void SetStaticDefaults() {
			AddMapEntry(Color.DarkGray);
			DustType = DustID.Electric;
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = false;
			Main.tileWaterDeath[Type] = false;
			Main.tileLavaDeath[Type] = false;
			Main.tileLighted[Type] = true;
			Main.tileBlockLight[Type] = false;

			TileObjectData.newTile.CopyFrom(TileObjectData.StyleAlch);
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.CoordinateHeights = [16];

			TileObjectData.newTile.AnchorLeft = default;
			TileObjectData.newTile.AnchorRight = default;
			TileObjectData.newTile.AnchorTop = default;
			TileObjectData.newTile.AnchorBottom = default;
			TileObjectData.newTile.AnchorWall = false;
			TileObjectData.newTile.AnchorAlternateTiles = null;

			TileObjectData.addTile(Type);
		}
		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			float strength = 2f;
			r = 0.65f * strength;
			g = 0.61f * strength;
			b = 0.61f * strength;
		}
	}
}