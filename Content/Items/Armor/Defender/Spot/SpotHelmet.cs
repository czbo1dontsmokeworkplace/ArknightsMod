using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot
{
	public class SpotHelmet : SpotSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(SpotHead);
		protected override int VanityItemType => ModContent.ItemType<SpotHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
