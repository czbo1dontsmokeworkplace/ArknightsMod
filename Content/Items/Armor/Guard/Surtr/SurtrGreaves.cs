using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Surtr
{
	public class SurtrGreaves : SurtrSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(SurtrLegs);
		protected override int VanityItemType => ModContent.ItemType<SurtrLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
