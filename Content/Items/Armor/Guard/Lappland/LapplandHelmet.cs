using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Lappland
{
	public class LapplandHelmet : LapplandSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(LapplandHead);
		protected override int VanityItemType => ModContent.ItemType<LapplandHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
