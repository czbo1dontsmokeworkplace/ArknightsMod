using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	public class SkadiGreaves : SkadiSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(SkadiLegs);
		protected override int VanityItemType => ModContent.ItemType<SkadiLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 7;
		}
	}
}
