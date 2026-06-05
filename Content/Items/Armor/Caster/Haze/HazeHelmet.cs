using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Haze
{
	public class HazeHelmet : HazeSetHeadPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(HazeHead);
		protected override int VanityItemType => ModContent.ItemType<HazeHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
