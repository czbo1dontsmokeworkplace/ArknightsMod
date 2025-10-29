using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ObjectData;
using ArknightsMod.Content.Items.Placeable.Infrastructure;
using Microsoft.Xna.Framework.Graphics;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Tiles.Infrastructure.Medical
{
	public class MedicalDeskTile : ModTile
	{
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style5x4);
			TileObjectData.newTile.Origin = new Point16(2, 2);
			TileObjectData.newTile.Width = 5;
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
			TileObjectData.addTile(Type);
			DustType = DustID.Electric;
			Main.tileLighted[Type] = true;
			AddMapEntry(new Color(151, 197, 159));
		}
        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = (fail ? 1 : 4);
        }
		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			//TileDrawer.DrawTileGlowMask(spriteBatch, i, j, Texture, Type);
		}
		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.38f;
			g = 0.85f;
			b = 1f;
		}
	}
}
