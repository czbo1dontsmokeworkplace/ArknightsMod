// Ported from CalamityMod/Particles/GlowOrbParticle.cs at the author's request.
// Original color, scale, gravity and two-layer draw equations are retained.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Particle.T
{
    public class T_PlasmaBall_Particle : global::ArknightsMod.Common.Particle.Particle
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public bool UseAltVisual = true;
        public float fadeOut = 1;
        public bool imporant;
        public bool glowCenter;
        
        
        public override BlendState DrawBlendState => UseAltVisual ? BlendState.Additive : BlendState.AlphaBlend;
        // Kept for source compatibility; the local manager has no particle eviction limit.

        public override string TexturePath => "ArknightsMod/Common/Particle/T/T_PlasmaBall_Particle";

        public T_PlasmaBall_Particle(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, float scale, Color color, bool AddativeBlend = true, bool needed = false, bool GlowCenter = true)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Lifetime = lifetime;
            Color = InitialColor = color;
            UseAltVisual = AddativeBlend;
            imporant = needed;
            glowCenter = GlowCenter;
        }

        public override void Update()
        {
            Vector2 previousVelocity = Velocity;
            fadeOut -= 0.1f;
            Scale *= 0.93f;
            Color = Color.Lerp(InitialColor, InitialColor * 0.2f, (float)Math.Pow(LifetimeRatio, 3D));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
            // Calamity integrates the old velocity before Update; our manager integrates afterward.
            Position += previousVelocity - Velocity;
        }

        public override void Draw()
        {
            Vector2 scale = new Vector2(1f, 1f) * Scale;
            Texture2D texture = Texture;

            Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, scale, 0, 0f);
            if (glowCenter)
                Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.White * fadeOut, Rotation, texture.Size() * 0.5f, scale * new Vector2(0.5f, 0.5f), 0, 0f);
        }
    }
}
