using System;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

/// <summary>进化的本质——伤害驾驶舱。调整伤害只改下面各行最后的整数。</summary>
internal static class EvolutionDamageCockpit
{
    // 【使用说明】
    // 1. 每行依次为：代码标识、中文名、英文名、伤害。只调整最后的整数即可。
    // 2. 同种弹幕只有一行；本体、仆从、炸弹分裂、所有阶段和所有招式共用此值。
    // 3. 这里是传入游戏的基础伤害，不是保证玩家最终扣除的生命值。
    //    原版难度规则、玩家防御/减伤、其他模组的伤害钩子仍可能影响实际扣血。
    // 4. 修改源码后需重新编译并重新加载模组；不是游戏中的实时设置界面。
    // 5. 不改变伤害判定时间、碰撞范围或预警机制；纯提示弹幕仍不能造成伤害。

    // ==================== 弹幕伤害 ====================
    // 对标本地炼狱四场战斗：普通弹幕靠近克隆体/世纪之花，重型攻击不照搬石巨人重炮。
    // 本表直接传给 Projectile.NewProjectile；专家模式下基础威力约为此值 ×4（防御前）。
    // 炼狱 NewProjectileBetter 会先除以4，切勿把其130～260直接填入本表！
    // 同种弹幕全阶段固定；普通/大师难度仍保留原有游戏难度倍率。
    internal static readonly (EvolutionShot Kind, string ChineseName, string EnglishName, int Damage)[] ProjectileDamages =
    {
        (EvolutionShot.Blood,       "血液",       "Blood Droplet",       35), // 重力抛射水滴，包括仆从吐出的血液、天降血雨。
        (EvolutionShot.Spirit,      "血灵",       "Blood Spirit",        35), // 追踪焰核；两翼血灵、扇形血灵、核心分裂共用。
        (EvolutionShot.Beam,        "血色激光",   "Blood Laser",         45), // 横/竖/斜激光墙、十字、扇形及仆从射线共用。
        (EvolutionShot.Rock,        "血岩",       "Blood Boulder",       44), // 抛射、空中落石、落地滚动、平台滚石共用。
        (EvolutionShot.Tentacle,    "血色触手",   "Blood Tentacle",      40), // 触手伸出后的攻击段。
        (EvolutionShot.Pulse,       "血色脉冲环", "Blood Pulse Ring",    35), // 仆从爆发的扩散圆环。
        (EvolutionShot.Core,        "育生核心",   "Brood Core",           0), // 不直接伤人；分裂的血灵读取 Blood Spirit 一行。
        (EvolutionShot.Spike,       "血色尖刺",   "Blood Spike",         37), // 浮空炸弹释放的定向长尖刺。
        (EvolutionShot.Fragment,    "血色碎片",   "Blood Fragment",      34), // 炸弹分裂的小型弹幕。
        (EvolutionShot.DashMarker,  "冲刺预瞄",   "Dash Telegraph",       0), // 纯提示，无伤害；不要通过此项尝试启用伤害。
        (EvolutionShot.Lance,       "血喷",       "Blood Lance",         40), // 高速移动尖梭，包括横排/竖排血喷与仆从齐射。
        (EvolutionShot.CrimsonBomb, "深红炸弹",   "Crimson Bomb",        46), // 仅范围爆炸型使用此伤害；分裂型只由碎片伤人。
        (EvolutionShot.Eruption,    "血色喷柱",   "Blood Eruption",      44), // 从地面/平台向上喷发的柱体。
    };

    // ==================== 本体分阶段碰撞伤害 ====================
    // 阶段编号对应内部状态；7 是驾驶舱专用编号，表示完美形态最后 8% 血量。
    // 保持现有规则：本体只在冲刺攻击窗口能碰撞伤人，悬浮/蓄力/转场不伤人。
    // 初生形态没有冲刺；把它的 0 改大不会凭空增加冲刺或开启静止接触伤害。
    internal static readonly (int Stage, string ChineseName, string EnglishName, int Damage)[] BossContactDamages =
    {
        (0, "初始化",             "Initialization",      0),
        (1, "初生形态：100%～60%", "Newborn",             0),
        (2, "第一次无敌转场",     "First Transition",    0),
        (3, "进化形态：60%～20%",  "Evolved",           155),
        (4, "第二次无敌转场",     "Second Transition",   0),
        (5, "完美形态：20%～8%",   "Perfect",           170),
        (6, "死亡演出",           "Death Animation",     0),
        (7, "最后狂暴：8%以下",    "Desperate",         180),
    };

    // ==================== 仆从碰撞伤害 ====================
    // 岩蛛两种冲刺预设共用一项；其余仆从当前没有接触伤害。
    // 第二阶段起的无敌辅助单位仍不碰撞伤人，不因改表而改变这个规则。
    internal static readonly (EvolutionBrood Kind, string ChineseName, string EnglishName, int Damage)[] BroodContactDamages =
    {
        (EvolutionBrood.Spider,      "变异岩蛛",   "Mutant Rock Spider",       55),
        (EvolutionBrood.GiantSpider, "变异巨岩蛛", "Mutant Giant Rock Spider",  0),
        (EvolutionBrood.Puppet,      "畸变体傀儡", "Abomination Puppet",        0),
        (EvolutionBrood.Abomination, "源石畸变体", "Originium Abomination",     0),
        (EvolutionBrood.Tumor,       "恶性瘤",     "Tumor",                     0),
        (EvolutionBrood.Bomb,        "浮空炸弹",   "Floating Bomb",             0),
    };

    // 以下是统一读取接口，通常无需修改；查不到类型时明确报错，避免漏配后悄悄使用其他伤害。
    internal static int ProjectileDamage(EvolutionShot kind)
    {
        foreach (var row in ProjectileDamages)
            if (row.Kind == kind) return Math.Max(0, row.Damage);
        throw new ArgumentOutOfRangeException(nameof(kind), kind, "驾驶舱缺少此弹幕的伤害配置");
    }

    internal static int BossContactDamage(int phase, bool charging, bool desperate = false)
    {
        if (!charging) return 0;
        int stage = phase == 5 && desperate ? 7 : phase;
        foreach (var row in BossContactDamages)
            if (row.Stage == stage) return Math.Max(0, row.Damage);
        throw new ArgumentOutOfRangeException(nameof(phase), phase, "驾驶舱缺少此阶段的碰撞伤害配置");
    }

    internal static int BroodContactDamage(EvolutionBrood kind)
    {
        foreach (var row in BroodContactDamages)
            if (row.Kind == kind) return Math.Max(0, row.Damage);
        throw new ArgumentOutOfRangeException(nameof(kind), kind, "驾驶舱缺少此仆从的碰撞伤害配置");
    }
}
