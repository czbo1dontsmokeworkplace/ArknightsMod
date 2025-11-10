using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class KaltsitDefault: ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Medic.Kaltsit.KaltsitHead>(),
			ModContent.ItemType<Armor.Vanity.Medic.Kaltsit.KaltsitBody>(),
			ModContent.ItemType<Armor.Vanity.Medic.Kaltsit.KaltsitLegs>()
		};
		}
	}
}
