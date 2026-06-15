using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mortis
{
	[AutoloadEquip(EquipType.Head)]
	public class MortisHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 221;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<MortisBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<MortisLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MortisHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<CyclicenePrefab>(8)
			.AddIngredient<PolyesterPack>(13)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
