using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Medic;

public enum MedicBranch { Physician, Therapist, MultiTarget, Chain, Wandering }

// One balance sheet for the complete recipe tree. Times are real game ticks, never AI sub-updates.
public sealed record MedicProfile(string Key, MedicBranch Branch, int Tier, int Progression,
    int Interval, int Healing, float Speed, float Gravity, Color Body, Color Core,
    int Parent = -1, int StackLimit = 0, float RegenPerStack = 0f, int StackDuration = 0,
    int CleanseLimit = 0, float FieldRadius = 0f)
{
    public int ArmingDelay { get; init; } = 10;
    public int PatientContactSize => Branch == MedicBranch.MultiTarget ? 64 : 32;
    public float ResistanceDurationMultiplier { get; init; } = 2f;
    public float ResistanceReduction { get; init; } = 1f;
    // Slow field deployment uses a short hand animation and a separate player cooldown.
    public int CastAnimation => Branch == MedicBranch.MultiTarget ? 24 : Interval;
    public float ArtRadius => (10f + Tier * .75f) * (Branch == MedicBranch.MultiTarget ? 2f : 1f);
    public int FieldDuration => Math.Max(600, Interval * 3 + 60);
}

public static class MedicCatalog
{
    public static readonly MedicProfile[] Profiles =
    [
        new("AnselStaff", MedicBranch.Physician, 0, 0, 96, 14, 6.2f, .13f, new(244, 150, 180), new(255, 235, 226)),
        new("MyrrhStaff", MedicBranch.Physician, 1, 1, 90, 23, 6.5f, .135f, new(231, 172, 84), new(255, 241, 195), 0),
        new("SussurroStaff", MedicBranch.Physician, 2, 2, 84, 38, 6.8f, .14f, new(58, 202, 130), new(222, 255, 219), 1),
        new("SilenceStaff", MedicBranch.Physician, 3, 3, 80, 50, 7f, .14f, new(241, 153, 75), new(255, 247, 220), 2),
        new("TuyeStaff", MedicBranch.Physician, 4, 5, 78, 66, 7.2f, .145f, new(219, 179, 96), new(205, 250, 247), 3),
        new("ShiningStaff", MedicBranch.Physician, 5, 6, 72, 88, 7.3f, .15f, new(161, 189, 234), new(255, 255, 247), 4),
        new("PurestreamStaff", MedicBranch.Therapist, 1, 1, 96, 14, 5.6f, .10f, new(82, 198, 217), new(226, 255, 244), 1),
        new("CeylonStaff", MedicBranch.Therapist, 2, 2, 90, 24, 5.8f, .105f, new(102, 184, 230), new(241, 247, 255), 6),
        new("WhisperainStaff", MedicBranch.Therapist, 4, 5, 84, 40, 6.1f, .11f, new(136, 172, 219), new(234, 242, 255), 7),
        new("LumenStaff", MedicBranch.Therapist, 5, 6, 78, 54, 6.3f, .115f, new(84, 173, 223), new(255, 236, 174), 8),
        new("PerfumerStaff", MedicBranch.MultiTarget, 1, 1, 240, 25, 4.1f, .17f, new(169, 199, 115), new(255, 242, 189), 1, FieldRadius: 100),
        new("BreezeStaff", MedicBranch.MultiTarget, 2, 2, 252, 42, 4.3f, .18f, new(110, 205, 170), new(231, 255, 223), 10, FieldRadius: 126),
        new("PtilopsisStaff", MedicBranch.MultiTarget, 4, 5, 264, 70, 4.5f, .19f, new(164, 185, 219), new(255, 248, 221), 11, FieldRadius: 154),
        new("NightingaleStaff", MedicBranch.MultiTarget, 5, 6, 276, 96, 4.6f, .20f, new(145, 164, 233), new(255, 247, 222), 12, FieldRadius: 178),
        new("PapyrusStaff", MedicBranch.Chain, 3, 3, 90, 18, 6.2f, .13f, new(210, 180, 105), new(255, 250, 217), 2, 8, .8f, 1440),
        new("PaprikaStaff", MedicBranch.Chain, 4, 5, 84, 26, 6.5f, .135f, new(227, 112, 139), new(255, 226, 215), 14, 14, 1.2f, 2100),
        new("Mon3trCore", MedicBranch.Chain, 5, 6, 78, 34, 6.7f, .14f, new(60, 195, 188), new(222, 255, 235), 15, 20, 1.8f, 3000),
        new("ChestnutStaff", MedicBranch.Wandering, 3, 3, 96, 17, 6f, .13f, new(146, 189, 104), new(255, 238, 175), 2, CleanseLimit: 1),
        new("HoneyberryStaff", MedicBranch.Wandering, 3, 4, 90, 25, 6.3f, .135f, new(184, 136, 213), new(255, 230, 195), 17, CleanseLimit: 2),
        new("MulberryStaff", MedicBranch.Wandering, 4, 5, 84, 34, 6.5f, .14f, new(119, 181, 214), new(245, 248, 255), 18, CleanseLimit: 3),
        new("EyjafjallaAlterStaff", MedicBranch.Wandering, 5, 6, 78, 46, 6.7f, .145f, new(242, 160, 181), new(255, 244, 215), 19, CleanseLimit: 6),
        // Kept for old inventories; the new tree begins with Ansel, and no recipe consumes Warfarin.
        new("WarfarinStaff", MedicBranch.Physician, 1, 0, 96, 16, 6.2f, .13f, new(205, 66, 93), new(255, 212, 202))
    ];

    public static MedicProfile Get(int id) => Profiles[Math.Clamp(id, 0, Profiles.Length - 1)];
    public static int Id(string key) => Array.FindIndex(Profiles, profile => profile.Key == key);
}
