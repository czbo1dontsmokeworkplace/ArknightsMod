using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fiammetta
{
	public class FiammettaHelmet : FiammettaSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(FiammettaHead);
		protected override int VanityItemType => ModContent.ItemType<FiammettaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
