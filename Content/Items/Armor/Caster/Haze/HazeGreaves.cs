using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Haze
{
	public class HazeGreaves : HazeSetLegsPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(HazeLegs);
		protected override int VanityItemType => ModContent.ItemType<HazeLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
