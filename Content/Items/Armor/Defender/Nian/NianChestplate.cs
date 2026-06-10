using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Nian
{
	public class NianChestplate : NianSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(NianBody);
		protected override int VanityItemType => ModContent.ItemType<NianBody>();

		protected override void SetSetDefaults() {
			Item.defense = 60;
		}
	}
}
