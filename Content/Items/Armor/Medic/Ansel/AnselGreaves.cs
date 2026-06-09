using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	public class AnselGreaves : AnselSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(AnselLegs);
		protected override int VanityItemType => ModContent.ItemType<AnselLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
