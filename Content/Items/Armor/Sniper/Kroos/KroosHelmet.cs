using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Kroos
{
	public class KroosHelmet : KroosSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(KroosHead);
		protected override int VanityItemType => ModContent.ItemType<KroosHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
