using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	public class WarfarinHelmet : WarfarinSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(WarfarinHead);
		protected override int VanityItemType => ModContent.ItemType<WarfarinHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
