using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter
{
	public class TexalterHelmet : TexalterSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(TexalterHead);
		protected override int VanityItemType => ModContent.ItemType<TexalterHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
