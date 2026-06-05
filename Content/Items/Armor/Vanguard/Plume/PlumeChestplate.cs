using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	public class PlumeChestplate : PlumeSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(PlumeBody);
		protected override int VanityItemType => ModContent.ItemType<PlumeBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
