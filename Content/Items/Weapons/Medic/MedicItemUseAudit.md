# 医疗武器物品贴图与使用动作核对（2026-10-29）

按 `ModItem.Texture` 最终指向的物品贴图判断使用动作，独立核对弹幕贴图。类名中的 `Staff` 不作为分类依据。

## 使用规则

- 法杖：原版 `ItemUseStyleID.Shoot`、`Item.staff[Type] = true`，显示手持物品图。
- 投掷医疗用品：原版手里剑、飞刀的 `Swing` 动作、`noUseGraphic = true`，隐藏挥舞中的物品图，只显示独立生成的医疗弹幕。
- 闪灵的剑：`Swing`，显示剑图；仍只有医疗弹幕产生治疗，不增加近战伤害。
- 枪械：`Shoot`、非 staff，显示手持枪图，发射独立弹幕。

投掷用品仍是可重复使用、消耗魔力的医疗武器，不改成消耗性飞刀。治疗量、技能、配方、向上发射、飞行自转、拖尾和后台自动施放沿用此前实现。

## 逐把核对

下表物品资源相对于 `Content/Items/Weapons/Medic/`；“共用苏苏洛”均实际指向 `Sussurro/SussurroStaff.png`（58×68）。

| 干员 | 实际物品贴图 | 使用动作 / 手持绘制 | 弹幕补充 |
| --- | --- | --- | --- |
| 安赛尔 | `AnselStaff.png`，36×34，医疗用品 | 投掷 Swing / 隐藏 | 专属 `AnselHealingArt.png`，10×18，持续自转 |
| 末药 | `MyrrhStaff.png`，30×28，药草篮 | 投掷 Swing / 隐藏 | 专属 `MyrrhHealingArt.png`，6×20，持续自转 |
| 苏苏洛 | 共用苏苏洛 | 法杖 Shoot / 显示 | 高速针流 |
| 赫默 | 共用苏苏洛 | 法杖 Shoot / 显示 | 专属 `SilenceHealingArt.png` 药瓶持续自转；物品仍是法杖 |
| 图耶 | 共用苏苏洛 | 法杖 Shoot / 显示 | 储液盾术体 |
| 闪灵 | `Shining/ShiningSword.png`，86×86，剑 | 挥剑 Swing / 显示 | 发出治疗尖芒 |
| 清流 | 共用苏苏洛 | 法杖 Shoot / 显示 | 水光术体 |
| 锡兰 | 共用苏苏洛 | 法杖 Shoot / 显示 | 扇状水光 |
| 絮雨 | 共用苏苏洛 | 法杖 Shoot / 显示 | 雨幕辉光 |
| 流明 | `LumenStaff.png`，50×52，长柄法杖 | 法杖 Shoot / 显示 | 专属 `LumenHealingArt.png` 鱼形弹幕朝向飞行方向 |
| 调香师 | 共用苏苏洛 | 法杖 Shoot / 显示 | 香水瓶与治疗场；按物品法杖图分类 |
| 微风 | 共用苏苏洛 | 法杖 Shoot / 显示 | 风轮与治疗场 |
| 白面鸮 | `PtilopsisStaff.png`，54×54，法杖 | 法杖 Shoot / 显示 | 数据光束与治疗场 |
| 夜莺 | 共用苏苏洛 | 法杖 Shoot / 显示 | 羽幕与治疗场 |
| 莎草 | 共用苏苏洛 | 法杖 Shoot / 显示 | 穗状光流 |
| 明椒 | 共用苏苏洛 | 法杖 Shoot / 显示 | 螺旋火光 |
| Mon3tr | `Content/Projectiles/Medic/Arts/Mon3trHealingArt.png`，32×18，手枪 | 持枪 Shoot / 显示 | 飞行端使用原版子弹图 `Terraria/Images/Projectile_1`，不会投出手枪 |
| 褐果 | 共用苏苏洛 | 法杖 Shoot / 显示 | 叶状净化术体 |
| 蜜莓 | 共用苏苏洛 | 法杖 Shoot / 显示 | 双核莓色微光 |
| 桑葚 | 共用苏苏洛 | 法杖 Shoot / 显示 | 净化绸光 |
| 纯烬艾雅法拉 | `EyjafjallaAlterStaff.png`，64×78，法杖 | 法杖 Shoot / 显示 | 泡沫辉光 |
| 华法琳（旧物品兼容） | 共用苏苏洛 | 法杖 Shoot / 显示 | 血雾术体 |
| 芙蓉 | `HibiscusStaff.png`，46×46，木杖 | 法杖 Shoot / 显示 | 五瓣星状术体 |
| 诺威尔 | 共用苏苏洛 | 法杖 Shoot / 显示 | 双重水光 |
| 焰影苇草 | `ReedFlameShadow/ReedFlameShadowStaff.png`，66×66，法杖 | 法杖 Shoot / 显示 | 保留现有攻击与技能；补齐原版 staff 标记 |
| 可露希尔 | `Closure/ClosureScanGun.png`，32×30，枪 | 持枪 Shoot / 显示 | 保留现有攻击与技能 |

合计：21 把法杖、2 件投掷用品、1 把剑、2 把枪。16 把医疗武器当前共用苏苏洛物品图。资源目录中旧 `Mon3trCore.png` 未被当前物品引用，不代表实际手持外形。

## 验证

- 源码编译通过，0 错误；265 条已有警告。
- 隔离 tModLoader 加载实际模组及测试模组，234 项检查通过。对照原版紫晶法杖、手里剑、飞刀、铁阔剑、燧发枪的实际默认参数，检查所有 26 把武器的使用分类。
- 回归验证医疗武器后台调用、魔法加成、向上发射、魔力消耗、耐药与群疗间隔；后台调用不会切换槽位或改写当前武器动画。
- 测试代码在 `Doc/Validation/MedicItemUse20261029/MedicItemUseVerification/`；日志和隔离包在 `bin/MedicItemUse20261029/`。
- 尚未进行游戏内手持姿势与多人联机验收；没有安装到日常模组目录，也未提交或推送。
