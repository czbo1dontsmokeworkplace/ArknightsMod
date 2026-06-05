using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
	public class LaPlumaGreaves : LaPlumaSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(LaPlumaLegs);
		protected override int VanityItemType => ModContent.ItemType<LaPlumaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
