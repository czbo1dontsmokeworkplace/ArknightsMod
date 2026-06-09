using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.W
{
	public class WGreaves : WSetLegsPiece
	{
		public override int Rarity => 6;

		public override void SetSetDefaults() {
			Item.defense = 3;
		}
		protected override string VanityItemName => nameof(WLegs);
		protected override int VanityItemType => ModContent.ItemType<WLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
