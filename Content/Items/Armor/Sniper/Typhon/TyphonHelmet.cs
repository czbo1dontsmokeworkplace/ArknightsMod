using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	public class TyphonHelmet : TyphonSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(TyphonHead);
		protected override int VanityItemType => ModContent.ItemType<TyphonHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
