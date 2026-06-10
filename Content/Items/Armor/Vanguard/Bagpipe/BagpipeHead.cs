using ArknightsMod.Common;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	[AutoloadEquip(EquipType.Head)]
	public class BagpipeHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 249;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<BagpipeBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<BagpipeLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<BagpipeHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<KetonColloid>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}

	internal class BagpipeHeadLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Item head = new(ModContent.ItemType<BagpipeHead>());
			return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo) {

			Texture2D texture = ModContent.Request<Texture2D>
				("ArknightsMod/Content/Items/Armor/Vanguard/Bagpipe/BagpipeHead_BackHair").Value;

			var offset = new Vector2(0, -3);
			PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
		}
	}
}
	
