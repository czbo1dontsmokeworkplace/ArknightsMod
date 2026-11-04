using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalItems;

public sealed class BossWeaponOriginTooltip : GlobalItem
{
    // Explicit weapon-only membership keeps bags, summons and accessories out.
    internal static readonly Dictionary<string, string> Sources = new() {
        ["HarbingerGuillotine"] = "AACT", ["CatastropheSalvo"] = "AACT", ["ScarletMeson"] = "AACT", ["UrsusArtilleryLink"] = "AACT",
        ["WinterScar"] = "FrostNova", ["YetiSnowGrenade"] = "FrostNova", ["BlackIceCoffin"] = "FrostNova", ["YetiChime"] = "FrostNova",
        ["BasaltBreaker"] = "Pompeii", ["CalderaBellows"] = "Pompeii", ["CinderTestament"] = "Pompeii", ["BasaltBrood"] = "Pompeii",
        ["TerminusLight"] = "Evolution", ["OriginSever"] = "Evolution", ["NestOfLife"] = "Evolution", ["RushingEnd"] = "Evolution",
        ["FighterDispatchTerminal"] = "MaterialistAntagonizer", ["MiniGatling"] = "MaterialistAntagonizer", ["OrbitalLaser"] = "MaterialistAntagonizer",
        ["RotorBladeCraft"] = "MaterialistAntagonizer", ["PlasmaProjector"] = "MaterialistAntagonizer",
        ["AbyssalSpine"] = "Isharmla", ["HeavensDeluge"] = "Isharmla", ["FinalTideFlute"] = "Isharmla", ["TidebornStaff"] = "Isharmla"
    };
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (item.ModItem?.Mod != Mod || !Sources.TryGetValue(item.ModItem.Name, out string boss)) return;
        tooltips.Add(new TooltipLine(Mod, "BossWeaponOrigin", Language.GetTextValue("Mods.ArknightsMod.BossWeaponOrigin",
            Mod.Find<ModNPC>(boss).DisplayName.Value)));
    }
}
