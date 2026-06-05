using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Mlynar
{
	public class MlynarChestplate : MlynarSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MlynarBody);
		protected override int VanityItemType => ModContent.ItemType<MlynarBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
