using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	public class SkadiHelmet : SkadiSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(SkadiHead);
		protected override int VanityItemType => ModContent.ItemType<SkadiHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
