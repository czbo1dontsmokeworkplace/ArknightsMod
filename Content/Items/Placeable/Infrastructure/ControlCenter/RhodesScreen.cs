using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using ArknightsMod.Content.Tiles.Infrastructure.ControlCenter;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure.ControlCenter
{
    public class RhodesScreen : ArknightsInfraFurniture
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<RhodesScreenTile>());
		}
    }
}
