using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Cardigan
{
	public class CardiganHelmet : CardiganSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(CardiganHead);
		protected override int VanityItemType => ModContent.ItemType<CardiganHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
