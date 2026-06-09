using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Indigo
{
	public class IndigoChestplate : IndigoSetBodyPiece
	{
		public override int Rarity => 4;
		protected override string VanityItemName => nameof(IndigoBody);
		protected override int VanityItemType => ModContent.ItemType<IndigoBody>();

		protected override void SetSetDefaults() {
			Item.defense = 9;
		}
	}
}
