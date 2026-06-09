using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy
{
	public class DorothyGreaves : DorothySetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(DorothyLegs);
		protected override int VanityItemType => ModContent.ItemType<DorothyLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
