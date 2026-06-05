using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fartooth
{
	public class FartoothHelmet : FartoothSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(FartoothHead);
		protected override int VanityItemType => ModContent.ItemType<FartoothHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
