using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Fang
{
	public class FangGreaves : FangSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(FangLegs);
		protected override int VanityItemType => ModContent.ItemType<FangLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
