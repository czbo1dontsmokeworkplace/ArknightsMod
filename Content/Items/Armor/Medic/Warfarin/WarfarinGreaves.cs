using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	public class WarfarinGreaves : WarfarinSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(WarfarinLegs);
		protected override int VanityItemType => ModContent.ItemType<WarfarinLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
