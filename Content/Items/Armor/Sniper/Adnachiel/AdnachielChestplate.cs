using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Adnachiel
{
	public class AdnachielChestplate : AdnachielSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(AdnachielBody);
		protected override int VanityItemType => ModContent.ItemType<AdnachielBody>();

		protected override void SetSetDefaults() {
			Item.defense = 10;
		}
	}
}
