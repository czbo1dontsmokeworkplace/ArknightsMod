	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Catapult;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class CatapultDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<CatapultHead>(),
			ModContent.ItemType<CatapultBody>(),
			ModContent.ItemType<CatapultLegs>()
		};
		}
	}
}
