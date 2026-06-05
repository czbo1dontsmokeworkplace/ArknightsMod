using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Saria
{
	public class SariaChestplate : SariaSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(SariaBody);
		protected override int VanityItemType => ModContent.ItemType<SariaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
