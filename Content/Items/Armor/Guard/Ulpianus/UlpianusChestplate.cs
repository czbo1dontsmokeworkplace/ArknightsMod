using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Ulpianus
{
	public class UlpianusChestplate : UlpianusSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(UlpianusBody);
		protected override int VanityItemType => ModContent.ItemType<UlpianusBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}
}
