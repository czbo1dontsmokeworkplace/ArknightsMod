using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Lava
{
	public class LavaChestplate : LavaSetBodyPiece
	{
		public override int Rarity => 3;
		protected override string VanityItemName => nameof(LavaBody);
		protected override int VanityItemType => ModContent.ItemType<LavaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
