using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	public class MudrockHelmet : MudrockSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MudrockHead);
		protected override int VanityItemType => ModContent.ItemType<MudrockHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
