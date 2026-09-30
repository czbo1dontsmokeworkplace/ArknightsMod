using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    // 每招只锁定一次阵型；相邻齐射共享通道，不再每轮重新夹住玩家当前位置。
    internal Vector2 PatternOrigin, PatternDrift;
    internal int PatternSerial = -1, PatternStart;
    internal Vector2 PatternCenter => PatternOrigin + PatternDrift * Math.Max(0, Timer - PatternStart);
    internal void PreparePattern(Player player)
    {
        if (PatternSerial == Serial || Main.netMode == NetmodeID.MultiplayerClient) return;
        PatternSerial = Serial; PatternStart = Timer;
        PatternOrigin = player.Center;
        PatternDrift = ArenaActive ? Vector2.Zero : EvolutionRules.FormationDrift(player.velocity, (int)NPC.ai[2] % 2 == 0 ? 1 : -1);
        NPC.netUpdate = true;
    }
    // 保留全部扇形弹，挪开正中央那一发，形成有宽度的通路。
    internal Vector2 FormationDirection(Vector2 origin, int index, int count, float spread = .18f)
    {
        Vector2 delta = PatternCenter - origin;
        int slot = EvolutionRules.FanSlot(index, count);
        float gap = MathF.Atan2(190, Math.Max(1, delta.Length()));
        return delta.SafeNormalize(Vector2.UnitY).RotatedBy(Math.Sign(slot) * (gap + (Math.Abs(slot) - 1) * spread));
    }
    internal Vector2 FormationLanding(int index, int count, float spacing = 180) =>
        PatternCenter + new Vector2(EvolutionRules.FanSlot(index, count) * spacing, 20);
    internal bool AxisHealthLocked => Phase == 5 && Attack == 6 && Timer >= 0 && Timer < EvolutionRules.AxisLockEnd;
}
