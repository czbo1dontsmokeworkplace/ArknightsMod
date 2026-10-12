using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Caster.Passenger;

// 配色随每个粒子保存，不切换全局颜色，惊蛰和异客同时攻击时互不串色。
internal readonly record struct LightningPalette(Color Body, Color Edge, Color Core, Vector3 Light)
{
    internal static readonly LightningPalette Passenger = new(
        new(255, 145, 28), new(255, 218, 88), new(255, 247, 210), new(0.78f, 0.38f, 0.06f));
    internal static readonly LightningPalette Leizi = new(
        new(111, 93, 225), new(183, 191, 255), new(242, 244, 255), new(0.36f, 0.32f, 0.7f));
    internal static readonly LightningPalette Pudding = new(
        new(41, 144, 226), new(112, 224, 247), new(227, 252, 255), new(0.07f, 0.40f, 0.68f));
}
