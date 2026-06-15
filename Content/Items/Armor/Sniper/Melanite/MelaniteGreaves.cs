using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	public class MelaniteGreaves : MelaniteSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(MelaniteLegs);
		protected override int VanityItemType => ModContent.ItemType<MelaniteLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 5;
		}
	}
}
