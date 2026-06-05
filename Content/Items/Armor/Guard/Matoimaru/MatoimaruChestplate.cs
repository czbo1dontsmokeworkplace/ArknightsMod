using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Matoimaru
{
	public class MatoimaruChestplate : MatoimaruSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MatoimaruBody);
		protected override int VanityItemType => ModContent.ItemType<MatoimaruBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
