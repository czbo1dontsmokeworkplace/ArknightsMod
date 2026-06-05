using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Amiya
{
	public class AmiyaChestplate : AmiyaSetBodyPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(AmiyaBody);
		protected override int VanityItemType => ModContent.ItemType<AmiyaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
