using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Provence
{
	public class ProvenceHelmet : ProvenceSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(ProvenceHead);
		protected override int VanityItemType => ModContent.ItemType<ProvenceHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
