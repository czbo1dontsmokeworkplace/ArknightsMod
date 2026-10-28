# 套装 / 增益弹幕追踪

所有套装、饰品和 Buff 赋予的额外追踪统一在 `EffectHomingGlobalProjectile` 中配置。
武器自身的专属制导、召唤物 AI、治疗弹追踪友方仍由对应弹幕负责。

## 新增效果

在 `EffectHomingGlobalProjectile.Load()` 的 `rules` 数组加入规则即可。例如：

```csharp
new HomingRule(
    p => p.GetModPlayer<YourEffectPlayer>().EffectActive,
    new[] { DamageClass.Ranged, DamageClass.Magic },
    Range: 600f,
    TurnDegreesPerTick: 3f),
```

`IsActive` 判断弹幕属主的当前状态，效果关闭后，已经飞出的弹幕也立即停止获得额外追踪。
`DamageTypes` 任意一项匹配即可，使用 `CountsAsClass`，支持其他模组继承的伤害职业。
不要用 `Main.LocalPlayer` 判断效果，否则多人游戏会读取错误玩家。

| 参数 | 含义 |
| --- | --- |
| `Range` | 从弹幕中心计算的索敌半径，像素 |
| `TurnDegreesPerTick` | 每个游戏帧最多转向多少度，按 `MaxUpdates` 平摊 |
| `Priority` | `Nearest` 最近；`HighestDefense` 防御最高，防御相同时取最近 |
| `TargetFilter` | 可选的 NPC 过滤函数；不符合条件的敌人不会被选择 |
| `IncludeArtsDamage` | 同时允许被本模组标记为法术伤害的弹幕 |
| `RequireLineOfSight` | 默认 true；目标必须与弹幕之间无遮挡 |

多条规则匹配时，按数组顺序使用第一条能找到目标的规则，只执行一次转向。
索敌不命中任何目标时维持弹幕原有行为，不缩短寿命、不销毁弹幕。
转向保留原弹速，不额外修改弹幕的旋转贴图字段。

## 当前规则

| 效果 | 伤害类型 | 范围 | 转向 | 目标 |
| --- | --- | --- | --- | --- |
| 史都华德全套 | 魔法及法术标记 | 600 px | 3°/游戏帧 | 最高防御敌人 |
| 末柠全套 | 魔法 | 600 px | 3°/游戏帧 | 最近敌人 |
| 安德切尔全套 | 远程 | 600 px | 3°/游戏帧 | 最近的可攻击敌对目标 |

只处理玩家拥有的、有伤害的友方飞行弹幕。召唤物本体、哨兵本体、长枪/鞭子 AI、
手持弹幕、不更新位置的弹幕、零速度弹幕不参与。尊重 `ICoreCasterGuidance.GuidanceReady`，
不破坏核心术师的发射阶段和禁用制导规则。

## 参考及验证

参考本机星极模组 `ASTES1A/Content/Projectiles/GlobalProj/ArrowStormTracking.cs` 的
“效果激活 → 伤害职业筛选 → 搜索目标 → 修正速度方向”流程。
统一实现改用明确的角速度上限，保留原弹速，且用弹幕属主判断状态，避免局部玩家依赖。

编译命令：

```powershell
dotnet build ArknightsMod.csproj -p:BuildMod=false -p:TargetFramework=net8.0 --no-restore -v:minimal
```

游戏验收建议：分别穿戴三套装备，比较穿齐/脱下后的弹道；在墙后放目标检查视线；
用有不同 `extraUpdates` 的弹幕比较每游戏帧转向；多人分别穿戴不同套装检查归属；
确认安德切尔对近战及远程敌人均可追踪，并优先选择最近的可攻击敌对目标。
