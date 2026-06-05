using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Melantha
{
	public class MelanthaGreaves : MelanthaSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MelanthaLegs);
		protected override int VanityItemType => ModContent.ItemType<MelanthaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
