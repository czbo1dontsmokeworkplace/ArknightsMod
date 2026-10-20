using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// Port of Calamity's Scorpio NanoParticle: discrete position jitter, axis-aligned squares,
// sinusoidal opacity and exponential drag. Arknights' particle manager owns time advancement.
// Reference code and NanoParticleSmall/Big textures: Azafure, LLC, Calamity Mod.
// https://github.com/CalamityTeam/CalamityModPublic (Particles/NanoParticle.cs).
public sealed class MaterialistNanoParticle : Particle
{
    public override string TexturePath => MaterialistAntagonizer.AssetRoot + "NanoParticleSmall";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.PointClamp;
    private bool big;
    public MaterialistNanoParticle(Vector2 point, Vector2 velocity, Color color, float scale, int life, bool large)
    {
        Position = point; Velocity = velocity; Color = color; Scale = scale; Lifetime = life; big = large;
    }
    public override void Update()
    {
        Velocity *= .95f; Scale *= .98f;
        Opacity = Math.Max(0, MathF.Sin(LifetimeRatio * MathHelper.Pi));
        if (Time % 3 == 0) Position += Main.rand.NextVector2Circular(13, 13);
        Lighting.AddLight(Position, Color.ToVector3() * (.25f * Opacity));
    }
    public override void Draw()
    {
        Texture2D texture = big ? MaterialistVisuals.Asset(MaterialistAntagonizer.AssetRoot + "NanoParticleBig") : Texture;
        int size = big ? 8 : 6;
        Rectangle frame = new(0, size, size, size);
        Main.spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * Opacity, 0,
            frame.Size() * .5f, Scale, SpriteEffects.None, 0);
    }
}
