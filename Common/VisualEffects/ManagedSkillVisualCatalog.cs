using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Systems.Gameplay.Skill;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Common.VisualEffects;

public enum SkillBodyMotif { Ember, Spark, Crystal, Petal, Mist, Shadow, Orbit, Slag }
public readonly record struct SkillBodyProfile(Color Color, SkillBodyMotif Motif);

// Explicit scope from the user's managed-weapon document. No class-wide fallback.
public static class ManagedSkillVisualCatalog
{
    public static readonly Dictionary<string, SkillBodyProfile> Profiles = Create();
    private static Dictionary<string, SkillBodyProfile> Create()
    {
        var entries = new Dictionary<string, SkillBodyProfile>();
        void Add(string name, uint rgb, SkillBodyMotif motif) => entries.Add(name,
            new(new Color((int)(rgb >> 16), (int)((rgb >> 8) & 255), (int)(rgb & 255)), motif));
        Add("QuartzGreatsword",0xD3B7D0,SkillBodyMotif.Crystal);
        Add("WindChimesHeavyBlade",0x93D6A0,SkillBodyMotif.Petal);
        Add("HoedererGreatsword",0xDC7652,SkillBodyMotif.Ember);
        Add("UlpianusAnchor",0x71B9CE,SkillBodyMotif.Mist);
        Add("HumusScythe",0xB59B68,SkillBodyMotif.Slag);
        Add("LaPlumaScythe",0xBC8BCF,SkillBodyMotif.Petal);
        Add("EntelechiaScythe",0xE26198,SkillBodyMotif.Petal);
        Add("OddaHammer",0xE7AE60,SkillBodyMotif.Slag);
        Add("ZimaFuryHammer",0xEC6658,SkillBodyMotif.Ember);
        Add("PepeHammer",0xF6C879,SkillBodyMotif.Orbit);
        Add("HongdouLance",0xEB3C5C,SkillBodyMotif.Spark);
        Add("GraniLance",0x5FC1F4,SkillBodyMotif.Spark);
        Add("WildManeLance",0xF68E41,SkillBodyMotif.Spark);
        Add("BagpipeSpear",0xFFC85B,SkillBodyMotif.Spark);
        Add("MousseGlove",0xDF879D,SkillBodyMotif.Petal);
        Add("AstraSword",0x8FB8FF,SkillBodyMotif.Orbit);
        Add("SurtrLaevatain",0xFF6846,SkillBodyMotif.Ember);
        Add("MelanthasSword",0xB99ED5,SkillBodyMotif.Shadow);
        Add("MataimaruGlaive",0xDD978E,SkillBodyMotif.Petal);
        Add("FrankaRapier",0xFFB55C,SkillBodyMotif.Slag);
        Add("NearlRadiantSword",0xFFE294,SkillBodyMotif.Orbit);
        Add("GravelDualBlades",0xD9A5A6,SkillBodyMotif.Shadow);
        Add("RedDagger",0xED4358,SkillBodyMotif.Shadow);
        Add("PhantomDaggers",0x6FC8C4,SkillBodyMotif.Shadow);
        Add("UtageKatana",0xCA79BC,SkillBodyMotif.Petal);
        Add("HellagurOdachi",0xDBB478,SkillBodyMotif.Slag);
        Add("SpecterBoneSaw",0xB0C5C5,SkillBodyMotif.Mist);
        Add("BlazeGreatsword",0xF87839,SkillBodyMotif.Ember);
        Add("GavialChainsaw",0x83D399,SkillBodyMotif.Spark);
        Add("IndigoStaff",0x779FEA,SkillBodyMotif.Orbit);
        Add("HarmonieStaff",0x7BD4D2,SkillBodyMotif.Mist);
        Add("EbenholzStaff",0xCDB9FF,SkillBodyMotif.Crystal);
        Add("PuddingStaff",0xC0ADFF,SkillBodyMotif.Spark);
        Add("LeiziStaff",0xB7D8FF,SkillBodyMotif.Spark);
        Add("PassengerConductor",0xF9CF80,SkillBodyMotif.Spark);
        Add("ClickStaff",0xF4CA70,SkillBodyMotif.Orbit);
        Add("RockrockStaff",0x7CE7C0,SkillBodyMotif.Spark);
        Add("GoldenglowWand",0xFFACD0,SkillBodyMotif.Spark);
        Add("DeepcolorSketch",0x9AE3CE,SkillBodyMotif.Mist);
        Add("SceneCamera",0xA2D5F3,SkillBodyMotif.Orbit);
        Add("MagallanController",0xB0E9F6,SkillBodyMotif.Crystal);
        Add("Lava_Dagger",0xF7A659,SkillBodyMotif.Ember);
        Add("GitanoOrb",0xAF8DE9,SkillBodyMotif.Orbit);
        Add("LeonhardtStaff",0xF8BB78,SkillBodyMotif.Crystal);
        Add("SkyfireStaff",0xFF845A,SkillBodyMotif.Ember);
        Add("MostimaStaff",0x94B5FF,SkillBodyMotif.Orbit);
        Add("CorroserumFlamethrower",0xA8D761,SkillBodyMotif.Mist);
        Add("IfritFlamethrower",0xFF9A49,SkillBodyMotif.Ember);
        Add("AphrissaProjector",0xDF9CFF,SkillBodyMotif.Crystal);
        Add("BeeswaxStaff",0xE8C68C,SkillBodyMotif.Slag);
        Add("CarnelianStaff",0xE57960,SkillBodyMotif.Slag);
        Add("LinStaff",0xEEACDC,SkillBodyMotif.Crystal);
        Add("CatapultLauncher",0xF3BB70,SkillBodyMotif.Slag);
        Add("MeteoriteCannon",0xECA66B,SkillBodyMotif.Ember);
        Add("FiammettaHandCannon",0xFF8A49,SkillBodyMotif.Ember);
        Add("AmbrielRifle",0xF2A7CF,SkillBodyMotif.Mist);
        Add("AndreanaRifle",0xA497CF,SkillBodyMotif.Shadow);
        Add("FirewatchRifle",0xB4D5AF,SkillBodyMotif.Mist);
        Add("TyphonBow",0xA9C7FF,SkillBodyMotif.Crystal);
        Add("PineconeNailgun",0xEACD8B,SkillBodyMotif.Slag);
        Add("ExecutorShotgun",0xACBBCF,SkillBodyMotif.Spark);
        Add("ChalterWatergun",0x7CD8F1,SkillBodyMotif.Mist);
        Add("KroosCrossbow",0xE9BF7C,SkillBodyMotif.Petal);
        Add("KroosAlterCrossbow",0xEEB77D,SkillBodyMotif.Petal);
        Add("MelaniteCrossbow",0xB9A6E4,SkillBodyMotif.Crystal);
        Add("ProvenceCrossbow",0xBB94D7,SkillBodyMotif.Shadow);
        Add("SchwarzBow",0xEBB87E,SkillBodyMotif.Slag);
        Add("GreyThroatCrossbow",0xB5DAE4,SkillBodyMotif.Petal);
        Add("BluePoisonCrossbow",0x82CDBE,SkillBodyMotif.Mist);
        Add("PozemkaCrossbow",0xF4A0BE,SkillBodyMotif.Petal);
        Add("HoolheyakWindArts",0x8BD6C9,SkillBodyMotif.Orbit);
        Add("EthanYoyo",0x68D3AD,SkillBodyMotif.Shadow);
        Add("NecrassScepter",0xB789F2,SkillBodyMotif.Ember);
        // New defender route referred to in the same document, including Horn.
        Add("NoirShield",0x98AFC1,SkillBodyMotif.Slag);
        Add("BeagleWeapon",0xE0BA8D,SkillBodyMotif.Slag);
        Add("CardiWeapon",0xD7A9B2,SkillBodyMotif.Petal);
        Add("CuoraWeapon",0xB3D59D,SkillBodyMotif.Orbit);
        Add("CroissantHammer",0xEBBD77,SkillBodyMotif.Spark);
        Add("NianWeapon",0xE9B896,SkillBodyMotif.Ember);
        Add("DN_Weapon",0xB8A0EA,SkillBodyMotif.Spark);
        Add("LiskarmPistol",0x9ACFEF,SkillBodyMotif.Spark);
        Add("Vulcan_Weapon",0xF2AB68,SkillBodyMotif.Slag);
        Add("MudrockHammer",0xD3B7C7,SkillBodyMotif.Crystal);
        Add("EunectesAxe",0xA4D89D,SkillBodyMotif.Spark);
        Add("HornGrenadeLauncher",0xF3CE8A,SkillBodyMotif.Ember);
        Add("FirewhistleLauncher",0xF2A06F,SkillBodyMotif.Ember);
        foreach (MedicProfile medic in MedicCatalog.Profiles)
            if (medic.Key != "WarfarinStaff") entries.Add(medic.Key, new(medic.Body,
                medic.Branch == MedicBranch.Therapist ? SkillBodyMotif.Mist
                : medic.Branch == MedicBranch.Chain ? SkillBodyMotif.Orbit
                : medic.Key == "Mon3trCore" ? SkillBodyMotif.Crystal : SkillBodyMotif.Petal));
        // Covered by the existing specialized phasing/player-body emitter, not doubled.
        Add("MyrtleStandard",0x91EB87,SkillBodyMotif.Mist);
        Add("ElysiumStandard",0x7DCDFF,SkillBodyMotif.Mist);
        Add("JacintStandard",0xF0A0C3,SkillBodyMotif.Mist);
        Add("SaileachStandard",0xFFDA78,SkillBodyMotif.Mist);
        return entries;
    }
    public static float Duration(string name, SkillData skill) => name == "SceneCamera" && skill?.Key.Index == 1
        ? 20 : skill?.CurrentLevelData.ActiveTime ?? 0;
    public static bool Eligible(string name, SkillData skill) => Profiles.ContainsKey(name) && skill != null
        && !skill.AutoTrigger && !skill.IsPermanent && Duration(name, skill) > 0
        && skill.ChargeType != SkillChargeType.None
        // Red's five-second deployment is triggered by switching/dashing/landing,
        // not by manually activating a timed skill; keep RedDeploymentAura unchanged.
        && name != "RedDagger"
        // Scene S1 is permanent camouflage; its CSV is not the authority for this behavior.
        && !(name == "SceneCamera" && skill.Key.Index == 0);
    public static int Interval(float seconds) => seconds <= 2 ? 3 : seconds <= 8 ? 6 : seconds <= 20 ? 12 : seconds <= 40 ? 20 : 30;
    public static int Burst(float seconds) => seconds <= 2 ? 12 : seconds <= 8 ? 9 : seconds <= 20 ? 6 : 3;
    public static SkillBodyProfile Profile(string name, int skill)
    {
        var profile = Profiles[name];
        // Skill tiers keep the authored hue; higher slots add a small brighter core.
        return profile with { Color = Color.Lerp(profile.Color, new Color(240, 238, 218), Math.Clamp(skill, 0, 2) * .07f) };
    }
}
