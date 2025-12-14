using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using ArknightsMod.Content.Tiles.Infrastructure.ControlCenter;
using ArknightsMod.Content.Tiles.Infrastructure;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure.ControlCenter
{
    public class RhodesHugeScreen : ArknightsInfraFurniture
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<RhodesHugeScreenTile>());
		}
    }
}
