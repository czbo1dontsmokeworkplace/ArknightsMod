using System;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

/// <summary>进化的本质——伤害驾驶舱。调整伤害只改下面各行最后的整数。</summary>
internal static class EvolutionDamageCockpit
{
    internal const int CrashContactDamage = 0; // 走廊下砸无接触伤害，不裹挟玩家；伤害来自之后的激光。
    // 【使用说明】
    // 1. 每行依次为：代码标识、中文名、英文名、伤害。只调整最后的整数即可。
    // 2. 同种弹幕只有一行；本体、仆从、炸弹分裂、所有阶段和所有招式共用此值。
    //    唯一例外：最终十六秒三叉光轴在特殊表独立配置，不影响普通激光或侧射能量刃。
    // 3. 这里是传入游戏的基础伤害，不是保证玩家最终扣除的生命值。
    //    原版难度规则、玩家防御/减伤、其他模组的伤害钩子仍可能影响实际扣血。
    // 4. 修改源码后需重新编译并重新加载模组；不是游戏中的实时设置界面。
    // 5. 不改变伤害判定时间、碰撞范围或预警机制；纯提示弹幕仍不能造成伤害。

    // ==================== 弹幕伤害 ====================
    // 以用户给出的大师世纪之花面板为准：轻弹114～180、重力焰核约185、重弹210；光轴600。
    // 下表存引擎基础值，大师标称伤害为整数×6（防御/伤害浮动前），因此185近似为186。
    // 大师走廊阶段不额外乘0.85，确保整场处于同一伤害档；普通/专家保留0.85。
    // 激怒普通弹保留×1.3；光轴独立读取，不再乘额外阶段/激怒伤害倍率。
    internal static readonly (EvolutionShot Kind, string ChineseName, string EnglishName, int Damage)[] ProjectileDamages =
    {
        (EvolutionShot.Bloodglow,   "赤色追踪血光", "Homing Bloodglow",    26), // 大师标称156；激怒沿用普通弹幕倍率。
        (EvolutionShot.Blood,       "血液",       "Blood Droplet",       24), // 重力抛射水滴，包括仆从吐出的血液、天降血雨。
        (EvolutionShot.Spirit,      "血灵",       "Blood Spirit",        25), // 阵列焰核与核心分裂的轻追踪焰核共用。
        (EvolutionShot.Beam,        "血色激光",   "Blood Laser",         27), // 横/竖/斜激光墙、十字、扇形及仆从射线共用。
        (EvolutionShot.Rock,        "血岩",       "Blood Boulder",       35), // 抛射、空中落石、落地滚动、平台滚石共用。
        (EvolutionShot.Tentacle,    "血色触手",   "Blood Tentacle",      30), // 触手伸出后的攻击段。
        (EvolutionShot.Pulse,       "血色脉冲环", "Blood Pulse Ring",    27), // 仆从爆发的扩散圆环。
        (EvolutionShot.Core,        "育生核心",   "Brood Core",           0), // 不直接伤人；分裂的血灵读取 Blood Spirit 一行。
        (EvolutionShot.Spike,       "血色尖刺",   "Blood Spike",         27), // 浮空炸弹释放的定向长尖刺。
        (EvolutionShot.Fragment,    "血色碎片",   "Blood Fragment",      22), // 炸弹分裂的小型弹幕。
        (EvolutionShot.DashMarker,  "冲刺预瞄",   "Dash Telegraph",       0), // 纯提示，无伤害；不要通过此项尝试启用伤害。
        (EvolutionShot.Lance,       "血喷",       "Blood Lance",         30), // 高速移动尖梭，包括横排/竖排血喷、光轴侧刃与仆从齐射。
        (EvolutionShot.CrimsonBomb, "深红炸弹",   "Crimson Bomb",        35), // 仅范围爆炸型使用此伤害；分裂型只由碎片伤人。
        (EvolutionShot.Eruption,    "血色喷柱",   "Blood Eruption",      30), // 从地面/平台向上喷发的柱体。
        (EvolutionShot.Radiation,   "育生闪耀激光", "Brood Flash Laser",     27), // 第一阶段五向瞬发激光；预警无伤害，亮起后8帧生效。
        (EvolutionShot.Reflection,  "反射血刃",   "Reflected Blood Blade", 30), // 保留旧弹幕类型兼容；光轴护盾已改为虚化，不再吞弹或反击。
        (EvolutionShot.CrashFan,    "坠生扇形光束", "Broodfall Fan",        27),
        (EvolutionShot.Flame,       "育生喷焰",     "Brood Flame",          19),
        (EvolutionShot.BroodRocket, "育生火箭",     "Brood Rocket",         35),
        (EvolutionShot.GravityFlare,"重力焰核",     "Gravity Flare",        31),
        (EvolutionShot.Serpent,     "蜿蜒辐射流",   "Serpentine Radiation", 28),
        (EvolutionShot.BroodSpit,   "育生尘焰",     "Brood Dust Spit",      23),
        (EvolutionShot.SpinBomb,    "旋舞育生炸弹", "Whirling Brood Bomb",  35),
        (EvolutionShot.Barrage,     "育生弹幕",     "Brood Barrage",        24),
        (EvolutionShot.Ricochet,    "走廊弹球",     "Corridor Ricochet",    27),
        (EvolutionShot.ScoreSeed,   "赤色曲籽",     "Scarlet Score Seed",   25),
    };

