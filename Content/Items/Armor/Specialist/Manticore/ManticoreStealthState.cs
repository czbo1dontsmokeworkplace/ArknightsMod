using System;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore;

internal sealed class ManticoreStealthState
{
    internal const int FadeDuration = 150, MaxTransparency = 250;
    internal int FadeTicks { get; private set; }
    internal int Transparency => FadeTicks * MaxTransparency / FadeDuration;
    internal bool Stealthed => FadeTicks > 0;
    private ulong brokenAt = ulong.MaxValue;

    internal void Tick(bool usingItem, ulong gameTick) {
        if (!usingItem && gameTick != brokenAt && FadeTicks < FadeDuration) FadeTicks++;
    }
    internal bool BreakOnHit(ulong gameTick) {
        bool wasStealthed = Stealthed;
        FadeTicks = 0;
        brokenAt = gameTick;
        return wasStealthed;
    }
    internal void Set(int fadeTicks, ulong gameTick) {
        FadeTicks = Math.Clamp(fadeTicks, 0, FadeDuration);
        if (FadeTicks == 0) brokenAt = gameTick;
    }
    internal void Clear() { FadeTicks = 0; brokenAt = ulong.MaxValue; }
}
