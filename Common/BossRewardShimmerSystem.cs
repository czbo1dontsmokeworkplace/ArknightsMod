using ArknightsMod.Content.Items.Evolution;
using ArknightsMod.Content.Items.Evolution.Arsenal;
using ArknightsMod.Content.Items.MaterialistArsenal;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common;

public sealed class BossRewardShimmerSystem : ModSystem
{
    public override void PostSetupContent()
    {
        // Keep each boss's rewards in a separate, closed shimmer cycle.
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<RotorBladeCraft>()] = ModContent.ItemType<MiniGatling>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<MiniGatling>()] = ModContent.ItemType<OrbitalLaser>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<OrbitalLaser>()] = ModContent.ItemType<FighterDispatchTerminal>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<FighterDispatchTerminal>()] = ModContent.ItemType<PlasmaProjector>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<PlasmaProjector>()] = ModContent.ItemType<RotorBladeCraft>();

        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<OriginSever>()] = ModContent.ItemType<RushingEnd>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<RushingEnd>()] = ModContent.ItemType<TerminusLight>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<TerminusLight>()] = ModContent.ItemType<NestOfLife>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<NestOfLife>()] = ModContent.ItemType<OriginSever>();

        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<EvolutionOrigin>()] = ModContent.ItemType<EvolutionTerminus>();
        ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<EvolutionTerminus>()] = ModContent.ItemType<EvolutionOrigin>();
    }
}
