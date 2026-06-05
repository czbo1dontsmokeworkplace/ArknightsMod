using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Kroos
{
	public class KroosChestplate : KroosSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(KroosBody);
		protected override int VanityItemType => ModContent.ItemType<KroosBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
