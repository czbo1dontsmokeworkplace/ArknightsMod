using System.ComponentModel;
using Terraria.ModLoader.Config;

// 全局技力（SP）恢复速度开关。
// 开启后，所有本模组武器的技力条恢复速度（自然回复、受击回复、攻击回复、饰品加速等）
// 整体加快到 2 倍——只影响"回转的快慢"，不影响任何数值本身：技能所需技力、技力上限等
// 数值不变；部署费用吸收、套装/技能直接赠送技力这类"技力返还"效果也不会被翻倍。
// 因此填写 CSV/武器数据时仍按 1 倍速的数值填写，无需为了适配二倍速而手动 /2。

namespace ArknightsMod.Common.Configs
{
	public class SkillChargeConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Label("技力恢复二倍速")]
		[Tooltip("开启后，所有干员武器的技力条恢复速度整体 x2（自然回复/受击回复/攻击回复/饰品加速）。\n" +
			"技力上限、技能消耗等数值不受影响；部署费用吸收、套装直接赠送技力等\"技力返还\"类效果也不会被翻倍。")]
		[DefaultValue(true)]
		public bool DoubleSPRegenSpeed { get; set; }
	}
}
