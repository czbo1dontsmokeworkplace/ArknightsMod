using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Steward
{
	public class StewardHelmet : StewardSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(StewardHead);
		protected override int VanityItemType => ModContent.ItemType<StewardHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
