using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Artillery;

/// <summary>Only our registered native smoke advances twice per frame, halving its lifetime.</summary>
public sealed class ArtillerySmokeLifetime : ModSystem
{
    private static readonly Dictionary<Gore, int> tracked = new();

    internal static void Track(Gore smoke) => tracked[smoke] = smoke.type;

    public override void Load() => On_Gore.Update += UpdateSmoke;

    private static void UpdateSmoke(On_Gore.orig_Update orig, Gore smoke)
    {
        bool accelerate = tracked.TryGetValue(smoke, out int type) && smoke.active && smoke.type == type;
        orig(smoke);
        if (!accelerate || !smoke.active)
        {
            tracked.Remove(smoke);
            return;
        }
        orig(smoke);
        if (!smoke.active) tracked.Remove(smoke);
    }

    public override void OnWorldUnload() => tracked.Clear();

    public override void Unload()
    {
        On_Gore.Update -= UpdateSmoke;
        tracked.Clear();
    }
}
