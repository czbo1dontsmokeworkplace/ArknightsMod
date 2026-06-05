using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Mlynar
{
	public class MlynarGreaves : MlynarSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MlynarLegs);
		protected override int VanityItemType => ModContent.ItemType<MlynarLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
