using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;

namespace ArknightsMod.Common.GlobalItems;

// Only the documented weapons receive traits. Medical and Defender weapons also show their parent profession.
public sealed class OperatorBranchTraits : GlobalItem
{
    internal static string Branch(Item item) => item.ModItem?.Mod.Name == "ArknightsMod"
        ? global::ArknightsMod.Content.Items.Weapons.Defender.DefenderRouteCatalog.Find(item)?.Branch ?? (item.ModItem.Name switch
    {
        "AnselStaff" or "HibiscusStaff" or "MyrrhStaff" or "WarfarinStaff" or "SussurroStaff" or "SilenceStaff"
            or "TuyeStaff" or "ShiningStaff" => "Medic",
        "PurestreamStaff" or "CeylonStaff" or "NowellStaff" or "WhisperainStaff" or "LumenStaff" => "MedicTherapist",
        "PerfumerStaff" or "BreezeStaff" or "PtilopsisStaff" or "NightingaleStaff" => "MedicMultiTarget",
        "PapyrusStaff" or "PaprikaStaff" or "Mon3trCore" => "MedicChain",
        "ChestnutStaff" or "HoneyberryStaff" or "MulberryStaff" or "EyjafjallaAlterStaff" => "MedicWandering",
        "HongdouLance" or "GraniLance" or "WildManeLance" or "BagpipeSpear" => "Charger",
        "MyrtleStandard" or "ElysiumStandard" or "JacintStandard" or "SaileachStandard" => "StandardBearer",
        "CatapultLauncher" or "MeteoriteCannon" or "FiammettaHandCannon" => "Artilleryman",
        "UtageKatana" or "HellagurOdachi" => "Musha",
        "MelanthasSword" or "MataimaruGlaive" or "FrankaRapier" or "NearlRadiantSword" => "Dreadnought",
        "MousseGlove" or "AstraSword" or "SurtrLaevatain" => "ArtsFighter",
        "CutterBlades" or "BibeakSword" or "IreneSaber" => "Swordmaster",
        "MidnightSword" or "AreneSword" or "LapplandSwords" or "FrostleafAxe" or "SilverAshWeapon" => "Lord",
        "ClickStaff" or "RockrockStaff" or "GoldenglowWand" => "MechAccord",
        "IndigoStaff" or "HarmonieStaff" or "EbenholzStaff" => "Mystic",
        "BeeswaxStaff" or "CarnelianStaff" or "LinStaff" => "Phalanx",
        "PineconeNailgun" or "ExecutorShotgun" or "ChalterWatergun" => "Spreadshooter",
        "GravelDualBlades" or "RedDagger" or "PhantomDaggers" => "Executor",
        "EthanYoyo" => "Ambusher",
        "SpecterBoneSaw" or "BlazeGreatsword" or "GavialChainsaw" => "Centurion",
        "QuartzGreatsword" or "WindChimesHeavyBlade" or "HoedererGreatsword" or "UlpianusAnchor" => "Crusher",
        "HumusScythe" or "LaPlumaScythe" or "EntelechiaScythe" => "Reaper",
        "OddaHammer" or "ZimaFuryHammer" or "PepeHammer" => "Earthshaker",
        "PuddingStaff" or "LeiziStaff" or "PassengerConductor" => "ChainCaster",
        "CorroserumFlamethrower" or "IfritFlamethrower" or "AphrissaProjector" => "BlastCaster",
        "Lava_Dagger" or "GitanoOrb" or "LeonhardtStaff" or "SkyfireStaff" or "MostimaStaff" => "SplashCaster",
        "AmbrielRifle" or "AndreanaRifle" or "FirewatchRifle" or "TyphonBow" => "Deadeye",
        "NecrassScepter" => "PrimalCaster",
        "DurinWand" or "StewardStaff" or "HazeMagicBook" or "NightmareStaff" or "AbsintheStaff"
            or "HoolheyakWindArts" or "EyjafjallaStaff" => "CoreCaster",
        "NoirShield" or "BeagleWeapon" or "CardiWeapon" or "CuoraWeapon" or "CroissantHammer" or "NianWeapon" => "Protector",
        "DN_Weapon" => "ArtsProtector",
        "LiskarmPistol" => "SentinelProtector",
        "Vulcan_Weapon" or "MudrockHammer" => "Juggernaut",
        "EunectesAxe" => "Duelist",
        "HornGrenadeLauncher" or "FirewhistleLauncher" => "Fortress",
        "MagallanController" => "Summoner",
        "KroosCrossbow" or "KroosAlterCrossbow" or "MelaniteCrossbow" or "ProvenceCrossbow"
            or "SchwarzBow" or "GreyThroatCrossbow" or "BluePoisonCrossbow" or "PozemkaCrossbow" => "Crossbowman",
        _ => null
    }) : null;

