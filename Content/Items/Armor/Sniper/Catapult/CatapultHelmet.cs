using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Catapult
{
	public class CatapultHelmet : CatapultSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(CatapultHead);
		protected override int VanityItemType => ModContent.ItemType<CatapultHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
