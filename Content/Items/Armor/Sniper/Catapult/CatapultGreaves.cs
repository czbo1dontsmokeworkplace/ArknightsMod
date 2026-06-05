using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Catapult
{
	public class CatapultGreaves : CatapultSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(CatapultLegs);
		protected override int VanityItemType => ModContent.ItemType<CatapultLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
