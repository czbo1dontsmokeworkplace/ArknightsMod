using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed class EvolutionEnrageScreenSystem : ModSystem
{
    private const string Key = "ArknightsMod:EvolutionEnrage";
    private float strength;
    public override void Load()
    {
        if (Main.dedServ) return;
        Filters.Scene[Key] = new Filter(new ScreenShaderData("FilterMiniTower").UseColor(.5f, .015f, .035f).UseOpacity(0), EffectPriority.Medium);
        Filters.Scene[Key].Load();
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        var config = ModContent.GetInstance<EvolutionCinematicConfig>();
        if (Main.gameMenu || Main.LocalPlayer.dead || config?.ScreenFilters == false)
        { Reset(); return; }
        Evolution selected = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is Evolution boss && boss.Phase == 5 && boss.CycleEnraged && npc.DistanceSQ(Main.LocalPlayer.Center) < 7000 * 7000)
            { selected = boss; break; }
        float target = selected == null ? 0 : config?.ReducedEffects == true ? .06f : .14f;
        strength = target > strength ? Math.Min(target, strength + .14f / 60) : Math.Max(target, strength - .14f / 30);
        Filter filter = Filters.Scene[Key]; if (filter == null) return;
        filter.GetShader().UseOpacity(strength);
        if (selected != null && !filter.IsActive()) Filters.Scene.Activate(Key, selected.NPC.Center);
        if (strength <= .0001f && filter.IsActive()) filter.Deactivate();
    }
    private void Reset()
    {
        strength = 0; if (Main.dedServ) return;
        Filter filter = Filters.Scene[Key]; if (filter == null) return;
        filter.GetShader().UseOpacity(0); if (filter.IsActive()) filter.Deactivate();
    }
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();
}
