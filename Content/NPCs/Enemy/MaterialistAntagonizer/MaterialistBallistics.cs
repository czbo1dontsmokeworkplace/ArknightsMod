using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

// Local ballistic streaks and impact fragments. Kingsbane/AuricBullet and P90Round
// informed the parallel-line density only; no Calamity particle, AI, debuff or asset is used here.
internal static class MaterialistBallistics
{
    private struct Fragment { internal Vector2 Position, Velocity; internal Color Color; internal int Age, Life; internal bool Chip; internal float Scale; }
    private struct Mark { internal Vector2 Position, Normal; internal int Age; internal bool Heavy; }
    private struct Tracer { internal Vector2 Position, Drift; internal float Angle, Length; internal int Age; internal Color Color; }
    private static readonly Fragment[] fragments = new Fragment[384];
    private static readonly Mark[] marks = new Mark[48];
    private static readonly Tracer[] tracers = new Tracer[2048];
    private static int tracerCursor;
    private static int fragmentCursor, markCursor;
    private static readonly ulong[] muzzleFrames = new ulong[Main.maxNPCs];
    internal static void Reset() { Array.Clear(fragments); Array.Clear(marks); Array.Clear(tracers); Array.Clear(muzzleFrames); fragmentCursor = markCursor = tracerCursor = 0; }
    internal static void AirWake(NPC npc, bool firing)
    {
        if (Main.dedServ || Main.gameMenu || npc.Distance(Main.LocalPlayer.Center) > 2400) return;
        for (int wing = -1; wing <= 1; wing += 2)
        {
            Vector2 point = npc.Center + new Vector2(-npc.spriteDirection * 46, wing * 20).RotatedBy(npc.rotation);
            Dust vapor = Dust.NewDustPerfect(point, DustID.Smoke, new Vector2(-npc.spriteDirection * .9f, -.25f) + npc.velocity * .08f,
                155, new Color(162, 170, 170), firing ? 1.1f : .8f);
            vapor.noGravity = true;
        }
    }
    internal static void Muzzle(Vector2 point, Vector2 velocity, int source)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(point, Main.LocalPlayer.Center) > 2400 * 2400) return;
        if (source >= 0 && source < muzzleFrames.Length)
        {
            ulong now = Main.GameUpdateCount + 10;
            if (now - muzzleFrames[source] < 6) return;
            muzzleFrames[source] = now;
        }
        Vector2 axis = velocity.SafeNormalize(Vector2.UnitY), normal = axis.RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i < (MaterialistVisuals.Reduced ? 1 : 2); i++)
            fragments[fragmentCursor++ % fragments.Length] = new Fragment {
                Position = point - axis * 13, Velocity = normal * Main.rand.NextFloat(2, 4) - axis * Main.rand.NextFloat(1, 3),
                Color = new Color(224, 177, 76), Age = 1, Life = 22, Chip = true, Scale = 1.2f
            };
        if (!MaterialistVisuals.Reduced)
        {
            Dust smoke = Dust.NewDustPerfect(point + axis * 8, DustID.Smoke, axis * .7f - Vector2.UnitY * .4f, 140,
                new Color(147, 134, 115), .85f);
            smoke.noGravity = true;
        }
    }
    internal static void Wake(Projectile p, bool dense = false)
    {
        if (Main.gameMenu || Vector2.DistanceSquared(p.Center, Main.LocalPlayer.Center) > 2400 * 2400) return;
        Vector2 normal = p.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        float travel = dense && p.ModProjectile is MaterialistHazard h ? Vector2.Distance(p.Center, h.Anchor) : float.MaxValue;
        for (int i = 0; i < (MaterialistVisuals.Reduced ? 1 : dense ? 3 : 2); i++)
            tracers[tracerCursor++ % tracers.Length] = new Tracer {
                Position = p.Center + normal * Main.rand.NextFloat(-15, 15), Drift = -p.velocity * Main.rand.NextFloat(.1f, .35f),
                Angle = p.velocity.ToRotation(), Length = Math.Min(travel, dense ? Main.rand.NextFloat(65, 145) : Main.rand.NextFloat(24, 68)), Age = 1,
                Color = Color.Lerp(new Color(184, 107, 24), new Color(255, 232, 143), Main.rand.NextFloat())
            };
    }
    internal static void Impact(Vector2 point, Vector2 normal, bool ground, bool heavy = false)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(point, Main.LocalPlayer.Center) > 2400 * 2400) return;
        marks[markCursor++ % marks.Length] = new Mark { Position = point, Normal = normal, Age = 1, Heavy = heavy };
        Vector2 tangent = normal.RotatedBy(MathHelper.PiOver2);
        int count = MaterialistVisuals.Reduced ? 4 : heavy && ground ? 16 : ground ? 10 : 6;
        for (int i = 0; i < count; i++)
        {
            bool chip = ground && i % 4 == 0;
            fragments[fragmentCursor++ % fragments.Length] = new Fragment {
                Position = point, Velocity = normal * Main.rand.NextFloat(2, heavy ? 10 : 7) + tangent * Main.rand.NextFloat(heavy ? -10 : -7, heavy ? 10 : 7),
                Color = chip ? new Color(155, 144, 124) : i % 3 == 0 ? new Color(255, 245, 196) : new Color(255, 184, 44),
                Age = 1, Life = Main.rand.Next(10, heavy ? 25 : 20), Chip = chip, Scale = heavy ? 1.65f : 1
            };
        }
        if (ground && !MaterialistVisuals.Reduced && Main.rand.NextBool(heavy ? 2 : 3))
            Dust.NewDustPerfect(point, DustID.Smoke, normal * (heavy ? 1.2f : .7f), 140, new Color(115, 105, 87), heavy ? 1.25f : .55f);
    }
    internal static void Update()
    {
        for (int i = 0; i < tracers.Length; i++)
        {
            ref Tracer t = ref tracers[i];
            if (t.Age == 0) continue;
            if (++t.Age > 5) { t = default; continue; }
            t.Position += t.Drift; t.Drift *= .9f;
        }
        for (int i = 0; i < fragments.Length; i++)
        {
            ref Fragment f = ref fragments[i];
            if (f.Age == 0) continue;
            if (++f.Age > f.Life) { f = default; continue; }
            f.Velocity *= .92f;
            f.Velocity.Y += f.Chip ? .23f : .11f;
            f.Position += f.Velocity;
        }
        for (int i = 0; i < marks.Length; i++)
            if (marks[i].Age > 0 && ++marks[i].Age > 13) marks[i] = default;
    }
    internal static void DrawImpacts()
    {
        foreach (Tracer t in tracers)
            if (t.Age > 0)
            {
                float fade = 1 - t.Age / 6f;
                Vector2 tail = t.Position - t.Angle.ToRotationVector2() * t.Length;
                MaterialistVisuals.Line(tail, t.Position, new Color(255, 143, 30, 0) * (fade * .22f), 6);
                MaterialistVisuals.Line(tail, t.Position, (t.Color with { A = 0 }) * fade, 2.9f);
                MaterialistVisuals.Line(Vector2.Lerp(tail, t.Position, .4f), t.Position, new Color(255, 246, 207, 0) * (fade * .55f), 1.1f);
            }
        foreach (Mark m in marks)
        {
            if (m.Age == 0) continue;
            float fade = 1 - m.Age / 14f;
            Vector2 tangent = m.Normal.RotatedBy(MathHelper.PiOver2);
            MaterialistVisuals.Line(m.Position - tangent * (4 + m.Age * 1.6f), m.Position + tangent * (4 + m.Age * 1.6f),
                new Color(255, 195, 75, 0) * fade, (m.Heavy ? 4 : 2) * fade + .5f);
            MaterialistVisuals.Glow(m.Position, new Color(255, 215, 131) * (fade * (m.Heavy ? .8f : .55f)),
                new Vector2((m.Heavy ? 60 : 32) * fade + 6, m.Heavy ? 23 : 12), tangent.ToRotation());
        }
        foreach (Fragment f in fragments)
        {
            if (f.Age == 0) continue;
            float fade = 1 - f.Age / (float)f.Life;
            Vector2 tail = f.Position - f.Velocity.SafeNormalize(Vector2.UnitY) * (f.Chip ? 2 : Math.Min(15, f.Velocity.Length() * 2)) * f.Scale;
            MaterialistVisuals.Line(tail, f.Position, (f.Chip ? f.Color : f.Color with { A = 0 }) * fade, (f.Chip ? 2.5f : 1.3f) * f.Scale);
        }
    }
    private static float RandomLane(int identity, int lane)
    {
        uint n = unchecked((uint)(identity * 747796405 + lane * 2891336453L));
        n = (n ^ (n >> 16)) * 2246822519u;
        return (n & 0xffff) / 65535f;
    }
    internal static void DrawRound(MaterialistHazard h)
    {
        Projectile p = h.Projectile;
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY), normal = axis.RotatedBy(MathHelper.PiOver2);
        // Each packet has parallel strokes with independent offsets and lengths, never bent homing ribbons.
        bool dense = h.DenseBallistics;
        int lines = MaterialistVisuals.Reduced ? dense ? 10 : 4 : dense ? 28 : 9;
        float travel = Vector2.Distance(p.Center, h.Anchor);
        for (int i = 0; i < lines; i++)
        {
            float a = RandomLane(p.identity, i * 3), b = RandomLane(p.identity, i * 3 + 1), c = RandomLane(p.identity, i * 3 + 2);
            float offset = dense ? Math.Min(travel, b * 110) : b * 72;
            Vector2 head = p.Center - axis * offset + normal * ((a - .5f) * (dense ? 25 : 18));
            // At 240 px/tick and one shot per tick, 250-360 px packets overlap along the firing lane.
            // Clamp to the travelled path so no bright tail appears behind the gun before launch.
            float length = dense ? Math.Min(Math.Max(0, travel - offset), 140 + c * 110) : 16 + c * 45;
            Color color = Color.Lerp(new Color(178, 100, 20), new Color(255, 229, 132), c) with { A = 0 };
            MaterialistVisuals.Line(head - axis * length, head, color * ((dense ? .32f : .45f) + (dense ? .32f : .45f) * b), 1.8f + c * 1.2f);
        }
        MaterialistVisuals.Line(p.Center - axis * 30, p.Center + axis * 3, new Color(255, 246, 207, 0), 3.4f);
        MaterialistVisuals.Glow(p.Center - axis * 15, new Color(255, 183, 49) * .4f, new Vector2(56, 17), p.rotation);
    }
}
