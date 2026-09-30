using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed class EvolutionCinematicConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool ReducedEffects { get; set; }

    [DefaultValue(true)]
    public bool ScreenShake { get; set; } = true;

    [DefaultValue(true)]
    public bool ScreenFilters { get; set; } = true;
}
