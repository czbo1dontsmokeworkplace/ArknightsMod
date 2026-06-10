using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Hibiscus
{
	public class HibiscusHelmet : HibiscusSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(HibiscusHead);
		protected override int VanityItemType => ModContent.ItemType<HibiscusHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
