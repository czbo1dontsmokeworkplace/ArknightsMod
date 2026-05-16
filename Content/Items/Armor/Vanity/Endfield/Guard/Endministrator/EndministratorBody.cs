using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Guard.Endministrator
{
	public class EndministratorBody : ArknightsVanityBody
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Body_Item";
		public override int Rarity => 6;
		public override int Value => 560000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Guard/Endministrator/Endmin_Female_Body1.4", EquipType.Body, this, Name);
		}

		public override void SafeSetStaticDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			Item.bodySlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
		}

		public override void SafeSetDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			if (Item.bodySlot < 0)
				Item.bodySlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
		}
	}
}
