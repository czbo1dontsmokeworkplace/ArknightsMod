using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Popukar
{
	public class PopukarChestplate : PopukarSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(PopukarBody);
		protected override int VanityItemType => ModContent.ItemType<PopukarBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
