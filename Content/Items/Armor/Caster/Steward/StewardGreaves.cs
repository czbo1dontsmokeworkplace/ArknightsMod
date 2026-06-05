using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Steward
{
	public class StewardGreaves : StewardSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(StewardLegs);
		protected override int VanityItemType => ModContent.ItemType<StewardLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
