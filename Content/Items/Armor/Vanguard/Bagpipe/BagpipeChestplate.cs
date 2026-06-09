using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	public class BagpipeChestplate : BagpipeSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(BagpipeBody);
		protected override int VanityItemType => ModContent.ItemType<BagpipeBody>();

		protected override void SetSetDefaults() {
			Item.defense = 29;
		}
	}
}
