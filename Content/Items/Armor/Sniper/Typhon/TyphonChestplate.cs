using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	public class TyphonChestplate : TyphonSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(TyphonBody);
		protected override int VanityItemType => ModContent.ItemType<TyphonBody>();

		protected override void SetSetDefaults() {
			Item.defense = 8;
		}
	}
}
