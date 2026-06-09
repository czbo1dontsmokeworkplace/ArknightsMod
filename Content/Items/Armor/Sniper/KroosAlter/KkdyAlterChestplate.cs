using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	public class KkdyAlterChestplate : KkdyAlterSetBodyPiece
	{
		public override int Rarity => 5;
		protected override string VanityItemName => nameof(KkdyAlterBody);
		protected override int VanityItemType => ModContent.ItemType<KkdyAlterBody>();

		protected override void SetSetDefaults() {
			Item.defense = 11;
		}
	}
}
