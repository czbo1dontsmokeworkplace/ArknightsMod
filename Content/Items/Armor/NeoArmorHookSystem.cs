using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	// 配方筛选与材料消耗顺序依赖 tModLoader 新版 On_Recipe / On_CraftingRequests Hook。
	// 当前运行版本尚未提供这些 Hook；时装升级仍由 NeoArmorGItem.OnCreated 处理。
	public class NeoArmorHookSystem : ModSystem
	{
	}
}
