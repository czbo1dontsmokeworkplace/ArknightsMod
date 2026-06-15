using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Indigo
{
	public class IndigoGreaves : IndigoSetLegsPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(IndigoLegs);
		protected override int VanityItemType => ModContent.ItemType<IndigoLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
