using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	public class TyphonGreaves : TyphonSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(TyphonLegs);
		protected override int VanityItemType => ModContent.ItemType<TyphonLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
