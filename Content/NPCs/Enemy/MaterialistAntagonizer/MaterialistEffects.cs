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
    private struct Wave { internal Vector2 Position; internal float Radius, Angle, Squash; internal Color Color; internal int Age, Life; }
    private static readonly Wave[] waves = new Wave[64];
    private static readonly ulong[] muzzleTimes = new ulong[Main.maxNPCs];
    private static int cursor, clock;
    private static float shake;
    internal static void Pulse(Vector2 position, float radius, Color color, float force = 0)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(Main.LocalPlayer.Center, position) > 2400 * 2400) return;
        waves[cursor++ % waves.Length] = new Wave { Position = position, Radius = radius, Color = color, Age = 1, Life = 36, Squash = 1 };
        shake = Math.Max(shake, force * MathHelper.Clamp(1 - Vector2.Distance(Main.LocalPlayer.Center, position) / 2400, 0, 1));
    }
    internal static void MuzzleShock(Vector2 point, float angle, Color color, float power, int source)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(Main.LocalPlayer.Center, point) > 2400 * 2400) return;
        if (source >= 0 && source < muzzleTimes.Length)
        {
            ulong now = Main.GameUpdateCount + 10;
            if (now - muzzleTimes[source] < 5) return;
            muzzleTimes[source] = now;
        }
        waves[cursor++ % waves.Length] = new Wave { Position = point, Radius = 68 * power, Color = color,
            Age = 1, Life = 18, Squash = .38f, Angle = angle + MathHelper.PiOver2 };
        for (int i = 0; i < (MaterialistVisuals.Reduced ? 3 : 9); i++)
            MaterialistVisuals.Nano(point, (angle + Main.rand.NextFloat(-.7f, .7f)).ToRotationVector2() * Main.rand.NextFloat(3, 9) * power,
                i % 3 == 0 ? MaterialistVisuals.White : color, .7f + power * .35f, 18);
    }
    public override void OnWorldUnload() { Array.Clear(waves); Array.Clear(muzzleTimes); MaterialistBallistics.Reset(); cursor = clock = 0; shake = 0; }
    public override void Unload() { OnWorldUnload(); MaterialistVisuals.Unload(); }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        clock++; shake *= .86f;
        MaterialistBallistics.Update();
        for (int i = 0; i < waves.Length; i++) if (waves[i].Age > 0 && ++waves[i].Age > waves[i].Life) waves[i] = default;
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
            float u = wave.Age / (float)wave.Life;
            float opacity = (1 - u) * (MaterialistVisuals.Reduced ? .30f : .65f);
            MaterialistVisuals.Ellipse(wave.Position, wave.Radius * MathF.Sqrt(u), wave.Squash, wave.Angle,
                (wave.Color with { A = 0 }) * opacity, 3 * (1 - u) + 1);
            if (!MaterialistVisuals.Reduced) MaterialistVisuals.Ellipse(wave.Position, wave.Radius * MathF.Sqrt(u) * .82f, wave.Squash, wave.Angle, wave.Color * opacity * .5f, 1);
        }
        MaterialistBallistics.DrawImpacts();
        if (boss != null && boss.Order is FlightOrder.Deploy or FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Overload)
        {
            float alpha = MaterialistVisuals.Reduced ? .12f : .28f;
            if (boss.Order == FlightOrder.Arrival) alpha *= MaterialistFormation.Smooth(boss.Timer / 24f) * (1 - MaterialistFormation.Smooth((boss.Timer - 178) / 32f));
            Vector2 center = boss.NPC.Center;
            MaterialistVisuals.Brackets(center, new Vector2(440, 230), MaterialistVisuals.Cyan, alpha * 2);
            if (!MaterialistVisuals.Reduced)
                for (int i = 0; i < 9; i++)
                {
                    float x = -192 + i * 48;
                    MaterialistVisuals.Line(center + new Vector2(x, -100), center + new Vector2(x, 100), MaterialistVisuals.Cyan * .07f, 1);
                }
        }
        Main.spriteBatch.End();
        if (boss != null) MaterialistTechRenderer.Draw(boss);
    }
}
