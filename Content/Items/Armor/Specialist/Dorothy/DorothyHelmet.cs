using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy
{
	public class DorothyHelmet : DorothySetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(DorothyHead);
		protected override int VanityItemType => ModContent.ItemType<DorothyHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
