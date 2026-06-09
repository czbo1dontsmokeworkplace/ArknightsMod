using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	public class BagpipeGreaves : BagpipeSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(BagpipeLegs);
		protected override int VanityItemType => ModContent.ItemType<BagpipeLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 10;
		}
	}
}
