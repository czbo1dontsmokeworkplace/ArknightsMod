using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai
{
	public class ExusiaiGreaves : ExusiaiSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ExusiaiLegs);
		protected override int VanityItemType => ModContent.ItemType<ExusiaiLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
