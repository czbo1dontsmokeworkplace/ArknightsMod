using System;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// Local presentation only. No death laser, slash, shard or fault explosion is an NPC/Projectile.
public sealed partial class MaterialistDeathSystem : ModSystem
{
    private EvolutionDeathCameraPlayback camera;
    private Vector2 center;
    private float rotation, impactShake;
    private int encounter = -1, age = -1, lastCue = -1;
    private bool following;
    internal static bool PresentationActive { get; private set; }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        if (Main.gameMenu || !Main.LocalPlayer.active) { Reset(); return; }
        UpdateFragments();
        impactShake *= .84f;
        MaterialistAntagonizer selected = null;
        float nearest = 2800 * 2800;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is not MaterialistAntagonizer boss || boss.Order != FlightOrder.Death) continue;
            if (boss.Encounter == encounter) { selected = boss; break; }
            float distance = npc.DistanceSQ(Main.LocalPlayer.Center);
            if (distance < nearest) { selected = boss; nearest = distance; }
        }
        if (selected != null)
        {
            if (encounter != selected.Encounter)
            {
                Reset(); encounter = selected.Encounter;
                // Join at the current synchronized point; never replay an old finale/entrance sound.
                lastCue = selected.Timer - 1;
            }
            center = selected.NPC.Center; rotation = selected.NPC.rotation;
            age = Math.Max(age, selected.Timer);
            following = true;
        }
        else if (following)
        {
            following = false;
            // Only a completed death retains the final electrical aftershock.
            if (age < MaterialistDeathRules.Finale) age = MaterialistDeathRules.VisualEnd;
            else age++;
        }
        else if (age >= 0) age++;
        PresentationActive = encounter >= 0 && age < MaterialistDeathRules.VisualEnd;
        bool allowCamera = !Main.LocalPlayer.dead && ModContent.GetInstance<MaterialistVisualConfig>().DeathCamera;
        camera.Update(selected != null && allowCamera, center, age, cancel: !allowCamera);
        if (PresentationActive)
        {
            // Small packet corrections cannot duplicate event emission or fill the screen with catch-up effects.
            for (int tick = Math.Max(lastCue + 1, age - 3); tick <= age; tick++) EmitCue(tick);
            lastCue = Math.Max(lastCue, age);
            Lighting.AddLight(center, MaterialistDeathRules.Tint(age / 10).ToVector3() * .9f);
        }
        if (!PresentationActive && camera.Weight == 0 && !HasFragments) encounter = -1;
    }

    private void EmitCue(int tick)
    {
        bool reduced = MaterialistVisuals.Reduced;
        if (tick == 1)
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .7f, Pitch = -.55f, MaxInstances = 1 }, center);
        if (MaterialistDeathRules.Burst(tick))
        {
            int count = MaterialistDeathRules.BurstCount(tick, reduced);
            float power = MathHelper.Lerp(1.7f, 3.6f, tick / (float)MaterialistDeathRules.Duration);
            for (int i = 0; i < count; i++)
                EmitFragments(MaterialistDeathRules.FaultPoint(center, tick, i), reduced ? 3 : 12, power, tick * 37 + i, false);
            if (tick % 12 == 0)
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .65f, Pitch = -.5f, MaxInstances = 3 }, center);
            if (tick % 24 == 0) Kick(3);
        }
        if (tick < MaterialistDeathRules.Finale && tick % 10 == 0)
        {
            EmitFragments(MaterialistDeathRules.FaultPoint(center, tick, 0), reduced ? 14 : 40,
                1 + tick / 180f, tick + 619, true);
            Kick(3);
        }
        if (tick >= 48 && tick < MaterialistDeathRules.Finale && (tick - 48) % 14 == 0)
            SoundEngine.PlaySound(SoundID.Item158 with { Volume = .43f, Pitch = tick / 260f, MaxInstances = 3 }, center);
        if (tick is 40 or 108 or 156)
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = .55f, Pitch = tick / 240f, MaxInstances = 2 }, center);
        if (tick == MaterialistDeathRules.Finale)
        {
            EmitFragments(center, reduced ? 40 : 220, 2.2f, 907, true);
            Kick(9);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = .8f, Pitch = -.6f, MaxInstances = 1 }, center);
            SoundEngine.PlaySound(SoundID.Zombie104 with { Volume = .6f, Pitch = -.4f, MaxInstances = 1 }, center);
        }
    }

    private void Kick(float force)
    {
        float proximity = 1 - MaterialistDeathRules.Smooth(1000, 2600, Vector2.Distance(Main.LocalPlayer.Center, center));
        impactShake = Math.Max(impactShake, Math.Min(10, force * proximity));
    }
    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer.dead) return;
        if (camera.Weight > 0)
            Main.screenPosition = camera.Apply(Main.screenPosition, new Vector2(Main.screenWidth, Main.screenHeight),
                new Vector2(Main.maxTilesX * 16, Main.maxTilesY * 16));
        if (MaterialistVisuals.Reduced || !ModContent.GetInstance<MaterialistVisualConfig>().ScreenShake) return;
        float force = MaterialistDeathRules.CameraForce(camera.LastAge) * camera.Weight;
        Main.screenPosition += new Vector2(MathF.Sin(camera.LastAge * 2.43f), MathF.Cos(camera.LastAge * 1.83f)) * force;
        float clock = Main.GameUpdateCount;
        Main.screenPosition += new Vector2(MathF.Sin(clock * 2.43f), MathF.Cos(clock * 1.83f)) * impactShake;
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || !PresentationActive && !HasFragments) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            if (PresentationActive) MaterialistVisuals.DrawDeathDisplay(center, rotation, age);
            DrawFragments();
        }
        finally { Main.spriteBatch.End(); }
    }
    private void Reset()
    {
        camera = default; encounter = age = lastCue = -1;
        following = PresentationActive = false; impactShake = 0;
        ClearFragments();
    }
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();
}
