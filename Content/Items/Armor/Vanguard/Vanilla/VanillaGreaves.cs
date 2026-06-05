using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Vanilla
{
	public class VanillaGreaves : VanillaSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(VanillaLegs);
		protected override int VanityItemType => ModContent.ItemType<VanillaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
