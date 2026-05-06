using ArknightsMod.Content.Items.Placeable.Infrastructure.Pipe;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Tiles.Infrastructure.Pipe
{
	public class PipeWall : InfrastructureWall
	{
		public override void SetDefaults()
		{
			DustType = DustID.Iron;
			AddMapEntry(Color.DarkGray);
			RegisterItemDrop(ModContent.ItemType<PipeWallItem>());
		}
	}
}
