using ArknightsMod.Content.Items.Placeable.Infrastructure.HROffice;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace ArknightsMod.Content.Tiles.Infrastructure.HROffice
{
	public class OfficeWallTile : InfrastructureWall
	{
		public override void SetDefaults()
		{
			DustType = DustID.Stone;
			AddMapEntry(new Color(73, 71, 69));
			RegisterItemDrop(ModContent.ItemType<OfficeWallItem>());
		}
	}
}
