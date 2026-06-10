using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Fang
{
	public class FangChestplate : FangSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(FangBody);
		protected override int VanityItemType => ModContent.ItemType<FangBody>();

		protected override void SetSetDefaults() {
			Item.defense = 23;
		}
	}
}
