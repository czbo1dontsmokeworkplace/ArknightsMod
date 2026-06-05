using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Lappland
{
	public class LapplandChestplate : LapplandSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(LapplandBody);
		protected override int VanityItemType => ModContent.ItemType<LapplandBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
