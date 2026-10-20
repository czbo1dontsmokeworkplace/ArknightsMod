# 育生弹幕与坠生开场（2026-10-19）

**此为同日早先的设计记录。当前激光、小怪吐弹与第三阶段规则以 [EvolutionCombatRevision.md](EvolutionCombatRevision.md) 为准；尤其傀儡已改成纯Dust吐弹，第三阶段也已扩展。**

## 参考保留

用户指定：轰击术师的“蚀清 → 伊芙利特 → 谬因”共享喷焰特效必须使用；Scorpio、Sulphuric Acid Cannon火箭、Dragoon Drizzlefish重力火球、Vitriolic Viper蜿蜒辐射流也作为后续素材与机制参考。

本次核对本地 `../CalamityMod` 和旧版 `../CalamityModPublic2.0.3.004`。版本间火焰实现不同，当前 `EssenceFire` 采用旋转FireProj与MediumMist叠层，`MeowFire` 采用HeavySmoke动画，`BrimstoneFireFriendly` 采用metaball；旧版MeowFire主要采用Dust。没有把这些不同版本说成完全相同的绘制代码。实现保留翻卷烟焰、亮芯叠层与扩散消散，并用于实际喷焰、俯冲尾焰和火箭推进尾焰。

| 参考 | 实际采用 | 文件 |
| --- | --- | --- |
| EssenceFire / MeowFire | 旋转火焰、三帧烟雾、七列六行HeavySmoke动画；赤红外焰与暖亮芯 | EvolutionArsenalVisuals.cs |
| ScorpioRocket | 先出膛等待、再加速推进，方向与弹体一致 | EvolutionHazard.BroodArsenal.cs |
| AcidRocket / SulphuricAcidCannonExplosion | 烟雾推进、停驻蓄爆、脉冲爆焰；停驻后30帧范围预警 | 同上 |
| DrizzlefishFireball | 横向阻力、重力抛物线、亮核和残影；不用原贴图 | 同上；Assets/EvolutionGravityFlare.png |
| VitriolicViperSpit | 沿主方向正弦蜿蜒、发光火花组成的短流；实际碰撞逐段沿可见曲线 | 同上 |

`ArsenalFlame.png`、`ArsenalSmoke.png`、`ArsenalMist.png`、`ArsenalSpark.png`、`ArsenalBurst.png` 分别来自本地灾厄的 `Projectiles/FireProj.png`、`Particles/HeavySmoke.png`、`Particles/MediumMist.png`、`Particles/GlowSpark.png`、`Particles/FlameExplosion.png`，作为用户允许复用的基础特效材质。绘制组合与参考代码来源署名：**Azafure, LLC**；[Calamity Mod source](https://github.com/CalamityTeam/CalamityModPublic)。未引入灾厄运行时依赖，未使用其火球或火箭实体贴图。

## 原创火球

`Assets/EvolutionGravityFlare.png` 使用内置imagegen新建，保留生成透明通道，运行时染成赤红。没有使用DrizzlefishFire或DrizzlefishFire2。

生成提示词：

> Single original transparent game projectile sprite for a biological crimson boss; neutral grayscale tint mask. Compact round white-hot embryonic nucleus, three asymmetrical curling gray flame tongues, short split wispy wake pointing left, bright rounded front on right. Fantasy cartoon painted sprite, readable at 40 pixels, clean silhouette, cel-like shading, transparent padding. No ground, text, border or checkerboard; do not copy any existing game's fireball.

## 战斗安排

80%转场仍保留。转场结束后，在原80%～40%可受伤战斗之前额外插入720帧（12秒）坠生开场；期间本体无敌，所以不能用高伤害跳过，也不会消耗原血量区间。原六招循环的次序保留，在末尾追加两翼喷焰和火箭／辐射流两招。第三阶段不变。

- 0～60帧移动到斜上方；60帧锁定俯冲折线、玩家位置和地面落点，之后不重瞄。
- 120～156帧沿预警俯冲；只有实际受击玩家才可能被裹挟，一次最多一人，最多24帧。没有命中也照常落地。裹挟与落地不追加伤害。
- 撞地后60帧撤离；捕获者释放时获得60帧保护，位置和拖行路径都检查实体砖。死者、远端玩家、其他战斗的玩家不被带走。
- 216、348帧各放一轮六束上向扇形光束，先预警64帧，伤害18帧，淡出20帧。近端75像素不伤人，中央44度通路保持开放。显示宽度覆盖碰撞宽度。
- 480～534帧每18帧孵化一只，共岩蛛、巨岩蛛、傀儡、赘生物四只，沿用总上限14与单种上限。孵化在第二轮光束结束后。
- 600帧抛出两枚侧翼重力焰核；720帧结束开场，原循环从第0招开始，保留孵化小怪。
- 傀儡二阶段改为两枚带轨迹预警的重力焰核；第一阶段仍是血液。预警和实际射弹共用侧向发射口与弹道公式。
- 岩蛛冲刺朝锁定方向，巨岩蛛起跳锁向；空中朝移动方向，傀儡瞄准时朝发射方向。爬墙将水平翻转与旋转配对，脚朝墙、头朝上，按竖向速度播放动画。跳跃落地、冲刺扬尘和爬行附着点添加少量烟粒。

伤害仍集中在 `EvolutionDamageCockpit.cs`。新弹幕沿用Encounter/Serial归属、180个伤害实体上限、阶段切换和离场清理。裹挟状态加入ExtraAI；多人客户端的受击捕获请求由服务端核对玩家、Boss归属、俯冲时段和距离。

## 验证范围

源码编译、StageReworkChecks的14,548项隔离断言、实际PreDraw的FNA离屏绘制已通过；隔离tModLoader打包和专服内容加载也通过（到Choose World，未创建或加载世界）。断言覆盖开场血量锁、原招序、捕获／释放／砖墙阻挡、站地玩家的实际地面落点、扇形伤害窗口与中央间隙、抛物线终点、曲线碰撞、左右墙姿态与状态序列化。

打包出现的 `Image loading failed: unknown image type` 定位为原有 `icon_small.png` 未转换为rawimg；新增六张素材均已转换并进入包内。未扩大范围修改图标。

离屏预览与自动检查不等同于整场实机手感、声音混合、复杂地形和双客户端延迟验收。实际游玩安装包未覆盖；隔离包位于 `Doc/Validation/Evolution/BroodfallSandbox/Mods/ArknightsMod.tmod`。
