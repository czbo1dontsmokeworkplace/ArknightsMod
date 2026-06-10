using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	// 干员套装盔甲（独立于时装，不含 NeoArmor 生命上限/生命果削减）。
	public abstract class OperatorSetArmorItem : ModItem
	{
		public abstract int Rarity { get; }

		// 对应时装物品类名，用于复用外观装备槽与材质。
		protected abstract string VanityItemName { get; }

		// 对应时装物品的 Item.type。
		protected abstract int VanityItemType { get; }

		protected abstract EquipType EquipSlot { get; }

		// 例如 Mods.ArknightsMod.ArmorSets.Melantha
		protected abstract string SetLocalizationPrefix { get; }

		public override string Texture =>
			ModContent.GetContent<ModItem>().First(item => item.Type == VanityItemType).Texture;

		public sealed override void SetDefaults() {
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = 1;
			Item.value = Item.buyPrice(silver: 15);
			Item.rare = NeoArmorItem.GetRarity(Rarity);
			Item.vanity = false;
			AssignVanityEquipSlot();
			SetSetDefaults();
		}

		protected virtual void SetSetDefaults() { }

		private void AssignVanityEquipSlot() {
			int slot = EquipLoader.GetEquipSlot(Mod, VanityItemName, EquipSlot);
			switch (EquipSlot) {
				case EquipType.Head:
					Item.headSlot = slot;
					break;
				case EquipType.Body:
					Item.bodySlot = slot;
					break;
				case EquipType.Legs:
					Item.legSlot = slot;
					break;
			}
		}

	}

	public abstract class OperatorSetHeadItem : OperatorSetArmorItem
	{
		protected sealed override EquipType EquipSlot => EquipType.Head;

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.ApplyHelmetTooltips(
				Mod,
				tooltips,
				$"{SetLocalizationPrefix}.HeadName",
				$"{SetLocalizationPrefix}.HelmetEffect",
				$"{SetLocalizationPrefix}.SetEffect");
			AppendActiveSetTooltips(tooltips);
		}

		protected virtual void AppendActiveSetTooltips(List<TooltipLine> tooltips) { }
	}

	public abstract class OperatorSetBodyItem : OperatorSetArmorItem
	{
		protected sealed override EquipType EquipSlot => EquipType.Body;

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.ApplySetPieceTooltips(
				Mod,
				tooltips,
				$"{SetLocalizationPrefix}.SetEffect");
			AppendActiveSetTooltips(tooltips);
		}

		protected virtual void AppendActiveSetTooltips(List<TooltipLine> tooltips) { }
	}

	public abstract class OperatorSetLegsItem : OperatorSetArmorItem
	{
		protected sealed override EquipType EquipSlot => EquipType.Legs;

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			OperatorOutfitTooltipLayout.ApplySetPieceTooltips(
				Mod,
				tooltips,
				$"{SetLocalizationPrefix}.SetEffect");
			AppendActiveSetTooltips(tooltips);
		}

		protected virtual void AppendActiveSetTooltips(List<TooltipLine> tooltips) { }
	}
}
