using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Typhon
{
	[AutoloadEquip(EquipType.Body)]
	public class TyphonBody : ArknightsVanityBody
	{
		public override void SafeSetDefaults()
		{
			Item.rare = ItemRarityID.Red;
		}
	}
}
