using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Hibiscus
{
	public class HibiscusGreaves : HibiscusSetLegsPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(HibiscusLegs);
		protected override int VanityItemType => ModContent.ItemType<HibiscusLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 3;
		}
	}
}
