using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
	public class LaPlumaHelmet : LaPlumaSetHeadPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(LaPlumaHead);
		protected override int VanityItemType => ModContent.ItemType<LaPlumaHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
