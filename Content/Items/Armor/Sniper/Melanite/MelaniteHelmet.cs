using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	public class MelaniteHelmet : MelaniteSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(MelaniteHead);
		protected override int VanityItemType => ModContent.ItemType<MelaniteHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
