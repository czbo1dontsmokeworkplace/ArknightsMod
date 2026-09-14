using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

// Lightning Strike's 1.4.5.8 weapon geometry, adapted to explicit endpoints and 1.4.4.9.
// Source: WeaponReference/LightningStrike/LightningGenerator.cs (1.4.5.8).
// Tile/liquid interactions are handled by GoldenglowLightningStrike using this same main path.
internal sealed class GoldenglowLightningGeometry
{
    internal sealed class Bolt
    {
        internal Vector2[] Points;
        internal Vector2[] Normals;
        internal float StartProgress;
        internal float EndProgress;
        internal int Depth;
    }

    internal readonly List<Bolt> Bolts = new(7);

    internal GoldenglowLightningGeometry(uint seed, Vector2 start, Vector2 end)
    {
        float length = Vector2.Distance(start, end);
        if (!float.IsFinite(length) || length < 8f || length > 2400f)
            return;
        Build(seed, 0, start, end, 0.9f, 8f, 0f, 1f);
    }

    private void Build(uint seed, int depth, Vector2 start, Vector2 target, float rotationStrength,
        float stepSize, float rangeStart, float rangeEnd)
    {
        Random32 random = new(seed);
        float rotation = 0f;
        float[] layers = new float[4];
        Vector2 point = start;
        Vector2 axis = target - start;
        float length = axis.Length();
        axis /= length;
        Vector2 normal = new(axis.Y, -axis.X);
        int limit = (int)Math.Max(length * 2f / stepSize, 1f);
        Vector2[] points = new Vector2[limit];
        int forks = 0;
        int i;
        for (i = 0; i < limit; i++)
        {
            points[i] = point;
            Vector2 remaining = target - point;
            float forwardDistance = Vector2.Dot(remaining, axis);
            if (forwardDistance < stepSize)
                break;
            float progress = MathHelper.Clamp(1f - forwardDistance / length, 0f, 1f);
            remaining /= remaining.Length();
            float sideways = -Vector2.Dot(remaining, normal);
            float tolerance = Math.Max(0.01f, Math.Min(progress, 1f - progress) * 10f);
            float correction = MathHelper.Clamp(sideways / tolerance, -1f, 1f);
            if (PickLayer(random.NextDouble(), 0.5f, out int layer))
            {
                float strength = rotationStrength;
                for (int higher = 3; higher > layer; higher--)
                    strength /= 1.5f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                noise += (correction - noise * Math.Abs(correction)) / 2f;
                float next = noise * strength;
                float delta = next - layers[layer];
                rotation += delta;
                layers[layer] = next;
                if (layer == 3)
                {
                    float roll = (float)random.NextDouble();
                    float chance = Remap(forks, 0f, 2f, 1f, 0f);
                    float reflectedAngle = rotation - delta * 1.4f;
                    if (Math.Abs(delta) >= rotationStrength * 0.65f && progress >= 0.3f &&
                        progress <= 0.8f && depth < 2 && roll < chance &&
                        Math.Abs(reflectedAngle) < MathHelper.Pi * 4f / 9f)
                    {
                        forks++;
                        float reach = (1f - progress) * 0.8f;
                        Vector2 forkTarget = point + remaining.RotatedBy(reflectedAngle) * length * reach;
                        Build(unchecked(random.State + 1), depth + 1, point, forkTarget,
                            rotationStrength * 0.9f, stepSize * 0.8f,
                            MathHelper.Lerp(rangeStart, rangeEnd, progress),
                            MathHelper.Lerp(rangeStart, rangeEnd, progress + reach));
                    }
                }
            }
            float resetChance = Remap(progress, 0.8f, 1f, 0f, 1f) +
                Remap(Math.Abs(correction), 0.5f, 1f, 0f, 1f);
            if (PickLayer(random.NextDouble(), resetChance, out int resetLayer))
            {
                resetLayer = 3 - resetLayer;
                rotation -= layers[resetLayer];
                layers[resetLayer] = 0f;
            }
            point += remaining.RotatedBy(rotation) * stepSize;
        }
        if (i < limit)
            Array.Resize(ref points, i + 1);
        if (depth > 0 && i <= 2 || points.Length < 2)
            return;
        Bolts.Add(new Bolt
        {
            Points = points, Normals = CalculateNormals(points), Depth = depth,
            StartProgress = rangeStart, EndProgress = rangeEnd
        });
    }

    private static bool PickLayer(double value, float chance, out int layer)
    {
        for (layer = 0; layer < 4; layer++)
        {
            if (value >= (double)(1f - chance))
                return true;
            value /= chance;
        }
        return false;
    }

    private static Vector2[] CalculateNormals(Vector2[] points)
    {
        float[] rotations = new float[points.Length];
        float previous = (points[0] - points[1]).ToRotation();
        rotations[0] = previous;
        for (int i = 1; i < points.Length - 1; i++)
        {
            float next = (points[i] - points[i + 1]).ToRotation();
            rotations[i] = previous + MathHelper.WrapAngle(next - previous) / 2f;
            previous = next;
        }
        rotations[^1] = previous;
        previous = rotations[0];
        for (int i = 1; i < rotations.Length - 1; i++)
        {
            float current = rotations[i];
            rotations[i] += (MathHelper.WrapAngle(previous - current) +
                MathHelper.WrapAngle(rotations[i + 1] - current)) / 2f;
            previous = current;
        }
        Vector2[] normals = new Vector2[points.Length];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = (rotations[i] + MathHelper.PiOver2).ToRotationVector2();
        return normals;
    }

    internal static float Remap(float value, float from, float to, float low, float high) =>
        MathHelper.Lerp(low, high, MathHelper.Clamp((value - from) / (to - from), 0f, 1f));

    // The original 32-bit generator is kept separate from Terraria's mutable global RNG.
    private struct Random32
    {
        internal uint State;
        internal Random32(uint seed) => State = seed;
        internal double NextDouble()
        {
            State = unchecked(State * 2438952949u + 1u);
            return State / 4294967296.0;
        }
    }
}

