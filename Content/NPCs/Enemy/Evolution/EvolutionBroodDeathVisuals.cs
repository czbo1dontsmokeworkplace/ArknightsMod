using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionBroodDeathVisuals
{
    internal static void Emit(Vector2 center, Vector2 size, Color theme)
    {
        if (Main.dedServ) return;
        // 死亡表现统一为柔和烟雾；没有通用发光碎条、假尸块或额外伤害射弹。
        int count = size.X > 50 ? 8 : 5;
        Color smoke = Color.Lerp(theme, new Color(54, 44, 48), .62f);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Main.rand.NextVector2Circular(size.X * .3f, size.Y * .3f);
            Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 1.8f) - Vector2.UnitY * .7f;
            OperatorTextureEffects.Puff(center + offset, velocity, smoke,
                Main.rand.NextFloat(28, 43) + size.X * .24f, Main.rand.Next(26, 42));
        }
    }
}
