using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// A private filter instance using Terraria's shipped screen shader: no new graphics dependency.
public sealed class EvolutionScreenFilter : ModSystem
{
    internal const string Key = "ArknightsMod:EvolutionExposure";
    public override void Load()
    {
        if (!Main.dedServ)
        {
            Filters.Scene[Key] = new Filter(new ScreenShaderData("FilterMiniTower").UseColor(Vector3.One).UseOpacity(0), EffectPriority.High);
            Filters.Scene[Key].Load();
        }
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        Filter filter = Filters.Scene[Key];
        if (filter == null) return;
        var config = ModContent.GetInstance<EvolutionCinematicConfig>();
        if (Main.gameMenu || Main.LocalPlayer.dead || config?.ScreenFilters == false || config?.ReducedEffects == true)
        { ResetFilter(); return; }
        Evolution selected = null;
        EvolutionCinematicFrame frame = default;
        float nearest = 2200 * 2200;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is not Evolution boss || !EvolutionCinematics.TryFrame(boss, out var sample)) continue;
            float distance = Vector2.DistanceSquared(Main.LocalPlayer.Center, sample.Center);
            if (distance < nearest) { selected = boss; frame = sample; nearest = distance; }
        }
        if (selected == null) { ResetFilter(); return; }
        // Activate early at zero strength so engine filter fade-in does not swallow the brief flash.
        if (!filter.IsActive()) Filters.Scene.Activate(Key, frame.Center);
        float flash = frame.ExposureFlash, dark = frame.ExposureDark;
        float proximity = 1 - EvolutionCinematicFrame.Smooth(1400, 2200, System.MathF.Sqrt(nearest));
        filter.GetShader().UseTargetPosition(frame.Center).UseColor(flash > 0 ? new Vector3(1f, .98f, .96f) : new Vector3(.025f, .006f, .018f))
            .UseOpacity(System.MathF.Max(flash, dark) * proximity);
    }
    private static void ResetFilter()
    {
        if (Main.dedServ) return;
        Filter filter = Filters.Scene[Key];
        if (filter == null) return;
        // Explicit zero removes residual exposure immediately on defeat, despawn or disabling the option.
        filter.GetShader().UseOpacity(0);
        if (filter.IsActive()) filter.Deactivate();
    }
    public override void OnWorldUnload() => ResetFilter();
    public override void Unload() => ResetFilter();
}
