using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mizuki
{
	public class MizukiHelmet : MizukiSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MizukiHead);
		protected override int VanityItemType => ModContent.ItemType<MizukiHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
