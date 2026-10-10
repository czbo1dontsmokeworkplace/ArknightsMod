using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class MaterialistVisualConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(false)] public bool ReducedEffects { get; set; }
    [DefaultValue(true)] public bool ScreenShake { get; set; } = true;
}

public sealed class MaterialistEffects : ModSystem
{
    private struct Wave { internal Vector2 Position; internal float Radius; internal Color Color; internal int Age; }
    private static readonly Wave[] waves = new Wave[24];
    private static int cursor, clock;
    private static float shake;
    internal static void Pulse(Vector2 position, float radius, Color color, float force = 0)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(Main.LocalPlayer.Center, position) > 2400 * 2400) return;
        waves[cursor++ % waves.Length] = new Wave { Position = position, Radius = radius, Color = color, Age = 1 };
        shake = Math.Max(shake, force * MathHelper.Clamp(1 - Vector2.Distance(Main.LocalPlayer.Center, position) / 2400, 0, 1));
    }
    public override void OnWorldUnload() { Array.Clear(waves); cursor = clock = 0; shake = 0; }
    public override void Unload() { OnWorldUnload(); MaterialistVisuals.Unload(); }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        clock++; shake *= .86f;
        for (int i = 0; i < waves.Length; i++) if (waves[i].Age > 0 && ++waves[i].Age > 36) waves[i] = default;
    }
    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu || MaterialistVisuals.Reduced || !ModContent.GetInstance<MaterialistVisualConfig>().ScreenShake) return;
        Main.screenPosition += new Vector2(MathF.Sin(clock * 2.3f), MathF.Cos(clock * 1.8f)) * Math.Min(6, shake);
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        MaterialistAntagonizer boss = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is MaterialistAntagonizer b && npc.Distance(Main.LocalPlayer.Center) < 2400) { boss = b; break; }
        bool any = boss != null;
        foreach (var wave in waves) if (wave.Age > 0) any = true;
        if (!any) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (var wave in waves)
        {
            if (wave.Age == 0) continue;
            float u = wave.Age / 36f;
            float opacity = (1 - u) * (MaterialistVisuals.Reduced ? .30f : .65f);
            MaterialistVisuals.Ring(wave.Position, wave.Radius * MathF.Sqrt(u), (wave.Color with { A = 0 }) * opacity, 0, 3 * (1 - u) + 1);
            if (!MaterialistVisuals.Reduced) MaterialistVisuals.Ring(wave.Position, wave.Radius * MathF.Sqrt(u) * .9f, wave.Color * opacity * .4f, 0, 1);
        }
        if (boss != null && boss.Order is FlightOrder.Deploy or FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Overload)
        {
            float alpha = MaterialistVisuals.Reduced ? .12f : .28f;
            Vector2 center = boss.NPC.Center;
            MaterialistVisuals.Ring(center, 290, MaterialistVisuals.Cyan * alpha, clock * .003f, 2, .62f, 6);
            MaterialistVisuals.Ring(center, 320, MaterialistVisuals.Cyan * alpha, -clock * .004f, 2, .62f, 48);
            if (!MaterialistVisuals.Reduced)
                for (int i = 0; i < 12; i++)
                {
                    float a = clock * .006f + i * MathHelper.TwoPi / 12;
                    Vector2 d = new(MathF.Cos(a), MathF.Sin(a) * .62f);
                    MaterialistVisuals.Line(center + d * 265, center + d * (i % 3 == 0 ? 335 : 310), MaterialistVisuals.Cyan * alpha, 2);
                }
        }
        Main.spriteBatch.End();
    }
}
