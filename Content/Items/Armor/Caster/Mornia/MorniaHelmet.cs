using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	public class MorniaHelmet : MorniaSetHeadPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(MorniaHead);
		protected override int VanityItemType => ModContent.ItemType<MorniaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 8;
		}
	}
}
