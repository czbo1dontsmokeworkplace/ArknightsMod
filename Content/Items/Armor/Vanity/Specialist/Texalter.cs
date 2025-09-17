using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist
{
	[AutoloadEquip(EquipType.Head)]
	public class TexalterHead : ArknightsVanityHead { }

	[AutoloadEquip(EquipType.Body)]
	public class TexalterBody : ArknightsVanityBody
	{
		public override void Load()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Back}", EquipType.Back, this);
		}

		public override void SafeSetStaticDefaults()
		{
			int cape = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);
			ArmorIDs.Body.Sets.IncludedCapeBack[Item.bodySlot] = cape;
			ArmorIDs.Body.Sets.IncludedCapeBackFemale[Item.bodySlot] = cape;
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class TexalterLegs : ArknightsVanityLegs { }
}
