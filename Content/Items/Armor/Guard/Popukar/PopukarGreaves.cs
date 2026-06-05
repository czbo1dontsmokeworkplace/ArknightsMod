using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Popukar
{
	public class PopukarGreaves : PopukarSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(PopukarLegs);
		protected override int VanityItemType => ModContent.ItemType<PopukarLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
