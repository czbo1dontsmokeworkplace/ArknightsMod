using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Midnight
{
	public class MidnightHelmet : MidnightSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MidnightHead);
		protected override int VanityItemType => ModContent.ItemType<MidnightHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
