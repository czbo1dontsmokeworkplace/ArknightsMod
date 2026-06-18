using System.ComponentModel;
using Terraria.ModLoader.Config;

// 武器数值全局覆写开关
// 开启后，WeaponStatOverride 中定义的数值将覆盖各武器自身的 SetDefaults 值
// 关闭时，各武器按照自身文件中的原始数值生效

namespace ArknightsMod.Common.Configs
{
	public class WeaponStatOverrideConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		// 在游戏内 Mod 配置界面中显示的开关
		[Label("启用统一武器数值覆写")]
		[Tooltip("开启后，所有武器的数值将由 Common/WeaponStatOverride.cs 统一控制。\n关闭则使用各武器原始数值。")]
		[DefaultValue(true)]
		public bool UseGlobalWeaponStats { get; set; }
	}
}