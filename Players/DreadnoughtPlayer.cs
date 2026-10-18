using ArknightsMod.Content.Items.Weapons.Charged;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class DreadnoughtPlayer : ModPlayer
{
    internal float Momentum;
    internal float Multiplier => 1f + Momentum;
    internal float Progress => Momentum / DreadnoughtRules.MaxMomentum;
    private int idleTicks;
    private Item fightingItem;

    internal void Attacking(Item item)
    {
        fightingItem = item;
        idleTicks = 0;
    }

    internal void RegisterHit() => Momentum = DreadnoughtRules.Gain(Momentum);

    public override void PostUpdate()
    {
        idleTicks = System.Math.Min(DreadnoughtRules.IdleGraceTicks + 1, idleTicks + 1);
        bool switchedAway = !ReferenceEquals(Player.HeldItem, fightingItem);
        if (switchedAway || idleTicks > DreadnoughtRules.IdleGraceTicks)
            Momentum = DreadnoughtRules.Decay(Momentum, switchedAway);
    }

    public override void UpdateDead()
    {
        Momentum = 0;
        idleTicks = 0;
        fightingItem = null;
    }
}
