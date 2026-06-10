using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Amiya
{
	public class AmiyaHelmet : AmiyaSetHeadPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(AmiyaHead);
		protected override int VanityItemType => ModContent.ItemType<AmiyaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