    // 光轴独立配置：大师标称600；提示弹幕仍为0。
    // 基础100 → 普通200、专家400、大师600；不绕过防御、减伤或普通无敌。
    internal static readonly (EvolutionShot Kind, EvolutionBeamStyle Style, string ChineseName, string EnglishName, int Damage)[] SpecialProjectileDamages =
    {
        (EvolutionShot.Beam, EvolutionBeamStyle.Axis, "终末血色光轴", "Final Hemal Axis", 100),
    };

    // ==================== 本体分阶段碰撞伤害 ====================
    // 阶段编号对应内部状态；7 是驾驶舱专用编号，表示完美形态最后 8% 血量。
    // 保持现有规则：本体只在冲刺攻击窗口能碰撞伤人，悬浮/蓄力/转场不伤人。
    // 初生形态没有冲刺；把它的 0 改大不会凭空增加冲刺或开启静止接触伤害。
    internal static readonly (int Stage, string ChineseName, string EnglishName, int Damage)[] BossContactDamages =
    {
        (0, "初始化",             "Initialization",      0),
        (1, "初生形态：100%～80%", "Newborn",             0),
        (2, "第一次无敌转场",     "First Transition",    0),
        (3, "进化形态：80%～40%",  "Evolved",           155),
        (4, "第二次无敌转场",     "Second Transition",   0),
        (5, "完美形态：40%～8%",   "Perfect",           170),
        (6, "死亡演出",           "Death Animation",     0),
        (7, "最后狂暴：8%以下",    "Desperate",         180),
    };

    // ==================== 仆从碰撞伤害 ====================
    // 第一阶段地面仆从的接触伤害；不改变后续无敌光球的零接触伤害规则。
    // 第二阶段为可击杀地面小怪；生命上限在生成时乘1.3，接触伤害在AI中乘1.1。
    // 第三阶段独立辅助光球不碰撞伤人。
    internal static readonly (EvolutionBrood Kind, string ChineseName, string EnglishName, int Damage)[] BroodContactDamages =
    {
        (EvolutionBrood.Spider,      "变异岩蛛",   "Mutant Rock Spider",       55),
        (EvolutionBrood.GiantSpider, "变异巨岩蛛", "Mutant Giant Rock Spider", 60),
        (EvolutionBrood.Puppet,      "畸变体傀儡", "Abomination Puppet",       50),
        (EvolutionBrood.Abomination, "源石畸变体", "Originium Abomination",    65),
        (EvolutionBrood.Tumor,       "恶性瘤",     "Tumor",                    35),
        (EvolutionBrood.Bomb,        "畸变赘生物", "Aberrant Excrescence",      38),
    };

    // 以下是统一读取接口，通常无需修改；查不到类型时明确报错，避免漏配后悄悄使用其他伤害。
    internal static int ProjectileDamage(EvolutionShot kind, EvolutionBeamStyle style = EvolutionBeamStyle.Standard)
    {
        foreach (var row in SpecialProjectileDamages)
            if (row.Kind == kind && row.Style == style) return Math.Max(0, row.Damage);
        foreach (var row in ProjectileDamages)
            if (row.Kind == kind) return Math.Max(0, row.Damage);
        throw new ArgumentOutOfRangeException(nameof(kind), kind, "驾驶舱缺少此弹幕的伤害配置");
    }

    // 所有实际发射都走这里，避免不同阶段和仆从继承旧倍率。
    internal static int SpawnDamage(EvolutionShot kind, EvolutionBeamStyle style, bool corridor, float cycleBoost, bool master)
    {
        int damage = ProjectileDamage(kind, style);
        if (kind == EvolutionShot.Beam && style == EvolutionBeamStyle.Axis) return damage;
        if (corridor && !master) damage = (int)(damage * EvolutionRules.EvolvedProjectileMultiplier);
        return (int)(damage * cycleBoost);
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
