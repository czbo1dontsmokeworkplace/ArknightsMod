using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	public class KkdyAlterHelmet : KkdyAlterSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(KkdyAlterHead);
		protected override int VanityItemType => ModContent.ItemType<KkdyAlterHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
