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
	public class GlassCurtainWall : InfrastructureWall
	{
		public override void SetDefaults()
		{
			DustType = DustID.Stone;
			Main.wallLight[Type] = true;
			AddMapEntry(Color.LightSkyBlue);
		}
	}
}
