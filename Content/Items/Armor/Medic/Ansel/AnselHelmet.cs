using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	public class AnselHelmet : AnselSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(AnselHead);
		protected override int VanityItemType => ModContent.ItemType<AnselHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
