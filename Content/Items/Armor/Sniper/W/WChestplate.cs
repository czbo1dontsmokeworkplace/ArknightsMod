using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.W
{
	public class WChestplate : WSetBodyPiece
	{
		public override int Rarity => 6;

		public override void SetSetDefaults() {
			Item.defense = 10;
		}
		protected override string VanityItemName => nameof(WBody);
		protected override int VanityItemType => ModContent.ItemType<WBody>();

		protected override void SetSetDefaults() {
			Item.defense = 10;
		}
	}
}
