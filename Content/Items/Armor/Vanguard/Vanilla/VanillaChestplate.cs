using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Vanilla
{
	public class VanillaChestplate : VanillaSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(VanillaBody);
		protected override int VanityItemType => ModContent.ItemType<VanillaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 18;
		}
	}
}
