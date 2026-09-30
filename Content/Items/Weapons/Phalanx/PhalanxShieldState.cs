using System;

namespace ArknightsMod.Content.Items.Weapons.Phalanx;

// One ledger per player, shared by all three staves and all copies of them.
// Neither equipping an item nor changing its tier creates new shield charges.
public sealed class PhalanxShieldState
{
    public const int RecoveryDelay = 300;
    public const int RecoveryInterval = 120;
    public int Spent { get; private set; }
    public int QuietFrames { get; private set; }
    public int RecoveryFrames { get; private set; }
    public int Remaining(int tier) => Math.Max(0, tier + 3 - Spent);
    public bool Block(int tier)
    {
        if (Remaining(tier) == 0) return false;
        Spent++;
        Disturb();
        return true;
    }
    public void Disturb() => QuietFrames = RecoveryFrames = 0;
    public void Tick()
    {
        if (QuietFrames < RecoveryDelay) { QuietFrames++; return; }
        if (Spent > 0 && ++RecoveryFrames >= RecoveryInterval)
        {
            Spent--;
            RecoveryFrames = 0;
        }
    }
    public void Restore(int spent, int quiet, int recovery)
    {
        Spent = Math.Clamp(spent, 0, 5);
        QuietFrames = Math.Clamp(quiet, 0, RecoveryDelay);
        RecoveryFrames = Math.Clamp(recovery, 0, RecoveryInterval - 1);
    }
}

public sealed class PhalanxCycle
{
    public const int ChargeDuration = 90;
    public int ChargeFrames { get; private set; }
    public float Opacity { get; private set; }
    public bool Tick(bool holding, bool attacking, bool charged)
    {
        float target = holding && !attacking && charged ? 1f : 0f;
        Opacity = Math.Clamp(Opacity + Math.Sign(target - Opacity) / 24f, 0, 1);
        if (!holding || !attacking) { CancelCharge(); return false; }
        if (++ChargeFrames < ChargeDuration) return false;
        ChargeFrames = 0;
        return true;
    }
    public void CancelCharge() => ChargeFrames = 0;
    public void ClearVisual() { Opacity = 0; CancelCharge(); }
}
