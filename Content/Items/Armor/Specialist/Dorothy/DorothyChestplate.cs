using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy
{
	public class DorothyChestplate : DorothySetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(DorothyBody);
		protected override int VanityItemType => ModContent.ItemType<DorothyBody>();

		protected override void SetSetDefaults() {
			Item.defense = 13;
		}
	}
}
