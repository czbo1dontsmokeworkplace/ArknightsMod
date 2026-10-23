using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static class MaterialistImpactJets
{
    private static ulong frame;
    private static int emitted;

    internal static void Emit(Vector2 point, Vector2 incoming)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(point, Main.LocalPlayer.Center) > 2200 * 2200) return;
        if (frame != Main.GameUpdateCount) { frame = Main.GameUpdateCount; emitted = 0; }
        int count = MaterialistVisuals.Reduced ? 4 : Main.rand.Next(16, 23);
        Vector2 reverse = -incoming.SafeNormalize(Vector2.UnitY);
        for (int i = 0; i < count && emitted < (MaterialistVisuals.Reduced ? 12 : 64); i++, emitted++)
        {
            bool smoke = i % 2 == 0;
            Vector2 direction = reverse.RotatedBy(Main.rand.NextFloat(-1.15f, 1.15f));
            Dust dust = Dust.NewDustPerfect(point + Main.rand.NextVector2Circular(5, 5),
                smoke ? DustID.Smoke : DustID.Ash, direction * Main.rand.NextFloat(smoke ? 1.5f : 3, smoke ? 7 : 12),
                smoke ? 155 : 65, smoke ? new Color(126, 116, 100) : new Color(243, 199, 115),
                Main.rand.NextFloat(smoke ? .8f : .65f, smoke ? 1.5f : 1.2f));
            dust.noGravity = true;
        }
    }
}
