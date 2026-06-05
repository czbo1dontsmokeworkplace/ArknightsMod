using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Adnachiel
{
	public class AdnachielGreaves : AdnachielSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(AdnachielLegs);
		protected override int VanityItemType => ModContent.ItemType<AdnachielLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
