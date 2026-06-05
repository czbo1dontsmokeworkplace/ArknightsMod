using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fartooth
{
	public class FartoothChestplate : FartoothSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(FartoothBody);
		protected override int VanityItemType => ModContent.ItemType<FartoothBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
