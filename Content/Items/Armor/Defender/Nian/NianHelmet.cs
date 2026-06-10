using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Nian
{
	public class NianHelmet : NianSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(NianHead);
		protected override int VanityItemType => ModContent.ItemType<NianHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
