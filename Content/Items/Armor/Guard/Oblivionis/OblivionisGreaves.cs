using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	public class OblivionisGreaves : OblivionisSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(OblivionisLegs);
		protected override int VanityItemType => ModContent.ItemType<OblivionisLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 11;
		}
	}
}
