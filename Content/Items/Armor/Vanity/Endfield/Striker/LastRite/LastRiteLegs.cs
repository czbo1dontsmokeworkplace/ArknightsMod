using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Striker.LastRite
{
	public class LastRiteLegs : ArknightsVanityLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_Legs_Item";
		public override int Rarity => 6;
		public override int Value => 15000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_Legs", EquipType.Legs, this, Name);
		}

		public override void SafeSetStaticDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
			Mod.Logger.Info($"[LastRiteEquip] {Name} SafeSetStaticDefaults legSlot={Item.legSlot}");
		}

		public override void SafeSetDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			if (Item.legSlot < 0) {
				Item.legSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
				Mod.Logger.Info($"[LastRiteEquip] {Name} SafeSetDefaults legSlot={Item.legSlot}");
			}
		}
	}
}
