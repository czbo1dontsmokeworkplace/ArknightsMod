using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Midnight
{
	public class MidnightGreaves : MidnightSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MidnightLegs);
		protected override int VanityItemType => ModContent.ItemType<MidnightLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
