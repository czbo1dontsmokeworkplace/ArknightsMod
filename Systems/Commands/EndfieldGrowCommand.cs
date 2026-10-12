using System;
using System.Collections.Generic;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Commands;

public sealed class EndfieldGrowCommand : ModCommand
{
    public override string Command => "endfieldgrow";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/endfieldgrow [all]";
    public override string Description => "在周围生成终末地自然资源；all 在任意平地各生成一种（调试）";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length > 1 || (args.Length == 1 && !string.Equals(args[0], "all", StringComparison.OrdinalIgnoreCase)))
        {
            caller.Reply($"用法：{Usage}", Color.OrangeRed);
            return;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            caller.Reply("请在单人游戏或服务器端执行，以保证资源生成同步。", Color.OrangeRed);
            return;
        }
        int px = (int)(caller.Player.Center.X / 16);
        int py = (int)(caller.Player.Center.Y / 16);
        if (args.Length == 1)
        {
            GrowAll(caller, px, py);
            return;
        }
        int count = 0;
        for (int x = px - 40; x <= px + 40; x++)
            for (int y = py - 40; y <= py + 40; y++)
                if (WorldGen.InWorld(x, y, 20) && Main.tile[x, y].HasTile && !Main.tile[x, y - 1].HasTile
                    && EndfieldNaturalGrowthSystem.TryGenerateAtGround(x, y, Main.rand, ignoreChance: true))
                    count++;
        caller.Reply($"已生成 {count} 处终末地自然资源；仍遵守地皮、深度、空间及住房保护规则。", Color.LightGreen);
    }

    private static void GrowAll(CommandCaller caller, int px, int py)
    {
        var grounds = new List<Point>();
        for (int x = px - 40; x <= px + 40; x++)
            for (int y = py - 40; y <= py + 40; y++)
                if (WorldGen.InWorld(x, y, 20) && Main.tile[x, y].HasTile && !Main.tile[x, y - 1].HasTile)
                    grounds.Add(new Point(x, y));
        grounds.Sort((a, b) => DistanceSquared(a, px, py).CompareTo(DistanceSquared(b, px, py)));

        var indices = new List<int>();
        for (int index = 0; index < EndfieldNaturalCatalog.Resources.Length; index++)
            indices.Add(index);
        // 优先安放体积大的资源，避免小资源先占用唯一足够高的空位。
        indices.Sort((a, b) =>
        {
            int heightOrder = EndfieldNaturalCatalog.Resources[b].Height.CompareTo(EndfieldNaturalCatalog.Resources[a].Height);
            return heightOrder != 0 ? heightOrder : a.CompareTo(b);
        });

        var missing = new List<string>();
        int count = 0;
        foreach (int index in indices)
        {
            bool placed = false;
            foreach (Point ground in grounds)
                if (EndfieldNaturalGrowthSystem.TryPlaceResourceAtGround(index, ground.X, ground.Y, bypassHabitat: true))
                {
                    placed = true;
                    count++;
                    break;
                }
            if (!placed)
                missing.Add(Terraria.Lang.GetItemNameValue(EndfieldNaturalCatalog.ItemType(index)));
        }
        caller.Reply($"已在周围 40 格内生成 {count}/{indices.Count} 种终末地自然资源（每种一处）。", Color.LightGreen);
        if (missing.Count > 0)
            caller.Reply($"空间不足，未生成：{string.Join("、", missing)}。请准备平整实心地面，上方留出至少 4 格空位。", Color.OrangeRed);
    }

    private static int DistanceSquared(Point point, int px, int py)
        => (point.X - px) * (point.X - px) + (point.Y - py) * (point.Y - py);
}
