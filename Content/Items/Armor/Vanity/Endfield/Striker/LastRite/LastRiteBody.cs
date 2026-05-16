using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Endfield.Striker.LastRite
{
	public class LastRiteBody : ArknightsVanityBody
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_Body_Item";
		public override int Rarity => 6;
		public override int Value => 15000;

		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_Body1.4", EquipType.Body, this, Name);
		}

		public override void SafeSetStaticDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Item.bodySlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
			Mod.Logger.Info($"[LastRiteEquip] {Name} SafeSetStaticDefaults bodySlot={Item.bodySlot}");
		}

		public override void SafeSetDefaults()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			if (Item.bodySlot < 0) {
				Item.bodySlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
				Mod.Logger.Info($"[LastRiteEquip] {Name} SafeSetDefaults bodySlot={Item.bodySlot}");
			}
		}
	}
}
