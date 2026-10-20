using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal readonly record struct EvolutionRayPath(Vector2 Start, Vector2 Bend, Vector2 End, bool Bounced)
{
    internal bool Collides(Rectangle box, float width)
    {
        float hit = 0;
        return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Start, Bend, width, ref hit) ||
            Bounced && Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Bend, End, width, ref hit);
    }
}

internal static class EvolutionRefractionRules
{
    internal static EvolutionRayPath Trace(Vector2 origin, Vector2 direction, float reach, float top, float bottom)
    {
        direction = direction.SafeNormalize(Vector2.UnitY);
        Vector2 end = origin + direction * reach;
        if (Math.Abs(direction.Y) < .0001f) return new(origin, end, end, false);
        float boundary = direction.Y < 0 ? top : bottom;
        float distance = (boundary - origin.Y) / direction.Y;
        if (distance < -.01f) return new(origin, end, end, false);
        distance = Math.Max(0, distance);
        Vector2 bend = origin + direction * distance;
        Vector2 reflected = new(direction.X, -direction.Y);
        float exitBoundary = direction.Y < 0 ? bottom : top;
        float exitDistance = (exitBoundary - bend.Y) / reflected.Y;
        // 第一面墙折射，第二面墙终止；不再拿旧射程的余量截断回程。
        Vector2 exit = bend + reflected * exitDistance;
        exit.Y = exitBoundary;
        return new(origin, bend, exit, true);
    }
}

public sealed partial class EvolutionHazard
{
    internal EvolutionRayPath RayPath(Vector2 start, Vector2 direction, float reach)
    {
        if (BeamStyle == EvolutionBeamStyle.Refracted && Parent is Evolution boss && boss.CorridorActive)
            return EvolutionRefractionRules.Trace(start, direction, reach, boss.CorridorTop, boss.CorridorBottom);
        Vector2 end = start + direction * reach;
        return new(start, end, end, false);
    }
    private void DrawRayBody(EvolutionRayPath path, int age, float envelope, float width, Color color, Color core)
    {
        EvolutionLaserVisuals.Draw(path.Start, path.Bend, age, envelope, width, color, core);
        if (path.Bounced) EvolutionLaserVisuals.Draw(path.Bend, path.End, age, envelope, width, color, core);
    }
    private void DrawRayContacts(EvolutionRayPath path, float envelope, float width)
    {
        if (!path.Bounced || envelope <= .001f) return;
        Vector2 incoming = (path.Bend - path.Start).SafeNormalize(Vector2.UnitY);
        Vector2 outgoing = (path.End - path.Bend).SafeNormalize(-incoming);
        float join = Math.Max(16, Vector2.Distance(path.Start, path.Bend) * .04f);
        float tail = Math.Max(16, Vector2.Distance(path.Bend, path.End) * .04f);
        // 实际墙面上保留接触亮芯，接缝不因材质的端点柔化而出现空隙。
        EvolutionVisuals.Line(path.Bend - incoming * join, path.Bend, EvolutionVisuals.Core * (.6f * envelope), 1.7f, false);
        EvolutionVisuals.Line(path.Bend, path.Bend + outgoing * tail, EvolutionVisuals.Core * (.6f * envelope), 1.7f, false);
        EvolutionVisuals.Line(path.End - outgoing * tail, path.End, EvolutionVisuals.Core * (.6f * envelope), 1.7f, false);
        EvolutionVisuals.Glow(path.Bend, EvolutionVisuals.Blood * (.55f * envelope), new Vector2(width * 2.5f));
        EvolutionVisuals.Glow(path.End, EvolutionVisuals.Core * (.45f * envelope), new Vector2(width * 1.8f));
    }
    private void DrawRayWarning(EvolutionRayPath path, bool fan = false)
    {
        if (fan)
        {
            float warning = Math.Clamp(Age / 16f, 0, 1) * Math.Clamp((Delay - Age) / 8f, 0, 1);
            EvolutionVisuals.Line(path.Start, path.Bend, EvolutionVisuals.Core * (.7f * warning), 2);
            if (path.Bounced)
            {
                EvolutionVisuals.Line(path.Bend, path.End, EvolutionVisuals.Core * (.7f * warning), 2);
                EvolutionVisuals.Glow(path.Bend, EvolutionVisuals.Core * (.45f * warning), new Vector2(26));
            }
            return;
        }
        EvolutionVisuals.LaserWarning(path.Start, path.Bend, Age, Delay);
        if (!path.Bounced) return;
        EvolutionVisuals.LaserWarning(path.Bend, path.End, Age, Delay);
        float opacity = EvolutionRules.LaserWarningOpacity(Age, Delay);
        EvolutionVisuals.Glow(path.Bend, EvolutionVisuals.Core * (.45f * opacity), new Vector2(26));
    }
}
