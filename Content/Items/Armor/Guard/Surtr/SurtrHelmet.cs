using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Surtr
{
	public class SurtrHelmet : SurtrSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(SurtrHead);
		protected override int VanityItemType => ModContent.ItemType<SurtrHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
