using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai
{
	public class ExusiaiHelmet : ExusiaiSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ExusiaiHead);
		protected override int VanityItemType => ModContent.ItemType<ExusiaiHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
