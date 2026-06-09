using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	public class RosmontisGreaves : RosmontisSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(RosmontisLegs);
		protected override int VanityItemType => ModContent.ItemType<RosmontisLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 7;
		}
	}
}
