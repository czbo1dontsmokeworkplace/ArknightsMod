using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Matoimaru
{
	public class MatoimaruGreaves : MatoimaruSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MatoimaruLegs);
		protected override int VanityItemType => ModContent.ItemType<MatoimaruLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
