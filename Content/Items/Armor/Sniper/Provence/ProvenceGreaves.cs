using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Provence
{
	public class ProvenceGreaves : ProvenceSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ProvenceLegs);
		protected override int VanityItemType => ModContent.ItemType<ProvenceLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 6;
		}
	}
}
