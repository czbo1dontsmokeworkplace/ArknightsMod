using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public interface IProbabilityDodgeSource
{
    string DodgeName { get; }
    double DodgeChance { get; }
    int DodgeBuffIcon => 0;
    // Conditional sources still use the shared probability roll, but only for
    // eligible incoming hits (for example, NPC contact instead of projectiles).
    bool CanDodge(Player.HurtInfo info) => true;
    void OnProbabilityDodge(Player.HurtInfo info) { }
}

// Read-only status: resource shields and lethal-hit protection retain their own
// consumption rules rather than being converted into a permanent 100% chance.
public readonly record struct GuaranteedAvoidanceState(string Name, bool Ready, string Detail);
public interface IGuaranteedAvoidanceSource
{
    GuaranteedAvoidanceState? GuaranteedAvoidance { get; }
}

public sealed class DodgeRegistry : ModSystem
{
    internal static ModPlayer[] ProbabilitySources = Array.Empty<ModPlayer>();
    internal static ModPlayer[] GuaranteedSources = Array.Empty<ModPlayer>();
    public override void PostSetupContent()
    {
        ProbabilitySources = ModContent.GetContent<ModPlayer>().Where(p => p is IProbabilityDodgeSource).ToArray();
        GuaranteedSources = ModContent.GetContent<ModPlayer>().Where(p => p is IGuaranteedAvoidanceSource).ToArray();
    }
    public override void Unload()
    {
        ProbabilitySources = GuaranteedSources = Array.Empty<ModPlayer>();
    }
}

public sealed class DodgePlayer : ModPlayer
{
    private IEnumerable<(ModPlayer Owner, IProbabilityDodgeSource Source, double Chance)> ProbabilitySources()
    {
        if (!Player.active || Player.dead) yield break;
        foreach (ModPlayer prototype in DodgeRegistry.ProbabilitySources)
            if (Player.TryGetModPlayer(prototype, out ModPlayer owner) && owner is IProbabilityDodgeSource source)
            {
                double chance = Math.Clamp(source.DodgeChance, 0, 1);
                if (double.IsFinite(chance) && chance > 0) yield return (owner, source, chance);
            }
    }

    public double DodgeChance => CombineIndependent(ProbabilitySources().Select(s => s.Chance));
    public static double CombineIndependent(IEnumerable<double> chances)
    {
        double failure = 1;
        foreach (double chance in chances)
            if (double.IsFinite(chance)) failure *= 1 - Math.Clamp(chance, 0, 1);
        return 1 - failure;
    }

    public override bool FreeDodge(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer || !info.Dodgeable || info.DamageSource == null
            || (info.DamageSource.SourceNPCIndex < 0 && info.DamageSource.SourceProjectileLocalIndex < 0)) return false;
        // One random roll. Intervals have the same probabilities as independent
        // sequential checks, preserving the winning source's own feedback/i-frames.
        var sources = ProbabilitySources().Where(entry => entry.Source.CanDodge(info)).ToArray();
        if (sources.Length == 0) return false;
        double roll = Main.rand.NextDouble(), remaining = 1;
        foreach (var entry in sources)
        {
            double interval = remaining * entry.Chance;
            if (roll < interval)
            {
                entry.Source.OnProbabilityDodge(info);
                return true;
            }
            roll -= interval;
            remaining *= 1 - entry.Chance;
        }
        return false;
    }

    private static int Icon(ModPlayer owner) => owner.GetType().Namespace?.Contains(".Armor.") == true
        ? owner.Player.armor[0].type : owner.Player.HeldItem.type;

    // Mod.Call returns primitives only; PRTS has no assembly dependency and never
    // replays a damage hook, rolls random numbers or consumes a defense resource.
    internal object Snapshot()
    {
        var probabilities = ProbabilitySources().ToArray();
        var guaranteed = new List<object>();
        if (Player.active && !Player.dead)
            foreach (ModPlayer prototype in DodgeRegistry.GuaranteedSources)
                if (Player.TryGetModPlayer(prototype, out ModPlayer owner)
                    && owner is IGuaranteedAvoidanceSource source
                    && source.GuaranteedAvoidance is GuaranteedAvoidanceState state)
                    guaranteed.Add(new object[] { state.Name, state.Ready, state.Detail, Icon(owner) });
        return new Dictionary<string, object>
        {
            ["Chance"] = CombineIndependent(probabilities.Select(s => s.Chance)),
            ["ProbabilitySources"] = probabilities.Select(s => new object[]
                { s.Source.DodgeName, s.Chance, Icon(s.Owner), s.Source.DodgeBuffIcon }).ToArray(),
            ["GuaranteedSources"] = guaranteed.ToArray()
        };
    }
}
