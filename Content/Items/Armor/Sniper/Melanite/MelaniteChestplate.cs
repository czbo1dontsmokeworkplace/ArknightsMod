using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	public class MelaniteChestplate : MelaniteSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(MelaniteBody);
		protected override int VanityItemType => ModContent.ItemType<MelaniteBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
