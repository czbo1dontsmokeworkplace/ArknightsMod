using System;

namespace ArknightsMod.Content.Items.Weapons.Guard.Gavial;

internal static class GavialBalance
{
    internal const int Damage = 240;
    internal const float Reach = 112f;
    internal const float AssaultReach = 158f;
    internal const float Width = 42f;
    internal const int NormalBiteTicks = 7;
    internal const int SoulBiteTicks = 4;
    internal const int HealCooldownTicks = 30;
    internal const int HealCap = 6;
    internal const int RepaymentTicks = 20 * 60;
    internal static float DamageMultiplier(int mode) => mode switch { 1 => 1.8f, 2 => 2.8f, 3 => 2.4f, _ => 1f };
}

/// <summary>整数累计结算：无论欠血量能否整除 1200，20 秒结束时都恰好偿还全部。</summary>
internal sealed class GavialDamageDebt
{
    internal int Banked { get; private set; }
    internal int Remaining => total - paid;
    private int total, paid, elapsed;

    internal void Bank(int amount) => Banked += Math.Max(0, amount);

    internal void BeginRepayment()
    {
        if (Banked <= 0) return;
        total = Remaining + Banked;
        Banked = paid = elapsed = 0;
    }

    internal int Tick()
    {
        if (Remaining <= 0) return 0;
        elapsed = Math.Min(elapsed + 1, GavialBalance.RepaymentTicks);
        int due = (int)((long)total * elapsed / GavialBalance.RepaymentTicks);
        int loss = due - paid;
        paid = due;
        return loss;
    }

    internal void Reset() => Banked = total = paid = elapsed = 0;
}
