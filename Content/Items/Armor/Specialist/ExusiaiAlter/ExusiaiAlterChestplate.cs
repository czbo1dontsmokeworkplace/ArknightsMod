using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter
{
	public class ExusiaiAlterChestplate : ExusiaiAlterSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(ExusiaiAlterBody);
		protected override int VanityItemType => ModContent.ItemType<ExusiaiAlterBody>();

		protected override void SetSetDefaults() {
			Item.defense = 11;
		}
	}
}
