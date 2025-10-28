using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Wisadel;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class WisdelDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<WisadelHead>(),
			ModContent.ItemType<WisadelBody>(),
			ModContent.ItemType<WisadelLegs>()
		};
		}
	}
}
