using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	public class SkadiChestplate : SkadiSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(SkadiBody);
		protected override int VanityItemType => ModContent.ItemType<SkadiBody>();

		protected override void SetSetDefaults() {
			Item.defense = 20;
		}
	}
}
