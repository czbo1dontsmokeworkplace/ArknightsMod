using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Matoimaru
{
	public class MatoimaruHelmet : MatoimaruSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MatoimaruHead);
		protected override int VanityItemType => ModContent.ItemType<MatoimaruHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
