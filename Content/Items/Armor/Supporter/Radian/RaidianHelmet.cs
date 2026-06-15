using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	public class RaidianHelmet : RaidianSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(RaidianHead);
		protected override int VanityItemType => ModContent.ItemType<RaidianHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
