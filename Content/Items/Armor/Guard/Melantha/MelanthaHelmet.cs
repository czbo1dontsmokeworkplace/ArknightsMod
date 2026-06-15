using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Melantha
{
	public class MelanthaHelmet : MelanthaSetHeadPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(MelanthaHead);
		protected override int VanityItemType => ModContent.ItemType<MelanthaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
