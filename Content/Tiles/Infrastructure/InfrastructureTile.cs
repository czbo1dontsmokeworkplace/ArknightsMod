using ArknightsMod.Content.Items.Placeable.Infrastructure;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace ArknightsMod.Content.Tiles.Infrastructure
{
	public abstract class InfrastructureTile : ModTile
	{
		/// <summary>
		/// 基础设置，已包含：<code>
		/// Main.tileSolid[Type] = true;
		/// Main.tileBlockLight[Type] = true;
		/// Main.tileMergeDirt[Type] = true;
		/// Main.tileMerge[Type][TileID.WoodBlock] = true;
		/// </code>
		/// 还需要写：<code>
		/// DustType = type;
		/// AddMapEntry(color);
		/// </summary>
		public virtual void SetDefaults() {

		}
		public sealed override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileMergeDirt[Type] = true;
			Main.tileMerge[Type][TileID.WoodBlock] = true;
			SetDefaults();
		}
		public override void NumDust(int i, int j, bool fail, ref int num) {
			num = fail ? 1 : 3;
		}
	}
}
