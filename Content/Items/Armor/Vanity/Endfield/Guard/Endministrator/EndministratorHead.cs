using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Guard.Endministrator
{
	public class EndministratorHead : ArknightsVanityHead
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Head_Item";
		public override int Rarity => 6;
		public override int Value => 560000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Head", EquipType.Head, this, Name);
		}

		public override void SafeSetStaticDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
		}

		public override void SafeSetDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			if (Item.headSlot < 0)
				Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
		}
	}
}
