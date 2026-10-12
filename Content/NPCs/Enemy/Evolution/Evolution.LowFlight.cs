using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    // 二阶段上下边界不跟随玩家，也没有左右边界。
    private void ConstrainLowFlight()
    {
        float top = CorridorTop + NPC.height * .5f + 24;
        float bottom = CorridorBottom - NPC.height * .5f - 48;
        NPC.Center = new Vector2(NPC.Center.X, MathHelper.Clamp(NPC.Center.Y, top, bottom));
        NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, top - NPC.Center.Y, bottom - NPC.Center.Y);
    }

    private void HoverLow(Player player)
    {
        float side = (int)NPC.ai[2] % 2 == 0 ? -1 : 1;
        Vector2 destination = new(player.Center.X + side * 390,
            MathHelper.Clamp(player.Center.Y - 190, CorridorTop + 170, CorridorBottom - 220));
        MoveTo(destination, 8, .055f);
    }

    private void DoLowFlight(Player player)
    {
        PreparePattern(player);
        HoverLow(player);
        int t = Timer;
        // 存活小怪跨招式保留；有限补员，避免每招清场造成“光球弹幕”的旧节奏。
        if (t == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            BirthLowBrood(EvolutionBrood.Spider, player, 0);
            BirthLowBrood(EvolutionBrood.Puppet, player, 1);
            BirthLowBrood((int)NPC.ai[2] % 2 == 0 ? EvolutionBrood.GiantSpider : EvolutionBrood.Abomination, player, 2);
        }
        if (t == 175)
        {
            BirthLowBrood(EvolutionBrood.Tumor, player, 3);
            BirthLowBrood(EvolutionBrood.Bomb, player, 4);
        }
        switch (Attack)
        {
            case 0: // 育生主导：只有两轮慢速抛射，给清理小怪留出时间。
                if (t == 110 || t == 245)
                    for (int side = -1; side <= 1; side += 2)
                        Lob(EvolutionShot.Blood, NPC.Center, PatternCenter + new Vector2(side * 240, 80), 85);
                break;
            case 1: // 竖直光束沿横向排布；相邻束间保留宽通路，不横扫800像素战斗带。
                if (t == 70 || t == 205) CorridorLaserColumns(t == 70 ? 0 : 1);
                break;
            case 2: // 仅在通道两侧留下慢速血灵，方向在预警时锁定。
                if (t == 85 || t == 220)
                    for (int side = -1; side <= 1; side += 2)
                        Shoot(EvolutionShot.Spirit,
                            new Vector2(PatternCenter.X + side * 360, CorridorTop + 70), Vector2.UnitY * 3,
                            delay: 55, lifetime: 155, stagedSpirit: true);
                break;
            case 3: // 少量纵向血喷，替代斜向/横向密集激光墙。
                if (t == 80 || t == 225)
                    for (int i = -2; i <= 2; i++)
                    {
                        if (i == 0) continue;
                        Shoot(EvolutionShot.Lance, new Vector2(PatternCenter.X + i * 220, CorridorTop + 30),
                            Vector2.UnitY * 10, EvolutionRules.CorridorHeight - 60, 70, 185);
                    }
                break;
        }
        if (t >= 390) NextAttack();
    }

    private void CorridorLaserColumns(int wave)
    {
        // 整招固定基准，第二轮只将外围光束外移，不把原有中央通路封死。
        for (int i = -3; i <= 3; i++)
        {
            if (i == 0) continue;
            float x = PatternOrigin.X + i * 210 + Math.Sign(i) * wave * 35;
            Shoot(EvolutionShot.Beam, new Vector2(x, CorridorTop + 12), Vector2.UnitY,
                EvolutionRules.CorridorHeight - 24, 72, 115, quietLaunch: true);
        }
    }

    private void BirthLowBrood(EvolutionBrood kind, Player player, int slot)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        float side = slot % 2 == 0 ? -1 : 1;
        Vector2 near = new(player.Center.X + side * (310 + slot * 38),
            MathHelper.Clamp(player.Bottom.Y - 48, CorridorTop + 60, CorridorBottom - 64));
        int height = EvolutionBroodNPC.GroundHeight(kind);
        int width = kind == EvolutionBrood.GiantSpider ? 58 : kind == EvolutionBrood.Abomination ? 66 : 36;
        Vector2 point = TryNestFloor(near, width, height, out Vector2 floor) && floor.Y + height * .5f < CorridorBottom
            ? floor : new Vector2(near.X, CorridorBottom - height * .5f - 8);
        // 出生35帧只显示育生光晕，尚无接触伤害。
        SpawnBrood(kind, point, side: -side, formation: slot);
    }
}
