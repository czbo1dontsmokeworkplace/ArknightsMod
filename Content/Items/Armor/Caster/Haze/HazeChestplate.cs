using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Haze
{
	public class HazeChestplate : HazeSetBodyPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(HazeBody);
		protected override int VanityItemType => ModContent.ItemType<HazeBody>();

		protected override void SetSetDefaults() {
			Item.defense = 5;
		}
	}
}
