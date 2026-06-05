using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.W
{
	public class WHelmet : WSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(WHead);
		protected override int VanityItemType => ModContent.ItemType<WHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
