using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Indigo
{
	public class IndigoHelmet : IndigoSetHeadPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(IndigoHead);
		protected override int VanityItemType => ModContent.ItemType<IndigoHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
