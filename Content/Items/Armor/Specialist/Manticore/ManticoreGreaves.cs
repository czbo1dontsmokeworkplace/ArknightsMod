using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore
{
	public class ManticoreGreaves : ManticoreSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ManticoreLegs);
		protected override int VanityItemType => ModContent.ItemType<ManticoreLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 9;
		}
	}
}
