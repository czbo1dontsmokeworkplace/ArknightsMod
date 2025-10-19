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
	public class OfficeBlockTile : InfrastructureTile
	{
		public override void SetDefaults()
		{
			DustType = DustID.Stone;
			AddMapEntry(new Color(166, 178, 180));
			RegisterItemDrop(ModContent.ItemType<OfficeBlockItem>());
		}
	}
}
