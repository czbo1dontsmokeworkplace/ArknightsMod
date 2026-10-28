using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

// A pair of attached feather fans. Motion is driven by a shared shoulder stroke,
// body lean and spread, never by independently orbiting / chasing wing cells.
public readonly record struct VectorWingInput(Vector2 Center, Vector2 Velocity, int Direction, bool Jump, float Gravity = 1)
{
    public static VectorWingInput From(Player player)
    {
        float gravity = player.gravDir < 0 ? -1 : 1;
        return new(new Vector2(player.MountedCenter.X, player.MountedCenter.Y * gravity),
            new Vector2(player.velocity.X, player.velocity.Y * gravity), player.direction, player.controlJump, gravity);
    }
}

public readonly record struct VectorWingPose(VectorWingFeather[] Pieces, Vector2 Center, float Gravity)
{
    public float Presence {
        get { if (Pieces == null) return 0; float value = 0; foreach (var p in Pieces) if (p.Active) value = Math.Max(value, p.FadePower); return value; }
    }
    public Vector2 World(Vector2 point) => new(point.X, point.Y * Gravity);
}

public sealed class VectorWingFeather
{
    public int Index, Time;
    public bool Active, Retiring;
    public Vector2 Front, Back;
    public float FrontRotation, BackRotation, FrontLength, BackLength, Width, Fade, MotionGlow;
    public float FadePower => Fade;
}

public sealed class VectorWingMotion
{
    private readonly VectorWingFeather[] pieces = new VectorWingFeather[7];
    private float presence, phase, spread = .78f, lean, stroke;
    private int age;
    public VectorWingPose Pose { get; private set; }
    public VectorWingMotion() { for (int i = 0; i < pieces.Length; i++) pieces[i] = new() { Index = i }; }
    public void Update(Player owner, int mode) => Step(VectorWingInput.From(owner), mode > 0 && !owner.dead);
    public void Step(VectorWingInput input, bool enabled)
    {
        age++;
        presence = Math.Clamp(presence + (enabled ? 1f / 18 : -1f / 14), 0, 1);
        float reveal = presence * presence * (3 - 2 * presence);
        float rising = Math.Clamp(-input.Velocity.Y / 8, 0, 1), falling = Math.Clamp(input.Velocity.Y / 12, 0, 1);
        float pace = Math.Clamp(Math.Abs(input.Velocity.X) / 9, 0, 1);
        float targetSpread = enabled ? .78f + rising * .22f - falling * .16f + (input.Jump ? .08f : 0) : .14f;
        spread = MathHelper.Lerp(spread, targetSpread, .1f);
        lean = MathHelper.Lerp(lean, Math.Clamp(input.Velocity.X * .025f, -.26f, .26f), .12f);
        phase += .065f + rising * .085f + pace * .035f + (input.Jump ? .04f : 0);
        float beat = MathF.Sin(phase);
        // One coherent shoulder beat for the whole wing, with a small flexible tip response.
        stroke = MathHelper.Lerp(stroke, beat * (.055f + rising * .13f + falling * .045f), .22f);
        for (int i = 0; i < pieces.Length; i++)
        {
            var p = pieces[i]; float t = i / 6f;
            p.Active = presence > 0; p.Retiring = !enabled; p.Time = age; p.Fade = reveal;
            p.Width = (12 - t * 3) * (.65f + reveal * .35f);
            p.MotionGlow = Math.Clamp((rising + pace * .45f) * Math.Max(0, beat), 0, 1);
            for (int side = -1; side <= 1; side += 2)
            {
                // Feather roots stay joined to the shoulder / leading edge, even during a turn.
                Vector2 root = input.Center + new Vector2(-input.Direction * 3 + side * (5 + t * 8), -10 + t * 6);
                float open = (.35f + spread * .65f) * (.2f + reveal * .8f);
                float angle = (-1.0f + t * 1.45f) * open + stroke * (1 + t * .18f) + side * lean;
                float layerScale = side == input.Direction ? 1 : .86f;
                float length = (68 - t * 24) * spread * (.2f + reveal * .8f) * layerScale;
                Vector2 direction = new(side * MathF.Cos(angle), MathF.Sin(angle));
                float rotation = MathF.Atan2(direction.Y, direction.X) - MathHelper.PiOver2;
                if (side > 0) { p.Front = root; p.FrontRotation = rotation; p.FrontLength = length; }
                else { p.Back = root; p.BackRotation = rotation; p.BackLength = length; }
            }
        }
        Pose = new(pieces, input.Center, input.Gravity);
    }
}
