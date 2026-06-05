using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter
{
	public class TexalterGreaves : TexalterSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(TexalterLegs);
		protected override int VanityItemType => ModContent.ItemType<TexalterLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
