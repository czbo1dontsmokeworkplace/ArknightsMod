# 粒子去重检查

本次逐项读取首轮的 46 个类型，并比较贴图文件哈希、实际 Draw/PreDraw、运动、寿命、混合方式与调用参数。46 是首轮整理的类数量，不表示 46 种互不相同的视觉效果。

## 已合并：两种 SchwarzSmear 光痕

`T_Orbit_Mote_Particle` 与 `T_Rift_Mote_Particle` 合并为 `T_Smear_Particle`，当前目录有 **45 个粒子类型**。两者都出自黑的武器，分别用于飞行伴随光痕和裂隙激光的散射光痕；共用同一贴图、加法混合、固定尺寸单层绘制、速度乘阻尼、幂函数淡出。

| 调用 | 高度 | 速度阻尼 | 淡出指数 | 初始角度 |
|---|---:|---:|---:|---|
| SchwarzBoltVisuals.SpawnFlightMotes | 5 | 0.88 | 1.4 | 弹丸方向 |
| SchwarzRiftLaser.SpawnMotes | 9 | 0.90 | 1.5 | 粒子自身速度方向 |

这些差异全部改为构造参数；宽度、颜色、速度、寿命和随机数调用顺序保留。没有按旧名字保留两套分支实现。

## 其他同贴图组

| 类型（省略 T_ 与 _Particle） | 检查结果 |
|---|---|
| Charge_Pulse / Electric_Pulse / Wave | RuptureRing 与 PassengerImpactRing 的 PNG 字节相同。但分别是前移双环、SmoothStep 收缩/扩张并叠加闪光、平方根扩张单环；不能只调整尺寸/颜色/速度替代。 |
| Flame / FireSmoke | 同 ForwardFlame。前者逐帧阻尼并缩小，后者按年龄解析弯曲、上浮、限制行程并变大，且烟雾使用另一混合模式。 |
| HeavySmoke / SoftSmoke | 都注册 Smoke2。前者还随机选择 Smoke1～4、绘制双层偏移阴影和非线性膨胀；后者单层线性膨胀。随机数消耗和绘制层数也不同。 |
| Slag_Spark / Starlight_Shard | 同 MousseLightSplinter。前者速度定向、固定大小、线性淡出、Additive；后者随机自转、缩小、幂函数淡出、AlphaBlend 下零 alpha 发光色。 |

以下 14 类都注册 DefaultParticle，但注册贴图不一定是实际绘制内容：

| 类型 | 实际行为及保留原因 |
|---|---|
| Barrier_Shard | 双层白芯、关联飞行源朝向或自转，同时改变 Color/Opacity/Scale。 |
| Charge_Absorption | 追随充能锚点、失联消散、历史轨迹，水晶模式还画切面。 |
| Charge_Flash | 前移亮核，三层固定比例的拉伸闪光。 |
| Clock_BlastFragment | 解析阻尼位移、行程限制；烟雾与碎片尺寸曲线/混合方式不同。 |
| Clock_HitShard | 定向缓出位移、切面/线段两种形状、附加光晕。 |
| Clock_Ritual | 三种仪式路径和几何图形，包括多段轨迹。 |
| Dial_Point | 跟随钟盘多轨道定位，钟盘整体透明度叠加闪烁。 |
| Ink | 深色双层墨迹、淡入淡出、速度/来源方向绑定、AlphaBlend。 |
| Lightning_Fracture | DefaultParticle 仅用于注册；实际绘制 PassengerFissionArc 分叉几何。 |
| Mech_Radial | 实际绘制运行时生成的 Bloom 蒙版，使用专门的 alpha 淡出处理。 |
| Medical_Light | 由 Kind 选择蝴蝶、针、星点、光条、枪口、花、十字；复合绘制已在同一类中参数化。 |
| Squash | 属于 ModDust；实际使用 Mousse 的 GlowOrb/SolidOrb，随速度压扁，三层绘制并照明。 |
| Streak | 双层同色光条、可变形、重力可选，缩放与颜色独立衰减。多把武器已共用此类。 |
| Time_Transit | 平滑穿越加正弦偏移，按相邻轨迹点计算方向和长度。 |

## 其余 21 类

这些类型没有构成上述相同主贴图组；也检查了裁帧、附加贴图与生命周期。

| 类型 | 主要素材 / 行为 |
|---|---|
| Chrono | ChronoStreak；速度控制尺寸及朝向。 |
| Clock_Mote | TemporalOrb；带脉动半径的轨道。 |
| Electric_Dust | 原版 Dust 图集；原版 Electric 更新与特定着色。 |
| Electric_Link | spark_07；在两端之间逐段生成波动路径。 |
| Ember | muzzle_02；自转并收缩。 |
| Facet | 注册 StarCore；实际绘制几何切面与光线、石块重力变体。 |
| Flame_Animated03 | fire_03；64×128 帧动画，自定义绘制状态。 |
| Flame_Animated28 | fire_28；85×85 九帧动画，固定位置。 |
| Flame_Attack | SurtrAttack_Dust；64×64 十六帧动画、移动与照明。 |
| Flame_Damage | surtrDamage_Dust；静止伤害火焰，分段变色透明度。 |
| Ink_Pulse | slash_04；SmoothStep 消隐、逐帧增大。 |
| Lighting_Bolts | 原版 PrettySparkleParticle；三个调用配置已共享同一原版实现。 |
| Lightning_AfterArc | PassengerArc；随机镜像、双层电弧缩放。 |
| Lightning_Impact | PassengerImpactBurst/Flash/Ring；预闪、爆发、电弧子粒子和照明。 |
| LinearSpark | OperatorVfx/LinearSpark；固定方向、长度缩减。 |
| Music_Note | 四张音符变体；轻微上浮与摇摆。 |
| Orbit_Glyph | ChronoSigilV2；随机裁取符号、轨道扩大。 |
| PlasmaBall | 原 GlowOrbParticle 贴图；双层球形光点、可选重力与混合。 |
| Ready_Ripple | circle_03；二次缓出扩张及自转。 |
| Slag_Drop | FireParticle；横向阻尼、下落、速度定向、缩小。 |
| Temporal_Shard | TemporalStar；随机自转及正弦透明度。 |

结论：存在可参数化的重复，已合并一组；没有发现另一组仅靠尺寸、颜色、速度即可互换的独立类型。这不代表剩余 45 类在更广泛的架构设计下无法共享代码。这里保留了轨迹、裁帧、混合和复合绘制差异，避免通过大量旧类型分支制造名义合并。

验证：共享 Smear 的 Update/Draw 代入两组参数后与旧实现表达式一致；调用处随机数调用序列保持；源码编译通过。游戏内画面尚未实测。
