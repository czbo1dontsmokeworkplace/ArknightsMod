using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fiammetta
{
	public class FiammettaChestplate : FiammettaSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(FiammettaBody);
		protected override int VanityItemType => ModContent.ItemType<FiammettaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
