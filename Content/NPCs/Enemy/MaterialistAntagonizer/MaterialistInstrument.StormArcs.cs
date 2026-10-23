using System;
using ArknightsMod.Content.Projectiles.Caster.Passenger;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistInstrument
{
    private StormArc[] stormArcs;
    private readonly record struct StormArc(Vector2 Origin, PassengerFissionArc Arc, int Strand, int Section);

    private void BuildStormArcs()
    {
        Vector2[] path = Path;
        int sections = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(Start, End) / 140f), 1, 12);
        stormArcs = new StormArc[sections * 3];
        Vector2 normal = (End - Start).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        for (int strand = 0; strand < 3; strand++)
        {
            Vector2 previous = Start;
            for (int section = 0; section < sections; section++)
            {
                float progress = (section + 1) / (float)sections;
                float index = progress * (path.Length - 1);
                int lower = Math.Min(path.Length - 2, (int)index);
                Vector2 next = Vector2.Lerp(path[lower], path[lower + 1], index - lower);
                // Shared joints keep every strand continuous; the three paths weave and reconverge.
                float offset = strand == 0 ? 0 : MathF.Sin(progress * MathHelper.Pi) *
                    (strand == 1 ? 1 : -1) * (6 + 6 * MathF.Sin(progress * 19 + Seed % 23));
                next += normal * offset;
                if (section == sections - 1) next = End;
                int seed = unchecked(Seed + strand * 15427 + section * 397);
                float width = strand == 0 ? 2.4f : 1.35f;
                var arc = new PassengerFissionArc(next - previous, seed, width, depth: strand == 0 ? 2 : 1);
                stormArcs[strand * sections + section] = new StormArc(previous, arc, strand, section);
                previous = next;
            }
        }
    }

    private void DrawStormArcs(float opacity)
    {
        if (stormArcs == null) BuildStormArcs();
        // These cached arcs belong to this strike, so cancellation also fades every visual branch.
        float age = Math.Max(1, Age - Delay + 1);
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                foreach (StormArc section in stormArcs)
                {
                    if (MaterialistVisuals.Reduced && section.Strand == 2) continue;
                    Color tint = section.Strand == 0 ? new Color(99, 211, 255) :
                        section.Strand == 1 ? new Color(146, 169, 255) : new Color(181, 246, 255);
                    float flicker = .84f + .16f * MathF.Sin(age * 1.4f + section.Section * .85f + section.Strand * 2);
                    section.Arc.Draw(Main.spriteBatch, section.Origin, tint, age, 16, .5f,
                        opacity * flicker * (section.Strand == 0 ? .92f : .58f));
                }
            }
            finally { Main.spriteBatch.End(); }
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
