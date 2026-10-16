using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Charge_Absorption_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly SpreadChargeVfx anchor;
    private readonly SpreadArt art;
    private readonly Vector2 origin;
    private readonly Vector2[] history = new Vector2[6];
    private Vector2 motion;
    private readonly float maxSpeed;
    private int orphanTicks;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;

    internal T_Charge_Absorption_Particle(SpreadChargeVfx anchor, SpreadArt art, Vector2 start, Vector2 aim, float charge)
    {
        this.anchor = anchor; this.art = art; Position = origin = start;
        Vector2 inward = (anchor.Center - start).SafeNormalize(aim);
        motion = inward * 2f + new Vector2(-inward.Y, inward.X) * 1.8f;
        maxSpeed = MathHelper.Lerp(12f, 7f, SpreadChargeVfx.Growth(charge));
        Lifetime = 34;
        Array.Fill(history, start);
    }
    public override void Update()
    {
        for (int i = history.Length - 1; i > 0; i--) history[i] = history[i - 1];
        history[0] = Position;
        Vector2 delta = anchor.Center - Position;
        bool lost = !anchor.Active || delta.LengthSquared() > 180f * 180f;
        if (lost || orphanTicks > 0)
        {
            orphanTicks++;
            motion *= .72f;
            if (orphanTicks >= 6) { Kill(); Color = Color.Transparent; return; }
        }
        else
        {
            float distance = delta.Length();
            if (distance < 6f) { Kill(); Color = Color.Transparent; return; }
            Vector2 desired = delta / distance * Math.Min(maxSpeed, distance * .38f);
            motion = Vector2.Lerp(motion, desired, .36f);
            if (motion.LengthSquared() > maxSpeed * maxSpeed) motion = motion.SafeNormalize(Vector2.Zero) * maxSpeed;
        }
        Position += motion;
        if (Vector2.DistanceSquared(origin, Position) > 170f * 170f) { Kill(); Color = Color.Transparent; return; }
        Rotation = motion.ToRotation();
        float fade = MathHelper.Clamp(Time / 3f, 0f, 1f) * MathHelper.Clamp((Lifetime - Time) / 6f, 0f, 1f)
            * (1f - orphanTicks / 6f);
        Color = SpreadArtsVfx.Edge(art) * fade;
    }
    public override void Draw()
    {
        if (Time >= Lifetime || !SpreadVfxBudget.Visible(Position)) return;
        for (int i = 0; i < history.Length - 1; i++)
            SpreadVfxDetail.Segment(i == 0 ? Position : history[i - 1], history[i],
                OperatorFlightVfx.Add(Color, (1f - i / (float)history.Length) * .55f), 2.2f - i * .32f);
        float size = 8f * MathHelper.Clamp((Lifetime - Time) / 6f, 0f, 1f);
        if (art == SpreadArt.Crystal)
            SpreadVfxDetail.Facet(Position, size * .5f, size * 1.3f, Rotation, Color, .9f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, OperatorFlightVfx.Add(Color, .85f), Rotation,
            Texture.Size() * .5f, new Vector2(size * 2.4f, size * (art == SpreadArt.Rupture ? .75f : 1.4f)) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
