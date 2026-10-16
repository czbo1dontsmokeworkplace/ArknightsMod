using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Medic.Sussurro;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Medical_Light_Particle : global::ArknightsMod.Common.Particle.Particle
{
    internal SussurroLightKind Kind;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void Update()
    {
        Velocity *= Kind == SussurroLightKind.Needle ? .95f : .94f;
        if (Kind == SussurroLightKind.Mote || Kind == SussurroLightKind.Cross)
            Velocity.Y -= .025f;
    }

    public override void Draw()
    {
        float p = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = 1f - MathF.Pow(p, 3f);
        float size = Scale * (1f - .3f * p);
        switch (Kind)
        {
            case SussurroLightKind.Butterfly:
                SussurroVisuals.DrawButterfly(Position, Rotation, Time, size, fade);
                break;
            case SussurroLightKind.Needle:
                // Halley 的内外双层纵向光粒，旋转严格跟随飞行方向。
                SussurroVisuals.Soft(Position, new Vector2(155f, 18f) * size, SussurroVisuals.Emerald, fade * .65f, Rotation);
                SussurroVisuals.Soft(Position, new Vector2(120f, 7f) * size, SussurroVisuals.Mint, fade, Rotation);
                SussurroVisuals.Soft(Position, new Vector2(85f, 2.8f) * size, SussurroVisuals.Heart, fade, Rotation);
                break;
            case SussurroLightKind.Mote:
                SussurroVisuals.Star(Position, 4.5f * size, Rotation + p * .7f, fade * .8f, Color);
                break;
            case SussurroLightKind.Streak:
                SussurroVisuals.Soft(Position, new Vector2(38f, 4f) * size, Color, fade, Rotation);
                break;
            case SussurroLightKind.Muzzle:
                float ring = (12f + 24f * p) * Scale;
                SussurroVisuals.Ring(Position, new Vector2(ring * .32f, ring), Rotation, p * 3f,
                    SussurroVisuals.Mint, (1f - p) * .8f, 1.4f);
                SussurroVisuals.Ring(Position, new Vector2(ring * .23f, ring * .7f), Rotation, -p * 4f,
                    SussurroVisuals.Emerald, (1f - p) * .6f);
                SussurroVisuals.Star(Position, 24f * size, Rotation, (1f - p) * .7f, SussurroVisuals.Heart);
                break;
            case SussurroLightKind.Flower:
                float bloom = (15f + 63f * (1f - MathF.Pow(1f - p, 3f))) * Scale;
                float flowerFade = MathF.Pow(1f - p, 1.4f);
                SussurroVisuals.Soft(Position, new Vector2(bloom * 2.7f), SussurroVisuals.Emerald, flowerFade * .48f);
                SussurroVisuals.Ring(Position, new Vector2(bloom), Rotation, p * 3f,
                    SussurroVisuals.Mint, flowerFade, 1.2f, 7f);
                SussurroVisuals.Ring(Position, new Vector2(bloom * .7f), -Rotation, -p * 5f,
                    SussurroVisuals.Emerald, flowerFade * .75f, 1.4f, 7f);
                SussurroVisuals.Ring(Position, new Vector2(bloom * 1.12f), Rotation, p,
                    SussurroVisuals.Mint, flowerFade * .35f);
                SussurroVisuals.Star(Position, 37f * size, Rotation, flowerFade, SussurroVisuals.Heart);
                break;
            case SussurroLightKind.Cross:
                float crossFade = MathF.Sin(MathF.PI * p);
                float r = 4.5f * size;
                SussurroVisuals.Soft(Position, new Vector2(24f) * size, SussurroVisuals.Emerald, crossFade * .5f);
                SussurroVisuals.Line(Position - Vector2.UnitX * r, Position + Vector2.UnitX * r, Color, 2f * size, crossFade);
                SussurroVisuals.Line(Position - Vector2.UnitY * r, Position + Vector2.UnitY * r, Color, 2f * size, crossFade);
                break;
        }
    }
}
