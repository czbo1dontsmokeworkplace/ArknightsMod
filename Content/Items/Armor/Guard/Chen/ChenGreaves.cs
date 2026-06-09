using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Chen
{
	public class ChenGreaves : ChenSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(ChenLegs);
		protected override int VanityItemType => ModContent.ItemType<ChenLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 10;
		}
	}
}
