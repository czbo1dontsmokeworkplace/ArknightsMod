using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Endfield.Striker.LastRite
{
	public class LastRiteHead : NeoArmorHead
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Endfield/Striker/LastRite/LastRite_Head_Item";
		public override int Rarity => 6;
		public override int Value => 15000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Endfield/Striker/LastRite/LastRite_Head", EquipType.Head, this, Name);
		}

		public override void SetStaticDefaultsNoServer()
		{
			Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
		}

		public override void SetVanityDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			if (Item.headSlot < 0) {
				Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
			}
		}
	}
}
