using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fiammetta
{
	public class FiammettaGreaves : FiammettaSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(FiammettaLegs);
		protected override int VanityItemType => ModContent.ItemType<FiammettaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
