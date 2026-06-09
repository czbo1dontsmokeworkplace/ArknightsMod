using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	public class RaidianGreaves : RaidianSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(RaidianLegs);
		protected override int VanityItemType => ModContent.ItemType<RaidianLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
