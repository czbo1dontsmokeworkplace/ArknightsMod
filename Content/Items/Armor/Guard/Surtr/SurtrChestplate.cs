using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Surtr
{
	public class SurtrChestplate : SurtrSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(SurtrBody);
		protected override int VanityItemType => ModContent.ItemType<SurtrBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
