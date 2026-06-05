using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot
{
	public class SpotGreaves : SpotSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(SpotLegs);
		protected override int VanityItemType => ModContent.ItemType<SpotLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
