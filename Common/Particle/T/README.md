# T 武器粒子目录

命名：`T_Xxx_Particle`；多个词可写为 `T_Xxx_Yyy_Particle`。命名空间：`ArknightsMod.Common.Particle.T`。

## 使用与边界

- 43 个既有粒子 / ModDust 实现移入本目录，逐项保留运动、颜色、尺寸、寿命、随机数调用和绘制公式。
- 通用拉伸光粒子统一使用 `T_Streak_Particle`；原 `DefaultParticle` 是兼容入口，共用同一实现。
- `T_Lighting_Bolts_Particle` 直接继承原版 `PrettySparkleParticle`；术师、星极、莫斯提马各自保留原来的对象池、形状及淡入淡出参数。仍交给原版世界粒子系统绘制。
- `T_PlasmaBall_Particle` 移植本地 Calamity 的 GlowOrbParticle，PNG 字节完全一致。通过本模组 ParticleManager 运行，已适配位移更新顺序；未替换既有武器的光点，避免改变画面。原 Important/needed 标志保留，但本模组管理器没有对应的容量淘汰机制。
- ModDust 仍由 Terraria Dust 系统更新；重命名后显式固定旧贴图路径，避免自动寻图改变。
- 共享旧贴图保留原路径，以免影响同模组其他使用者。PlasmaBall 的新贴图随代码放在本目录。
- 武器的发射配置、原版 DustID、带伤害/联网状态的弹幕、整段拖尾/光束/法阵绘制器仍留在各自调用位置。目录统一的是粒子实现，不改变这些效果的触发和绘制时机。

## 分类

| 家族 | 类型关键词 |
|---|---|
| 光点与光芒 | PlasmaBall、Streak、Lighting_Bolts、Medical_Light、Starlight_Shard |
| 电能 | Electric_Link、Electric_Pulse、Electric_Dust、Lightning_Fracture、Lightning_AfterArc、Lightning_Impact |
| 蓄力与波纹 | Charge_Absorption、Charge_Pulse、Charge_Flash、Ready_Ripple、Wave |
| 火焰与烟尘 | Flame、FireSmoke、Ember、Slag_Spark、Slag_Drop、SoftSmoke、HeavySmoke |
| 时钟与轨道 | Clock_*、Chrono、Temporal_Shard、Orbit_*、Time_Transit、Dial_Point |
| 材质与装饰 | Facet、Squash、Ink、Ink_Pulse、Barrier_Shard、Smear、Mech_Radial、Music_Note、LinearSpark |

同家族中运动/衰减/混合方式不同的类型保留独立实现。共用外观不等于行为相同，不能通过强行合并改变原有观感。

## 旧名对照

