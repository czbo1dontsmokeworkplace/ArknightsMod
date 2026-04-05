using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure.InstancedSpace;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure.InstancedSpace
{
	public class ProtocolSpacePortal : ModItem
	{
		public override string Texture => "ArknightsMod/Assets/Textures/InstancedSpace/ProtocolSpacePortal";

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 99;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 12;
			Item.useAnimation = 12;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.consumable = true;
			Item.value = Item.buyPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.createTile = ModContent.TileType<ProtocolSpacePortalTile>();
		}
	}
}
