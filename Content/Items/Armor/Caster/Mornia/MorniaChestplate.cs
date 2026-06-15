using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	public class MorniaChestplate : MorniaSetBodyPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(MorniaBody);
		protected override int VanityItemType => ModContent.ItemType<MorniaBody>();

		protected override void SetSetDefaults() {
			Item.defense = 14;
		}
	}
}
