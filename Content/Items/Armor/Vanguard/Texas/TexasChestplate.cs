using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	public class TexasChestplate : TexasSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(TexasBody);
		protected override int VanityItemType => ModContent.ItemType<TexasBody>();

		protected override void SetSetDefaults() {
			Item.defense = 26;
		}
	}
}
