	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Adnachiel;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class AdnachielDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<AdnachielHead>(),
			ModContent.ItemType<AdnachielBody>(),
			ModContent.ItemType<AdnachielLegs>()
		};
		}
	}
}
