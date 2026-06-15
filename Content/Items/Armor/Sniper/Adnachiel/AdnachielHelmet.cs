using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Adnachiel
{
	public class AdnachielHelmet : AdnachielSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(AdnachielHead);
		protected override int VanityItemType => ModContent.ItemType<AdnachielHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
