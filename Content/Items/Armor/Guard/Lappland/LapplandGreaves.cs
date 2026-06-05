using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Lappland
{
	public class LapplandGreaves : LapplandSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(LapplandLegs);
		protected override int VanityItemType => ModContent.ItemType<LapplandLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
