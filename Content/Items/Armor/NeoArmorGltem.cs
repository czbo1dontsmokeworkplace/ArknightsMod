using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Armor
{
	public static class NeoArmorUtils {
		public static NeoArmorGItem neoarmor(this Item item) {
			return item.GetGlobalItem<NeoArmorGItem>();
		}
		public static readonly Condition NeedVanity = new Condition("Mods.ArknightsMod.NeoArmor.NeedVanity", () => true);
	}
	public class NeoArmorGItem : GlobalItem
	{
		public override bool InstancePerEntity => true;
		public bool hasUpgraded = false;
		public bool isNeoArmor;
		public override void PostUpdate(Item Item) {
			if (isNeoArmor && hasUpgraded) {
				Item.maxStack = 1;
				if (Item.ModItem is NeoArmorItem neoArmor)
					neoArmor.SetBaseArmorDefaults();
			}
		}
		public override void UpdateInventory(Item Item, Player player) {
			if (isNeoArmor && hasUpgraded) {
				Item.maxStack = 1;
			}
		}
		public override void OnCreated(Item Item, ItemCreationContext context) {
			if (isNeoArmor)
			{
				if (context is RecipeItemCreationContext)
				{
					RecipeItemCreationContext c = context as RecipeItemCreationContext;
					foreach (var i in c.ConsumedItems)
					{
						if (Item.type == i.type && i.neoarmor().isNeoArmor && !i.neoarmor().hasUpgraded)
						{
							hasUpgraded = true;
							if(Item.ModItem is NeoArmorItem neoArmor)
								neoArmor.SetDefaults();
							break;
						}
					}
				}
			}
		}

		public override void NetSend(Item Item, BinaryWriter writer) {
			writer.Write(hasUpgraded);
		}
		public override void NetReceive(Item Item, BinaryReader reader) {
			hasUpgraded = reader.ReadBoolean();
		}
		public override void LoadData(Item Item, TagCompound tag) {
			hasUpgraded = tag.GetBool("hasUpgraded");
		}
		public override void SaveData(Item Item, TagCompound tagCompound) {
			tagCompound["hasUpgraded"] = hasUpgraded;
		}
	}
}
