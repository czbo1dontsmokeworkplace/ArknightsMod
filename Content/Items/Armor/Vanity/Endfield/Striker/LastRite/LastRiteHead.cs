using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Striker.LastRite
{
	public class LastRiteHead : ArknightsVanityHead
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_Head_Item";
		public override int Rarity => 6;
		public override int Value => 15000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_Head", EquipType.Head, this, Name);
		}

		public override void SafeSetStaticDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
			Mod.Logger.Info($"[LastRiteEquip] {Name} SafeSetStaticDefaults headSlot={Item.headSlot}");
		}

		public override void SafeSetDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			if (Item.headSlot < 0) {
				Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
				Mod.Logger.Info($"[LastRiteEquip] {Name} SafeSetDefaults headSlot={Item.headSlot}");
			}
		}
	}
}
