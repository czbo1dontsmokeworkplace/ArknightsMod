using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Endfield.Striker.LastRite
{
	public class LastRiteLegs : NeoArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Endfield/Striker/LastRite/LastRite_Legs_Item";
		public override int Rarity => 6;
		public override int Value => 15000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Endfield/Striker/LastRite/LastRite_Legs", EquipType.Legs, this, Name);
		}

		public override void SetStaticDefaultsNoServer()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
		}

		public override void SetVanityDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			if (Item.legSlot < 0) {
				Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
			}
		}
	}
}
