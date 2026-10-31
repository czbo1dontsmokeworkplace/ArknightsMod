# 医疗武器逐把定制（2026-10-23）

保留既有治疗量、技力、技能效果、配方、10帧治疗生效延迟和耐药机制。疗养师首次下落后锁存追踪；群愈治疗场、链愈再生叠层和行医净化仍由服务端结算。

本轮推翻写实插画图集、低像素灰色物体图集及23把武器的线框弹体。仅图耶、调香师、微风保留已认可的主体与反馈，治疗武器线条方案3/24（12.5%）；连同两把输出武器为3/26（11.54%）。其余采用独立灰度发光遮罩、尖头实心拖尾、Dust和有独立生命周期的自定义材质粒子，颜色仍沿用各干员主题色。

| 武器 | 弹道与外形 |
| --- | --- |
| 安赛尔（AnselStaff） | 使用用户提供的AnselHealingArt贴图，飞行持续自转；粉色归元漩涡shader包裹，保留粒子尾迹与寻伤员弹道 |
| 末药（MyrrhStaff） | 原药瓶贴图持续自转，琥珀色归元漩涡shader包裹，保留短残影、粒子尾迹和一次地形反弹 |
| 苏苏洛（SussurroStaff） | 射出高速三联针流，短程飞行后转向伤员 |
| 赫默（SilenceStaff） | 原医疗药剂贴图持续自转，橙色归元漩涡shader包裹；短暂飞行后转向伤员 |
| 图耶（TuyeStaff） | 发射六边形储液盾，减速蓄压后转向伤员 |
| 闪灵（ShiningStaff） | 挥出黑纱薄光包裹的银白尖芒，加速飞行后转向伤员 |
| 清流（PurestreamStaff） | 送出流动水光，下落后沿涟漪曲线追踪伤员 |
| 锡兰（CeylonStaff） | 展开扇状水光，下落后旋弯追踪伤员 |
| 絮雨（WhisperainStaff） | 送出雨幕辉光，缓缓下落并散落细小光滴 |
| 流明（LumenStaff） | 原蓝白鱼形贴图、窄包边、短贴图残影和点状/火花粒子尾迹，下落后快速追踪伤员 |
| 调香师（PerfumerStaff） | 抛出香水瓶，落点展开六瓣花香治疗场 |
| 微风（BreezeStaff） | 送出旋转风轮，弧线落地后展开旋风治疗场 |
| 白面鸮（PtilopsisStaff） | 发射银蓝数据光束，减速落地后展开碎光治疗场 |
| 夜莺（NightingaleStaff） | 放出柔羽光幕，飘落后展开柔光庇护治疗场 |
| 莎草（PapyrusStaff） | 送出金色穗状光流，滑翔转弯并留下再生微光 |
| 明椒（PaprikaStaff） | 送出粉红螺旋火光，弯曲追踪并留下再生微光 |
| Mon3tr（Mon3trCore） | 手持原32×18手枪贴图发射小型实体治疗弹，高速转向伤员；手枪本身不再作为飞行弹体 |
| 褐果（ChestnutStaff） | 送出旋转叶状光斑，以螺旋轨迹输送净化治疗 |
| 蜜莓（HoneyberryStaff） | 送出双核莓色微光，摇曳转向并提供净化治疗 |
| 桑葚（MulberryStaff） | 送出蓝色净化绸光，分段修正方向追踪伤员 |
| 纯烬艾雅法拉（EyjafjallaAlterStaff） | 放出漂浮的粉色泡沫辉光，缓慢转向伤员并提供净化治疗 |
| 华法琳（WarfarinStaff） | 送出猩红血雾旋流，蜿蜒追踪伤员并输送治疗 |
| 芙蓉（HibiscusStaff） | 放出五瓣星状生命光，沿轻柔曲线转向伤员 |
| 诺威尔（NowellStaff） | 送出双重水光，下落后校正方向追踪伤员 |
| 焰影苇草 | 金橙火焰扫光、尖头火流和余烬；原二技能环绕/追踪/归位及三技能分叉、灼痕保留 |
| 可露希尔 | 紫红能量尖芯、晶状碎光和离散轨迹；原伤害、费用与三技能激光保留 |

## 素材与绘制

2026-10-29补充：末药、赫默、流明改用 `MedicSpriteFlightVisuals`，参考本地灾厄 `SupernovaBomb.PreDraw` 的原贴图错位包边/短残影，以及飞行阶段的独立点状和火花粒子；不使用发光遮罩主体、光带shader或宽幅光晕。`Mon3trHealingArt.png` 虽沿用历史文件名，其内容是手枪，只由物品/手持端使用；飞行弹体使用原版小型子弹贴图。治疗数值、追踪、耐药、再生、技能与配方均保留。

同日后续修正：安赛尔换用用户提供的弹幕PNG；安赛尔、末药、赫默依据真实帧龄以0.22弧度/帧持续自转，和飞行方向解耦；流明仍朝向飞行方向。前三名新增 `MedicVortexVisuals`，直接复用灾厄归元漩涡小漩涡的原始 `ExoVortexShader`、`BlobbyNoise` 和双向旋转构成，各自使用 `MedicProfile.Body` 的固定主体RGB，实体贴图保持可见。此为上段“不使用shader”方案的明确后续调整；仍不移植归元漩涡的逻辑、伤害和爆炸。素材归属及来源见 `Content/Textures/MedicVfx/ExoVortexCredits.md`。

`Content/Textures/MedicVfx/MedicalMaterials.png` 是1024×768原创灰度发光遮罩图集，每格256×256。以灰度控制亮度并支持柔边渐隐，不把物体本身画成灰色，也不进行像素量化。采用白色亮芯、灰度过渡与透明背景；使用时按主题色叠加。图集由内置 imagegen 根据用户给出的粒子参考生成，提示词记录于 `Doc/Validation/MedicalMaterial/AssetPrompt.txt`。

本地参考：CalamityMod/Particles 的 VerticalSmear、PulseStar、PearlParticleGlow、FadeStreak、TimedSmokeParticle、BloomParticle，以及 Projectiles/Magic/PhantasmalFuryProj、LightBlade、ClimaxProj 的材质、残影、粒子运动组织；无灾厄运行时依赖，未复制其绘制代码或shader。

`MedicFlow.fx` 为独立编写的实心、尖头、真实历史路径拖尾；绘制后恢复普通SpriteBatch的顶点/像素状态。`MedicIdentity.fx` 仅留三个已认可设计及调香师治疗场的花瓣。

验证与新包在 `bin/MedicalMaterial`，验证代码在 `Doc/Validation/MedicalMaterial` 及 `Doc/Validation/MedicalArts/MedicalVerification`。离线预览不等于游戏内动态及多人联机验收；本轮按既有要求暂不安装。

同日最终纠正：上述漩涡主体方案已撤销，安赛尔、末药、赫默只接入归元小漩涡的 SideStreakTrail 拖尾shader，沿真实历史路径绘制并染为各自主体色。ExoVortex主体shader及BlobbyNoise资源已移出生产目录；三人自转和流明朝向保持。
