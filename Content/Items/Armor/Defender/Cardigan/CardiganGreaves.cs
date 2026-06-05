using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Cardigan
{
	public class CardiganGreaves : CardiganSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(CardiganLegs);
		protected override int VanityItemType => ModContent.ItemType<CardiganLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
