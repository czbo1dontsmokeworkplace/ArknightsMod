using ArknightsMod.Content.Items.Armor.Guard.Melantha;
using ArknightsMod.Content.Items.Armor.Guard.Utage;
using ArknightsMod.Content.Items.Armor.Sniper.W;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	// 在时装装备槽注册完成后再配置套装盔甲的静态绘制规则。
	internal class OperatorSetArmorSystem : ModSystem
	{
		public override void PostSetupContent() {
			ConfigureHead<WHead>();
			ConfigureBody<WBody>();
			ConfigureLegs<WLegs>();

			ConfigureHead<MelanthaHead>();
			ConfigureBody<MelanthaBody>();
			ConfigureLegs<MelanthaLegs>();

			ConfigureHead<UtageHead>();
			ConfigureBody<UtageBody>();
			ConfigureLegs<UtageLegs>();
		}

		private static void ConfigureHead<T>() where T : ModItem {
			int slot = EquipLoader.GetEquipSlot(ModContent.GetInstance<T>().Mod, typeof(T).Name, EquipType.Head);
			if (slot >= 0)
				ArmorIDs.Head.Sets.DrawHead[slot] = false;
		}

		private static void ConfigureBody<T>() where T : ModItem {
			int slot = EquipLoader.GetEquipSlot(ModContent.GetInstance<T>().Mod, typeof(T).Name, EquipType.Body);
			if (slot >= 0) {
				ArmorIDs.Body.Sets.HidesTopSkin[slot] = true;
				ArmorIDs.Body.Sets.HidesArms[slot] = true;
			}
		}

		private static void ConfigureLegs<T>() where T : ModItem {
			int slot = EquipLoader.GetEquipSlot(ModContent.GetInstance<T>().Mod, typeof(T).Name, EquipType.Legs);
			if (slot >= 0)
				ArmorIDs.Legs.Sets.HidesBottomSkin[slot] = true;
		}
	}
}
