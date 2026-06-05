using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	public class RaidianChestplate : RaidianSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(RaidianBody);
		protected override int VanityItemType => ModContent.ItemType<RaidianBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
