using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot
{
	public class SpotChestplate : SpotSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(SpotBody);
		protected override int VanityItemType => ModContent.ItemType<SpotBody>();

		protected override void SetSetDefaults() {
			Item.defense = 35;
		}
	}
}
