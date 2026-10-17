using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;

namespace ArknightsMod.Common.GlobalItems;

// Keep the branch rule after the item's own description and upgrade information.
public sealed class OperatorBranchTraits : GlobalItem
{
    internal static string Branch(Item item) => item.ModItem?.Name switch
    {
        "CatapultLauncher" or "MeteoriteCannon" or "FiammettaHandCannon" => "Artilleryman",
        "UtageKatana" or "HellagurOdachi" => "Musha",
        "MelanthaSword" or "MatoimaruNaginata" or "FrankaRapier" or "NearlRadiantSword" => "Dreadnought",
        "MousseGlove" or "AstraSword" or "SurtrLaevatain" => "ArtsFighter",
        "ClickStaff" or "RockrockStaff" or "GoldenglowWand" => "MechAccord",
        "IndigoStaff" or "HarmonieStaff" or "EbenholzStaff" => "Mystic",
        "BeeswaxStaff" or "CarnelianStaff" or "LinStaff" => "Phalanx",
        "PineconeNailgun" or "ExecutorShotgun" or "ChalterWatergun" => "Spreadshooter",
        "GravelDualBlades" or "RedDagger" or "PhantomDaggers" => "Executor",
        "SpecterBoneSaw" or "BlazeGreatsword" or "GavialChainsaw" => "Centurion",
        "WarfarinStaff" or "SussurroStaff" or "ShiningStaff" => "Medic",
        "PuddingStaff" or "LeiziStaff" or "PassengerConductor" => "ChainCaster",
        "Lava_Dagger" or "GitanoOrb" or "LeonhardtStaff" or "SkyfireStaff" or "MostimaStaff" => "SplashCaster",
        "AmbrielRifle" or "AndreanaRifle" or "FirewatchRifle" or "FartoothGreatbow" => "Deadeye",
        "KroosCrossbow" or "KroosAlterCrossbow" or "MelaniteCrossbow" or "ProvenceCrossbow"
            or "SchwarzBow" or "GreyThroatCrossbow" or "BluePoisonCrossbow" or "PozemkaCrossbow" => "Crossbowman",
        _ => null
    };

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (Branch(item) is string branch)
            tooltips.Add(new TooltipLine(Mod, "OperatorBranch", Language.GetTextValue("Mods.ArknightsMod.BranchTraits." + branch)));
    }

    public override void HoldItem(Item item, Player player)
    {
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
        item.axe = saw.axe;
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
