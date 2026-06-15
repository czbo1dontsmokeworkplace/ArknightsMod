using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	// 时装装备槽注册完成后再配置 NeoArmor 的静态绘制规则，避免 SetStaticDefaults 阶段数组越界。
	internal sealed class NeoArmorEquipSystem : ModSystem
	{
		public override void PostSetupContent() {
			foreach (ModItem item in ModContent.GetContent<ModItem>()) {
				switch (item) {
					case NeoArmorHead:
						ConfigureHead(item);
						break;
					case NeoArmorBody:
						ConfigureBody(item);
						break;
					case NeoArmorLegs:
						ConfigureLegs(item);
						break;
				}
			}
		}

		private static void ConfigureHead(ModItem item) {
			int slot = EquipLoader.GetEquipSlot(item.Mod, item.Name, EquipType.Head);
			if (slot < 0 || slot >= ArmorIDs.Head.Sets.DrawHead.Length)
				return;

			ArmorIDs.Head.Sets.DrawHead[slot] = false;
		}

		private static void ConfigureBody(ModItem item) {
			int slot = EquipLoader.GetEquipSlot(item.Mod, item.Name, EquipType.Body);
			if (slot < 0 || slot >= ArmorIDs.Body.Sets.HidesArms.Length)
				return;

			ArmorIDs.Body.Sets.HidesTopSkin[slot] = true;
			ArmorIDs.Body.Sets.HidesArms[slot] = true;
		}

		private static void ConfigureLegs(ModItem item) {
			int slot = EquipLoader.GetEquipSlot(item.Mod, item.Name, EquipType.Legs);
			if (slot < 0 || slot >= ArmorIDs.Legs.Sets.HidesBottomSkin.Length)
				return;

			ArmorIDs.Legs.Sets.HidesBottomSkin[slot] = true;
		}
	}
}
