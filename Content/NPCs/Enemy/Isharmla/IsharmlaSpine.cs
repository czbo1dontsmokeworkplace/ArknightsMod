using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

// Arc-length history of the head's route. A portal starts a new continuous leg;
// there is deliberately no line segment between its entrance and exit.
public sealed class IsharmlaSpine
{
    private const float RetainedLength = 3700;
    private readonly List<Node> nodes = new(800);
    private struct Node
    {
        public Vector2 Position, Forward;
        public float Distance;
        public int Leg;
    }
    public readonly struct Pose
    {
        public readonly Vector2 Position, Forward;
        public readonly int Leg;
        public Pose(Vector2 position, Vector2 forward, int leg) { Position = position; Forward = forward; Leg = leg; }
    }
    public float Travel => nodes.Count == 0 ? 0 : nodes[^1].Distance;
    public bool Initialized => nodes.Count != 0;
    public void Reset(Vector2 head, Vector2 forward)
    {
        nodes.Clear(); forward = forward.SafeNormalize(Vector2.UnitX);
        for (float d = -RetainedLength; d <= 0; d += 20)
            nodes.Add(new Node { Position = head + forward * d, Forward = forward, Distance = d });
    }
    public void Record(Vector2 position, Vector2 forward)
    {
        if (!Initialized) { Reset(position, forward); return; }
        Node previous = nodes[^1];
        Vector2 delta = position - previous.Position;
        float distance = delta.Length();
        if (distance < 4) return;
        // Corrections from the network must not draw a diagonal across the arena.
        if (distance > 600) { Reset(position, forward); return; }
        int steps = Math.Max(1, (int)MathF.Ceiling(distance / 14));
        Vector2 direction = delta.SafeNormalize(forward);
        for (int i = 1; i <= steps; i++)
            nodes.Add(new Node { Position = Vector2.Lerp(previous.Position, position, i / (float)steps),
                Forward = Vector2.Lerp(previous.Forward, direction, i / (float)steps).SafeNormalize(direction),
                Distance = previous.Distance + distance * i / steps, Leg = previous.Leg });
        int remove = 0;
        while (remove + 2 < nodes.Count && nodes[remove + 1].Distance < Travel - RetainedLength) remove++;
        if (remove > 0) nodes.RemoveRange(0, remove);
    }
    public void CrossPortal(Vector2 entrance, Vector2 exit, Vector2 incoming, Vector2 outgoing)
    {
        Record(entrance, incoming);
        Node end = nodes[^1];
        nodes.Add(new Node { Position = exit, Forward = outgoing.SafeNormalize(Vector2.UnitX), Distance = end.Distance, Leg = end.Leg + 1 });
    }
    public Pose Sample(float behind)
    {
        if (!Initialized) return new Pose(Vector2.Zero, Vector2.UnitX, 0);
        float distance = Travel - behind;
        int low = 0, high = nodes.Count - 1;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (nodes[mid].Distance <= distance) low = mid; else high = mid - 1;
        }
        Node a = nodes[low];
        if (low == nodes.Count - 1 || a.Distance > distance)
            return new Pose(a.Position + a.Forward * (distance - a.Distance), a.Forward, a.Leg);
        Node b = nodes[low + 1];
        if (a.Leg != b.Leg) return new Pose(a.Position, a.Forward, a.Leg);
        float t = MathHelper.Clamp((distance - a.Distance) / Math.Max(.001f, b.Distance - a.Distance), 0, 1);
        return new Pose(Vector2.Lerp(a.Position, b.Position, t), Vector2.Lerp(a.Forward, b.Forward, t).SafeNormalize(a.Forward), a.Leg);
    }
    public float Visibility(float behind)
    {
        float distance = Travel - behind, visibility = 1;
        for (int i = 1; i < nodes.Count; i++)
            if (nodes[i].Leg != nodes[i - 1].Leg)
                visibility = Math.Min(visibility, IsharmlaRules.Smooth(Math.Abs(distance - nodes[i].Distance) / 115));
        return visibility;
    }
    public void Write(BinaryWriter writer)
    {
        // Keep portal seams exactly; downsample ordinary travel for late joiners.
        var selected = new List<Node>();
        for (int i = 0; i < nodes.Count; i++)
            if (selected.Count == 0 || i == nodes.Count - 1 || nodes[i].Distance - selected[^1].Distance >= 28 ||
                i > 0 && nodes[i].Leg != nodes[i - 1].Leg || i + 1 < nodes.Count && nodes[i].Leg != nodes[i + 1].Leg)
                selected.Add(nodes[i]);
        writer.Write((ushort)selected.Count);
        foreach (Node n in selected)
        {
            writer.Write(n.Position.X); writer.Write(n.Position.Y); writer.Write(n.Forward.ToRotation());
            writer.Write(n.Distance); writer.Write(n.Leg);
        }
    }
    public void Read(BinaryReader reader)
    {
        nodes.Clear(); int count = reader.ReadUInt16();
        if (count > 1024) throw new InvalidDataException("Invalid sea-dragon trail snapshot");
        for (int i = 0; i < count; i++)
            nodes.Add(new Node { Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                Forward = reader.ReadSingle().ToRotationVector2(), Distance = reader.ReadSingle(), Leg = reader.ReadInt32() });
    }
}
