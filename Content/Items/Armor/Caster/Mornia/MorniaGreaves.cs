using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	public class MorniaGreaves : MorniaSetLegsPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(MorniaLegs);
		protected override int VanityItemType => ModContent.ItemType<MorniaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 6;
		}
	}
}
