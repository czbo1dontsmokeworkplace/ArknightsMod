using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity
{
	public abstract class ArknightsArmorHead : ArknightsVanityHead
	{
		public virtual (float ratio, int value) LifeReplacement => (0f, 0);
		public sealed override void SafeSetDefaults()
		{
			Item.vanity = false;
			SetArmorDefaults();
		}
		public virtual void SetArmorDefaults() { }
		public virtual void ModifyArmorTooltips(ref List<TooltipLine> tooltips) { }
		public sealed override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			int index = -1;
			for (int i = 0; i < tooltips.Count; i++) {
				if (tooltips[i].Name.Equals("Equipable")) {
					index = i;
					break;
				}
			}
			if (index == -1)
				index = tooltips.Count - 1;
			tooltips.Insert(index + 1, new TooltipLine(Mod, "LifeReplacement", Language.GetTextValue("Mods.ArknightsMod.ArmorBonus.LifeReplacement",
				LifeReplacement.ratio.ToString("P0"), LifeReplacement.value)));

			ModifyArmorTooltips(ref tooltips);
		}
		public sealed override void UpdateEquip(Player player)
		{
			player.GetModPlayer<ArknightsArmorPlayer>().LifeReplacement_Head = LifeReplacement;
			UpdateArmorEquip(player);
		}
		public virtual void UpdateArmorEquip(Player Player) {

		}
	}
}
