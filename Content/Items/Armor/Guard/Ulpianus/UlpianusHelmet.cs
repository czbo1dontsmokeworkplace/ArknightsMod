using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Ulpianus
{
	public class UlpianusHelmet : UlpianusSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(UlpianusHead);
		protected override int VanityItemType => ModContent.ItemType<UlpianusHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
