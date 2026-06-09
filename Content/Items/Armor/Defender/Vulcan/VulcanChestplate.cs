using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	public class VulcanChestplate : VulcanSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(VulcanBody);
		protected override int VanityItemType => ModContent.ItemType<VulcanBody>();

		protected override void SetSetDefaults() {
			Item.defense = 44;
		}
	}
}
