namespace ArknightsMod.Content.Buffs;

// Same control effect; the clock supplies its own impact feedback instead of
// repeatedly drawing the shared white rotating stun sprite over the enemy.
public sealed class MostimaClockStun : StunDebuff
{
    public override string Texture => "ArknightsMod/Content/Buffs/StunDebuff";
    protected override bool ShowIndicator => false;
}
