using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Ling
{
	public class LingChestplate : LingSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(LingBody);
		protected override int VanityItemType => ModContent.ItemType<LingBody>();

		protected override void SetSetDefaults() {
			Item.defense = 11;
		}
	}
}
