using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	public class RosmontisHelmet : RosmontisSetHeadPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(RosmontisHead);
		protected override int VanityItemType => ModContent.ItemType<RosmontisHead>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
