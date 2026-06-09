using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Kroos
{
	public class KroosGreaves : KroosSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(KroosLegs);
		protected override int VanityItemType => ModContent.ItemType<KroosLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
