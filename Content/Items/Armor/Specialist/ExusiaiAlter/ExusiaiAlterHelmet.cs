using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter
{
	public class ExusiaiAlterHelmet : ExusiaiAlterSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(ExusiaiAlterHead);
		protected override int VanityItemType => ModContent.ItemType<ExusiaiAlterHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
