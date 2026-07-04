using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Wisadel
{
	[AutoloadEquip(EquipType.Head)]
	public class WisadelHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 189;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<WisadelBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<WisadelLegs>() && legs.neoarmor().hasUpgraded;
		}

		// 头盔单件效果：魂灵之影环绕
		public override void UpdateArmorEquip(Player player) {
			player.GetModPlayer<WisadelSetPlayer>().WisadelHelmetActive = true;
		}

		public override void ModifyArmorTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.AddWrappedEffectLines(Mod, tooltips, "Mods.ArknightsMod.ArmorSets.Wisadel.HelmetEffect", "HelmetEffect");
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<WisadelHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<PolymerizedGel>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
