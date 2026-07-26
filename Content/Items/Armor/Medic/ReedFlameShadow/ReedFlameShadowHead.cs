using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.ReedFlameShadow
{
	[AutoloadEquip(EquipType.Head)]
	public class ReedFlameShadowHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 166;

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ReedFlameShadowBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<ReedFlameShadowLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ReedFlameShadowHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<OrirockConcentration>(10)
			.AddIngredient<LoxicKohl>(10)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
