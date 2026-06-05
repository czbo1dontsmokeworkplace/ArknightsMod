using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai
{
	public class ExusiaiChestplate : ExusiaiSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ExusiaiBody);
		protected override int VanityItemType => ModContent.ItemType<ExusiaiBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
