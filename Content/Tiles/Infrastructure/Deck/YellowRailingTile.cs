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

namespace ArknightsMod.Content.Tiles.Infrastructure.Deck
{
	public class YellowRailingTile : ModTile
	{
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style5x4);
			TileObjectData.newTile.Origin = new Point16(2, 1);
			TileObjectData.newTile.Width = 5;
			TileObjectData.newTile.Height = 2;
			TileObjectData.newTile.CoordinateHeights = [16, 16];
			TileObjectData.addTile(Type);
            DustType = DustID.Copper;
			LocalizedText name = CreateMapEntryName();
			AddMapEntry(new Color(133, 64, 24), name);
		}
        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = (fail ? 1 : 4);
        }
    }
}
