using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter
{
	public class ExusiaiAlterGreaves : ExusiaiAlterSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(ExusiaiAlterLegs);
		protected override int VanityItemType => ModContent.ItemType<ExusiaiAlterLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