    internal static string Profession(string branch) => branch switch
    {
        "Mystic" or "ChainCaster" or "MechAccord" or "SplashCaster" or "Phalanx"
            or "PrimalCaster" or "BlastCaster" or "CoreCaster" => "Caster",
        "ArtsFighter" or "Dreadnought" or "Musha" or "Centurion" or "Crusher" or "Reaper" or "Earthshaker" or "Swordmaster" or "Lord" => "Guard",
        "Artilleryman" or "Deadeye" or "Crossbowman" or "Spreadshooter" => "Sniper",
        "Executor" or "Ambusher" => "Specialist",
        "Charger" or "StandardBearer" => "Vanguard",
        "Medic" or "MedicTherapist" or "MedicMultiTarget" or "MedicChain" or "MedicWandering" => "Medic",
        "Fortress" or "Protector" or "ArtsProtector" or "SentinelProtector" or "Juggernaut" or "Duelist" or "PrimalProtector" => "Defender",
        "Summoner" => "Supporter",
        _ => null
    };

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        // Flavor follows all item-specific controls and numbers, immediately before
        // the final profession block (two lines for medical and Defender weapons).
        if (item.ModItem?.Mod.Name == "ArknightsMod")
        {
            string flavorKey = "Mods.ArknightsMod." + item.ModItem.LocalizationCategory + "." + item.ModItem.Name + ".FlavorText";
            if (Language.Exists(flavorKey))
                tooltips.Add(new TooltipLine(Mod, "WeaponFlavor", Language.GetTextValue(flavorKey)));
        }
        if (Branch(item) is string branch)
        {
            string profession = Profession(branch);
            if (profession is "Medic" or "Defender")
            {
                tooltips.Add(new TooltipLine(Mod, "OperatorClass", Language.GetTextValue("Mods.ArknightsMod.BranchTraits." + profession + "Profession")));
            }
            tooltips.Add(new TooltipLine(Mod, "OperatorBranch", Language.GetTextValue("Mods.ArknightsMod.BranchTraits." + branch)));
        }
    }

    public override void HoldItem(Item item, Player player)
    {
        if (Branch(item) == "Ambusher")
            player.aggro -= 2000;
        if (Branch(item) == "Musha")
            player.AddBuff(BuffID.PotionSickness, 600);
    }

    public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Branch(item) == "ArtsFighter") modifiers.DefenseEffectiveness *= 0f;
    }

    public override void SetDefaults(Item item)
    {
        if (Branch(item) != "Centurion") return;
        Item saw = new(ItemID.ButchersChainsaw);
        item.useTime = saw.useTime;
    }
}

public sealed class ArtsFighterProjectileTrait : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool ignoresDefense;
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        string ns = projectile.ModProjectile?.GetType().Namespace ?? "";
        ignoresDefense = ns.Contains(".Guard.Laevatain") || ns.Contains(".Guard.Mousse")
            || ns.Contains(".Guard.Astesia") || ns.Contains(".Guard.ArtsFighter")
            || source is EntitySource_ItemUse item && OperatorBranchTraits.Branch(item.Item) == "ArtsFighter"
            || source is EntitySource_Parent parent && parent.Entity is Projectile p
                && p.GetGlobalProjectile<ArtsFighterProjectileTrait>().ignoresDefense;
    }
    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (ignoresDefense) modifiers.DefenseEffectiveness *= 0f;
    }
}
