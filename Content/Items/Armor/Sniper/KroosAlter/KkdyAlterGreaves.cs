using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	public class KkdyAlterGreaves : KkdyAlterSetLegsPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(KkdyAlterLegs);
		protected override int VanityItemType => ModContent.ItemType<KkdyAlterLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 4;
		}
	}
}
