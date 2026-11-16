using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.BossArsenal;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// Reuse BlackIceCoffin's actual six-frame fracture renderer; never spawn the
// friendly damage projectile as a boss effect. Twelve local slots bound overlap.
public sealed class FrostNovaIceBurstSystem : ModSystem
{
    private struct Burst { internal Vector2 Center; internal int Age; internal float Scale; }
    private static readonly Burst[] bursts = new Burst[12];
    private static int cursor;
    internal static void Emit(Vector2 center, float scale = 1)
    {
        if (!FrostNovaVisuals.Visible(center)) return;
        for (int i = 0; i < bursts.Length; i++)
            if (bursts[i].Scale > 0 && bursts[i].Age < 8 && Vector2.DistanceSquared(bursts[i].Center, center) < 72 * 72) return;
        bursts[cursor] = new Burst { Center = center, Scale = Math.Clamp(scale, .5f, 1.7f) };
        cursor = (cursor + 1) % bursts.Length;
        FrostNovaVisuals.Shatter(center, Vector2.Zero, (int)(12 * scale));
        FrostNovaColdMistSystem.Begin(center);
        FrostNovaVisuals.Cast(center, 2, scale);
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gamePaused) return;
        for (int i = 0; i < bursts.Length; i++) if (bursts[i].Scale > 0 && ++bursts[i].Age >= 36) bursts[i].Scale = 0;
    }
    internal static void Draw(Vector2 center, int age, float scale)
    {
        // Impact visuals are actual particles, emitted once by Emit; no drawing projectile or expanding circle.
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool any = false;
        foreach (var burst in bursts) if (burst.Scale > 0) { any = true; break; }
        if (!any) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try { foreach (var burst in bursts) if (burst.Scale > 0) Draw(burst.Center, burst.Age, burst.Scale); }
        finally { Main.spriteBatch.End(); }
    }
    public override void OnWorldUnload() { Array.Clear(bursts); cursor = 0; }
    public override void Unload() => OnWorldUnload();
}
