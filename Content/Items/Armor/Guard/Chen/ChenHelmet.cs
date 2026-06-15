using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Chen
{
	public class ChenHelmet : ChenSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(ChenHead);
		protected override int VanityItemType => ModContent.ItemType<ChenHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