| 原名称 | 新名称 | 原位置（迁移前） |
|---|---|---|
| `ShiningBarrierShardParticle` | [T_Barrier_Shard_Particle](T_Barrier_Shard_Particle.cs) | `Content/Projectiles/Medic/Shining/ShiningProjectiles.cs` |
| `SpreadAbsorptionParticle` | [T_Charge_Absorption_Particle](T_Charge_Absorption_Particle.cs) | `Common/VisualEffects/SpreadChargeVfx.cs` |
| `SpreadChargeFlash` | [T_Charge_Flash_Particle](T_Charge_Flash_Particle.cs) | `Common/VisualEffects/SpreadChargeVfx.cs` |
| `SpreadChargePulse` | [T_Charge_Pulse_Particle](T_Charge_Pulse_Particle.cs) | `Common/VisualEffects/SpreadChargeVfx.cs` |
| `MostimaChronoParticle` | [T_Chrono_Particle](T_Chrono_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaVisuals.cs` |
| `MostimaBlastFragment` | [T_Clock_BlastFragment_Particle](T_Clock_BlastFragment_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaClockExplosion.cs` |
| `MostimaHitShard` | [T_Clock_HitShard_Particle](T_Clock_HitShard_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaClockHit.cs` |
| `MostimaClockMote` | [T_Clock_Mote_Particle](T_Clock_Mote_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaVisuals.cs` |
| `MostimaRitualParticle` | [T_Clock_Ritual_Particle](T_Clock_Ritual_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaGrandClockVfx.cs` |
| `MostimaDialPoint` | [T_Dial_Point_Particle](T_Dial_Point_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaClockwork.cs` |
| `PassengerElectricDust` | [T_Electric_Dust_Particle](T_Electric_Dust_Particle.cs) | `Content/Projectiles/Caster/Passenger/PassengerElectricDust.cs` |
| `PuddingLinkParticle` | [T_Electric_Link_Particle](T_Electric_Link_Particle.cs) | `Content/Projectiles/Caster/Pudding/PuddingVisuals.cs` |
| `PuddingPulseParticle` | [T_Electric_Pulse_Particle](T_Electric_Pulse_Particle.cs) | `Content/Projectiles/Caster/Pudding/PuddingVisuals.cs` |
| `MeteoriteEmber` | [T_Ember_Particle](T_Ember_Particle.cs) | `Content/Projectiles/Sniper/Meteorite/MeteoriteShellVisuals.cs` |
| `SpreadFacetParticle` | [T_Facet_Particle](T_Facet_Particle.cs) | `Common/VisualEffects/SpreadVfxDetail.cs` |
| `SpreadFireParticle` | [T_FireSmoke_Particle](T_FireSmoke_Particle.cs) | `Common/VisualEffects/SpreadVfxDetail.cs` |
| `fire_03` | [T_Flame_Animated03_Particle](T_Flame_Animated03_Particle.cs) | `Content/Dusts/Fire/fire_03.cs` |
| `fire_28` | [T_Flame_Animated28_Particle](T_Flame_Animated28_Particle.cs) | `Content/Dusts/Fire/fire_28.cs` |
| `SurtrAttack_Dust` | [T_Flame_Attack_Particle](T_Flame_Attack_Particle.cs) | `Content/Dusts/SurtrAttack_Dust.cs` |
| `surtrDamage_Dust` | [T_Flame_Damage_Particle](T_Flame_Damage_Particle.cs) | `Content/Dusts/surtrDamage_Dust.cs` |
| `OperatorFlameParticle` | [T_Flame_Particle](T_Flame_Particle.cs) | `Common/VisualEffects/OperatorFlightVfx.cs` |
| `MagallanHeavySmokeParticle` | [T_HeavySmoke_Particle](T_HeavySmoke_Particle.cs) | `Content/Projectiles/Supporter/Magallan/MagallanVisuals.cs` |
| `ShiningInkParticle` | [T_Ink_Particle](T_Ink_Particle.cs) | `Content/Projectiles/Medic/Shining/ShiningProjectiles.cs` |
| `ShiningInkPulseParticle` | [T_Ink_Pulse_Particle](T_Ink_Pulse_Particle.cs) | `Content/Projectiles/Medic/Shining/ShiningProjectiles.cs` |
| `PassengerAfterArcParticle` | [T_Lightning_AfterArc_Particle](T_Lightning_AfterArc_Particle.cs) | `Content/Projectiles/Caster/Passenger/PassengerLightningVisuals.cs` |
| `PassengerFractureParticle` | [T_Lightning_Fracture_Particle](T_Lightning_Fracture_Particle.cs) | `Content/Projectiles/Caster/Passenger/PassengerLightningVisuals.cs` |
| `PassengerLightningImpactParticle` | [T_Lightning_Impact_Particle](T_Lightning_Impact_Particle.cs) | `Content/Projectiles/Caster/Passenger/PassengerLightningImpactParticle.cs` |
| `OperatorLinearSpark` | [T_LinearSpark_Particle](T_LinearSpark_Particle.cs) | `Common/VisualEffects/OperatorFlightVfx.cs` |
| `MechRadialParticle` | [T_Mech_Radial_Particle](T_Mech_Radial_Particle.cs) | `Content/Projectiles/Caster/MechAccord/MechAccordVfx.cs` |
| `SussurroLightParticle` | [T_Medical_Light_Particle](T_Medical_Light_Particle.cs) | `Content/Projectiles/Medic/Sussurro/SussurroVisuals.cs` |
| `HarmonieNoteParticle` | [T_Music_Note_Particle](T_Music_Note_Particle.cs) | `Content/Projectiles/Caster/Harmonie/HarmonieVisuals.cs` |
| `MostimaOrbitGlyph` | [T_Orbit_Glyph_Particle](T_Orbit_Glyph_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaTemporalParticles.cs` |
| `SchwarzOrbitMote` | [T_Smear_Particle](T_Smear_Particle.cs) | `Content/Projectiles/Sniper/Schwarz/SchwarzBoltVisuals.cs` |
| `ChargeReadyRipple` | [T_Ready_Ripple_Particle](T_Ready_Ripple_Particle.cs) | `Common/VisualEffects/ChargeCastFeedback.cs` |
| `SchwarzRiftMote` | [T_Smear_Particle](T_Smear_Particle.cs) | `Content/Projectiles/Sniper/Schwarz/SchwarzRiftLaser.cs` |
| `LaevatainSlagDrop` | [T_Slag_Drop_Particle](T_Slag_Drop_Particle.cs) | `Content/Projectiles/Guard/Laevatain/LaevatainSlagfire.cs` |
| `LaevatainSlagSpark` | [T_Slag_Spark_Particle](T_Slag_Spark_Particle.cs) | `Content/Projectiles/Guard/Laevatain/LaevatainSlagfire.cs` |
| `OperatorSmokeParticle` | [T_SoftSmoke_Particle](T_SoftSmoke_Particle.cs) | `Common/VisualEffects/OperatorTextureEffects.cs` |
| `ArtsFighterSquashDust` | [T_Squash_Particle](T_Squash_Particle.cs) | `Content/Dusts/ArtsFighterSquashDust.cs` |
| `AstraLightShard` | [T_Starlight_Shard_Particle](T_Starlight_Shard_Particle.cs) | `Content/Projectiles/Guard/Mousse/MousseVisuals.cs` |
| `MostimaTemporalShard` | [T_Temporal_Shard_Particle](T_Temporal_Shard_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaTemporalParticles.cs` |
| `MostimaTimeTransit` | [T_Time_Transit_Particle](T_Time_Transit_Particle.cs) | `Content/Projectiles/Caster/Mostima/MostimaGrandClockVfx.cs` |
| `OperatorWaveParticle` | [T_Wave_Particle](T_Wave_Particle.cs) | `Common/VisualEffects/OperatorFlightVfx.cs` |
| `DefaultParticle` | [T_Streak_Particle](T_Streak_Particle.cs) | `Common/Particle/DefaultParticle.cs`（保留兼容入口） |
| 原版 `PrettySparkleParticle` | [T_Lighting_Bolts_Particle](T_Lighting_Bolts_Particle.cs) | 三处原版对象池调用 |
| Calamity `GlowOrbParticle` | [T_PlasmaBall_Particle](T_PlasmaBall_Particle.cs) | 连同原始 PNG 移植 |

## 参数化去重

已复查最初的 46 个类型，Orbit_Mote 与 Rift_Mote 合并为 `T_Smear_Particle`，当前为 **45 个类型**。高度、阻尼、淡出指数和初始角度由调用处提供；颜色、速度、尺寸及随机数调用保持原值。详见 [去重检查](DEDUPLICATION.md)。

## 首轮迁移检查记录（合并前）

- 43 个迁移类型在归一化名称、基类全名及隐式贴图路径后，与迁移前代码逐 token 一致。
- 所有既有调用文件在剔除搬移声明、还原类型名后，与迁移前代码逐 token 一致。
- Streak 行为与原 DefaultParticle 逐 token 一致。
- PlasmaBall 贴图 SHA256：`a9af785cd24b23a71c0873a822b9a5a89ce2ad6e8a9a30457e11a6e946d61d7a`。
- 源码编译通过；未进行游戏内画面、多人或性能验收。
