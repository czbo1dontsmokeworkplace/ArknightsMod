using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	public class BagpipeHelmet : BagpipeSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(BagpipeHead);
		protected override int VanityItemType => ModContent.ItemType<BagpipeHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
