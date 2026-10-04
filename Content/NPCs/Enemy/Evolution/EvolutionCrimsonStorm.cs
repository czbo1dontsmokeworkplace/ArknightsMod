using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// 独立客户端演出，不启动原版沙尘暴事件、不写风速、不增加 Buff 或伤害。
public sealed class EvolutionCrimsonStorm : ModSystem
{
    private sealed class Wisp
    {
        internal Vector2 Position, Velocity;
        internal float Rotation, Size;
        internal int Age, Life;
        internal bool Grain;
    }
    private readonly List<Wisp> wisps = new();
    private float intensity;
    private int clock, encounter, wind = 1;
    private Effect barrier;
    private bool barrierFailed;

    internal static float StormTarget(float encounterAge, float distance) =>
        MathHelper.SmoothStep(0, 1, Math.Clamp(encounterAge / 240f, 0, 1)) * Math.Clamp((4200 - distance) / 1200f, 0, 1);

    public override void OnWorldLoad() => ResetStorm();
    public override void OnWorldUnload() => ResetStorm();
    private void ResetStorm() { wisps.Clear(); intensity = 0; clock = encounter = 0; wind = 1; }
    public override void Unload()
    {
        ResetStorm();
        Effect old = barrier; barrier = null; barrierFailed = false;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused) return;
        Evolution nearby = null;
        float nearest = 4200;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is Evolution boss && n.Distance(Main.LocalPlayer.Center) < nearest)
            { nearby = boss; nearest = n.Distance(Main.LocalPlayer.Center); }
        if (nearby != null && encounter != nearby.Encounter)
        {
            encounter = nearby.Encounter;
            wind = Main.windSpeedCurrent < 0 ? -1 : 1; // 整场固定吹向，不随阶段改变。
        }
        float target = nearby == null ? 0 : StormTarget(nearby.NPC.ai[3], nearest);
        intensity = MathHelper.Clamp(intensity + Math.Clamp(target - intensity, -1f / 180, 1f / 240), 0, 1);
        if (intensity <= .001f) { wisps.Clear(); return; }
        clock++;
        for (int i = wisps.Count - 1; i >= 0; i--)
        {
            Wisp p = wisps[i];
            p.Position += p.Velocity;
            p.Age++;
            if (p.Age >= p.Life || Vector2.DistanceSquared(p.Position, Main.LocalPlayer.Center) > 4000 * 4000) wisps.RemoveAt(i);
        }
        int interval = EvolutionCinematics.Reduced ? 14 : 7;
        if (clock % interval != 0 || wisps.Count >= 64) return;
        bool grain = clock % (interval * 3) != 0;
        // 在镜头周围生成并渐显；宽屏仅增加覆盖面积，不增加不透明度。
        Vector2 size = new(Main.screenWidth / Main.GameViewMatrix.Zoom.X, Main.screenHeight / Main.GameViewMatrix.Zoom.Y);
        wisps.Add(new Wisp
        {
            Position = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloat(-size.X * .65f, size.X * .65f), Main.rand.NextFloat(-size.Y * .65f, size.Y * .65f)),
            Velocity = new Vector2(wind * (grain ? 11 : 4.5f), grain ? 1.2f : -.35f),
            Age = 0, Life = grain ? 100 : 240, Grain = grain,
            Size = Main.rand.NextFloat(.7f, 1.3f), Rotation = Main.rand.NextFloat(-.2f, .2f)
        });
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        if (intensity > .001f && wisps.Count > 0)
        {
            Texture2D smoke = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Sniper/Typhon/Effects/ChargeSmoke01").Value;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                foreach (Wisp p in wisps)
                {
                    float fade = MathF.Sin(MathHelper.Pi * p.Age / p.Life) * intensity;
                    if (p.Grain)
                        EvolutionVisuals.Glow(p.Position, new Color(213, 35, 54) * (.26f * fade), new Vector2(18, 3) * p.Size, .11f * wind);
                    else
                        DrawStormSmoke(Main.spriteBatch, smoke, p.Position - Main.screenPosition, p.Size, p.Rotation, fade);
                }
            }
            finally { Main.spriteBatch.End(); }
        }
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is Evolution boss && boss.Phase == 1) DrawNestBarrier(boss);
    }

    internal static void DrawStormSmoke(SpriteBatch batch, Texture2D smoke, Vector2 screenCenter, float size, float rotation, float fade) =>
        batch.Draw(smoke, screenCenter, null, new Color(143, 21, 39, 0) * (.28f * fade),
            rotation, smoke.Size() * .5f, new Vector2(640, 200) * size / smoke.Size(), SpriteEffects.None, 0);

    private void DrawNestBarrier(Evolution boss)
    {
        if (barrierFailed) return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Texture oldTexture = device.Textures[1];
        SamplerState oldSampler = device.SamplerStates[1];
        bool begun = false;
        try
        {
            // 蜜蜡/卡涅利安/林法杖同款双层球面材质，仅换血红色和尺寸。
            barrier ??= new Effect(device, Mod.GetFileBytes("Assets/Effects/Phalanx/PhalanxBarrier.fxc"));
            Texture2D noise = ModContent.Request<Texture2D>("ArknightsMod/Assets/Effects/Phalanx/NoiseSoft").Value;
            float opacity = MathHelper.SmoothStep(0, 1, Math.Clamp(boss.NPC.ai[3] / 75f, 0, 1));
            float beat = Math.Max(0, boss.Timer % EvolutionNestRules.RadiationInterval - 60) / 60f;
            float charge = boss.Timer % EvolutionNestRules.RadiationInterval is >= 60 and < 124 ? Math.Clamp(beat, 0, 1) : 0;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, barrier, Main.GameViewMatrix.TransformationMatrix);
            begun = true;
            DrawBarrierMaterial(Main.spriteBatch, device, barrier, noise, boss.NPC.Center - Main.screenPosition, Main.GlobalTimeWrappedHourly, opacity, charge);
        }
        catch (Exception e) { barrierFailed = true; Mod.Logger.Warn("Evolution nest barrier unavailable: " + e); }
        finally
        {
            if (begun) Main.spriteBatch.End();
            device.Textures[1] = oldTexture; device.SamplerStates[1] = oldSampler;
        }
    }

    internal static void DrawBarrierMaterial(SpriteBatch batch, GraphicsDevice device, Effect effect, Texture2D noise,
        Vector2 screenCenter, float time, float opacity, float charge)
    {
        effect.Parameters["globalTime"].SetValue(time);
        effect.Parameters["uShieldColor"].SetValue(new Vector3(.36f, .015f, .045f));
        effect.Parameters["uEdgeColor"].SetValue(new Vector3(1, .17f + charge * .12f, .25f));
        effect.Parameters["uCoreColor"].SetValue(new Vector3(1, .68f, .72f));
        effect.Parameters["uCharge"].SetValue(opacity);
        effect.Parameters["uHitFlash"].SetValue(charge * .3f);
        effect.Parameters["uOpacity"].SetValue(opacity * .6f);
        effect.Parameters["uRotation"].SetValue(time * .22f);
        device.Textures[1] = noise; device.SamplerStates[1] = SamplerState.LinearWrap;
        float diameter = 310 * (1 + MathF.Sin(time * 2) * .018f);
        for (int layer = 0; layer < 2; layer++)
        {
            effect.Parameters["uLayer"].SetValue((float)layer);
            effect.CurrentTechnique.Passes[0].Apply();
            batch.Draw(noise, screenCenter, null, Color.White, 0,
                noise.Size() * .5f, new Vector2(diameter * (1 + layer * .025f)) / noise.Size(), SpriteEffects.None, 0);
        }
    }
}
