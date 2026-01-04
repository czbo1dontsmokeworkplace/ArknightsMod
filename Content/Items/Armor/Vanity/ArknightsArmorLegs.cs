using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity
{
	public abstract class ArknightsArmorLegs : ArknightsVanityLegs
	{
		public virtual int LifeBonus => 0;
		public sealed override void SafeSetDefaults() {
			Item.vanity = false;
			SetArmorDefaults();
		}
		public virtual void SetArmorDefaults() { }
		public sealed override void UpdateEquip(Player player)
		{
			player.GetModPlayer<ArknightsArmorPlayer>().LifeCrystalAndFruitEffectReduction = 0.5f;
			player.statLifeMax2 += LifeBonus;

			UpdateArmorEquip(player);
		}
		public virtual void UpdateArmorEquip(Player Player) {

		}
		public virtual void ModifyArmorTooltips(ref List<TooltipLine> tooltips) { }
		public sealed override void ModifyTooltips(List<TooltipLine> tooltips) {
			int index = -1;
			for (int i = 0; i < tooltips.Count; i++) {
				if (tooltips[i].Name.Equals("Equipable")) {
					index = i;
					break;
				}
			}
			if (index == -1)
				index = tooltips.Count - 1;

			var lifeBonusText = new TooltipLine(Mod, "LifeBonus",
					Language.GetTextValue("Mods.ArknightsMod.ArmorBonus.LifeBonus", LifeBonus));
			tooltips.Insert(index + 1, lifeBonusText);

			var lifeReducText = new TooltipLine(Mod, "LifeReduction",
					Language.GetTextValue("Mods.ArknightsMod.ArmorBonus.LifeItemsReduction", 0.5.ToString("P0")));
			lifeReducText.OverrideColor = Colors.RarityTrash;
			tooltips.Insert(index + 2, lifeReducText);

			ModifyArmorTooltips(ref tooltips);
		}
	}
}
