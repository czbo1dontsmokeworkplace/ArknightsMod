using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Entelechia
{
	public class EntelechiaGreaves : EntelechiaSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(EntelechiaLegs);
		protected override int VanityItemType => ModContent.ItemType<EntelechiaLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 11;
		}
	}
}
