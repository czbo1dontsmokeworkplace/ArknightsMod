using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Amiya
{
	public class AmiyaGreaves : AmiyaSetLegsPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(AmiyaLegs);
		protected override int VanityItemType => ModContent.ItemType<AmiyaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
