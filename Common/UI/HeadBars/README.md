# 通用头顶条 / Weapon Head Bar

以后说“加 BrinyBaron 那个头顶条”或“加通用头顶条”，就使用这里。

本组件沿用作者在 `CalamityLegendsComeBack/UI/BoundedHeadBarRenderer.cs` 中实现、BrinyBaron 右键冲刺冷却使用的造型：64×14 削角短框、七段渐变填充、细亮边与内部扫描亮光。绘制不依赖 Calamity，也不额外修改着色器或 SpriteBatch 状态。

## 给其他武器接入

继承 `WeaponHeadBar`，只需实现 `TryGetBar`：

```csharp
public sealed class ExampleHeadBar : WeaponHeadBar
{
    protected override bool TryGetBar(out HeadBarRequest request)
    {
        request = default;
        if (Player.HeldItem.ModItem is not ExampleWeapon) return false;
        request = new HeadBarRequest(
            Player.GetModPlayer<ExamplePlayer>().Progress, // 0～1
            new HeadBarStyle(background, startColor, endColor) {
                Scale = 1f, Opacity = .92f, OffsetY = 58f,
                FadeInTicks = 12, FadeOutTicks = 18, SlidePixels = 4f,
                FillSmoothing = .22f
            });
        return true;
    }
}
```

共享绘制层自动发现子类，无需改注册列表。返回 `true` 时即使进度是零也显示空条，返回 `false` 时保留最后的颜色和进度并淡出；淡入淡出中途切回会平滑反转。只有本地玩家显示，影子、其他玩家和专服不会重复绘制。位置使用玩家绘制坐标，随缩放和倒重力移动。

- `WeaponHeadBar.cs`：可继承的公共基类和自动绘制层。
- `HeadBarStyle.cs`：进度、颜色、尺寸、位置、淡入淡出参数。
- `HeadBarAnimation.cs`：显示、填充平滑和命中闪光状态。
- `BoundedHeadBarRenderer.cs`：原设计的绘制器，也可直接用于其他 UI。
- `DreadnoughtHeadBar.cs`：无畏者的完整接入例子。
- `MechAccordHeadBar.cs`：御械术士的实际攻击阶段接入例子。

## 无畏者

空条对应100%面板伤害，满条对应300%面板伤害；不再叠加攻击频率。每圈只能命中一个敌人，命中后立即关闭本圈出伤，下一圈重新允许命中；保留三倍伤害、三倍击退、必定暴击和重击特效。每次有效命中增加20个百分点，10次叠满；无需连续命中同一目标，持续空挥保留当前进度。手持停攻1秒后正常衰减，满条约4秒回到基础值；切走后立即以3倍速度衰减，满条约1.33秒归零。无畏者头顶条在切走后保留显示、持续回落，归零才淡出；其他头顶条仍按各自显示条件处理。实际伤害使用武器正常面板计算，挥动控制器不再重复乘一次伤害增益。

## 御械术士

卡达、洛洛、澄闪手持时显示，分别使用青、橙、粉色；停攻显示空条，切走淡出。读取对应持杖弹幕的真实攻击阶段，不另设计时器：卡达和洛洛普通攻击180帧填满、强化60帧耗尽；澄闪普通攻击150帧填满、强化36帧耗尽，随后重复。填充不额外平滑，确保强化开始时正好满条，显示和隐藏仍使用公共淡入淡出。

## 重剑手

四把重剑手武器按住左键蓄力时显示 `CrusherHeadBar`，直接读取 `CrusherHeldBlade.ChargeProgress`，填充不增加平滑延迟。松手隐藏，保留公共8帧淡出；颜色对应金晶、青风、赤焰、深海。铎铃二技能的独立蓄势也由相同条读取 `CrusherPlayer.Charge`，不与普通左键蓄力重复显示。

## 冲锋手

红豆、格拉尼、野鬃、风笛的右键冷却使用 `VanguardChargeHeadBar`，继承 `WeaponHeadBar`。直接读取原有 `VanguardSpearPlayer.Cooldown.Ratio`，0代表刚冲锋、1代表5秒共享冷却结束；不增加计时器或填充平滑。手持时显示，切走以公共8帧淡出，切回以4帧淡入，保留红/蓝/橙/金主题色。独立的旧 `VanguardChargeCooldownLayer` 已移除，避免两条叠画。冷却逻辑、跨武器共享及死亡继续计时不变。
