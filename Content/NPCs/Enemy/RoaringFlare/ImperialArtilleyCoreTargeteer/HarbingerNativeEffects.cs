using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

// Reconnect the original registered materials. No replacement screen shader is used here.
public sealed class HarbingerNativeEffects : ModSystem
{
    private static readonly string[] Keys = { "AACTOC", "AACTOC2", "AACTTP", "LightRing", "AACTSTG3RBFence", "AACTSTG3RBNoise", "CollapsedExplosionPart1", "CollapsedExplosionPart2", "IACTSW" };
    private bool engaged;
    private static Filter Use(string key, bool active, Vector2 at) {
        Filter filter = Filters.Scene[key];
        if (filter == null) return null;
        if (active) { if (!filter.IsActive()) Filters.Scene.Activate(key, at); }
        else if (filter.IsActive()) filter.Deactivate();
        return active ? filter : null;
    }
    public override void PostUpdateEverything() {
        if (Main.dedServ) return;
        AACT boss = null;
        if (!Main.gameMenu) foreach (NPC npc in Main.ActiveNPCs) if (npc.ModNPC is AACT found) { boss = found; break; }
        if (boss == null) { if (engaged) Reset(); return; }
        engaged = true;
        Vector2 at = boss.NPC.Center;
        int phase = boss.Phase, clock = boss.Clock;
        bool transition = boss.NPC.ai[3] > 0;
        float age = HarbingerRules.TransitionTicks - boss.NPC.ai[3];
        bool colors = transition && age >= 0 && age < 60;
        Use("AACTOC", colors, at)?.GetShader().UseIntensity(.5f - age / 120f);
        Use("AACTOC2", colors && age < 30, at)?.GetShader().UseIntensity(age);
        bool warp = transition && age is >= 20 and < 100;
        float pulse = MathF.Sin(Math.Clamp((age - 20) / 80f, 0, 1) * MathHelper.Pi);
        Use("AACTTP", warp, at)?.GetShader().UseColor(360 * pulse, Math.Max(.12f, .5f - pulse * .35f), 0).UseTargetPosition(at);
        float breath = .2f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.Pi / 3) + .8f;
        Use("LightRing", phase == 2, at)?.GetShader().UseColor((220 + 20 * MathF.Sin(clock * MathHelper.Pi / 180)) / 255, 0, 0)
            .UseTargetPosition(at + new Vector2(0, 3)).UseIntensity(1024).UseOpacity(6).UseProgress(breath);
        float deathFade = phase == 4 ? Math.Max(0, 1 - clock / 60f) : 1;
        bool final = phase == 3 || phase == 4 && deathFade > 0;
        float noise = phase == 4 ? deathFade : .25f + .65f * Math.Clamp(1 - boss.NPC.life / (boss.NPC.lifeMax * .3f), 0, 1);
        Use("AACTSTG3RBFence", final, at)?.GetShader().UseIntensity(Math.Min(.999f, deathFade));
        Use("AACTSTG3RBNoise", final, at)?.GetShader().UseIntensity(Math.Min(.999f, noise));
        bool collapse = false, wave = false;
        foreach (Projectile p in Main.ActiveProjectiles) {
            collapse |= p.ModProjectile is CollapsedExplodeAim;
            wave |= p.ModProjectile is IACTScreenWave or ExplodeWave;
        }
        // Retirement bypasses OnKill to avoid spawning damaging descendants.
        // Release their shared filters only when no native user remains.
        if (!collapse) { Use("CollapsedExplosionPart1", false, at); Use("CollapsedExplosionPart2", false, at); }
        if (!wave) Use("IACTSW", false, at);
    }
    private void Reset() {
        if (!Main.dedServ) foreach (string key in Keys) Use(key, false, Vector2.Zero);
        engaged = false;
    }
    public override void OnWorldUnload() => Reset();
    public override void Unload() { if (engaged) Reset(); }
}
