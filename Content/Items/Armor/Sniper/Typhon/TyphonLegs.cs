using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	[AutoloadEquip(EquipType.Legs)]
	public class TyphonLegs : ArknightsVanityLegs
	{
		public override void SafeSetDefaults()
		{
			Item.rare = ItemRarityID.Red;
		}
	}
}
