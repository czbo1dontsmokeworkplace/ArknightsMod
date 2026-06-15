using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Ulpianus
{
	public class UlpianusGreaves : UlpianusSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(UlpianusLegs);
		protected override int VanityItemType => ModContent.ItemType<UlpianusLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
