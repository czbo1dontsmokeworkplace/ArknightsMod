using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Entelechia
{
	public class EntelechiaHelmet : EntelechiaSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(EntelechiaHead);
		protected override int VanityItemType => ModContent.ItemType<EntelechiaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
