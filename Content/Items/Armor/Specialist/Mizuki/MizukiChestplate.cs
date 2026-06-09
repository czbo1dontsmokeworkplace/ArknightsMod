using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mizuki
{
	public class MizukiChestplate : MizukiSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MizukiBody);
		protected override int VanityItemType => ModContent.ItemType<MizukiBody>();

		protected override void SetSetDefaults() {
			Item.defense = 27;
		}
	}
}
