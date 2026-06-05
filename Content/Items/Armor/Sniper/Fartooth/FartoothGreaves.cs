using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fartooth
{
	public class FartoothGreaves : FartoothSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(FartoothLegs);
		protected override int VanityItemType => ModContent.ItemType<FartoothLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
