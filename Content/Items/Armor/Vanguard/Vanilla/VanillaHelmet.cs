using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Vanilla
{
	public class VanillaHelmet : VanillaSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(VanillaHead);
		protected override int VanityItemType => ModContent.ItemType<VanillaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
