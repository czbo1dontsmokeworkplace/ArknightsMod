using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	public class MudrockGreaves : MudrockSetLegsPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MudrockLegs);
		protected override int VanityItemType => ModContent.ItemType<MudrockLegs>();

		protected override void SetSetDefaults() {
			Item.defense = 17;
		}
	}
}
