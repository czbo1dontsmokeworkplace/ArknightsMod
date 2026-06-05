using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	public class RosmontisChestplate : RosmontisSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(RosmontisBody);
		protected override int VanityItemType => ModContent.ItemType<RosmontisBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
