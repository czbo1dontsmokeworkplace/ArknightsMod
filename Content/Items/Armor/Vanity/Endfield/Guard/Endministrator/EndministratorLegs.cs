using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Guard.Endministrator
{
	public class EndministratorLegs : ArknightsVanityLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Legs_Item";
		public override int Rarity => 6;
		public override int Value => 560000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Legs", EquipType.Legs, this, Name);
		}

		public override void SafeSetStaticDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
		}

		public override void SafeSetDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			if (Item.legSlot < 0)
				Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
		}
	}
}
