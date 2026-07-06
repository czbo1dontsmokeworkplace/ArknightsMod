#if DEBUG
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ArknightsMod.Common.Configs
{
	public class DebugConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Label("[Debug] NeoArmor 获得时自动升级")]
		[Tooltip("开启后，所有 NeoArmor 物品在合成/获取时默认变为升级盔甲状态\n背包中已有的未升级 NeoArmor 也会自动升级")]
		[DefaultValue(false)]
		public bool AutoUpgradeNeoArmor { get; set; }
	}
}
#endif
