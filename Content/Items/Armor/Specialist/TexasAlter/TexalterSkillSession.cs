namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter;

internal sealed class TexalterSkillSession
{
    private uint activation;
    private bool initialized, consumed;
    internal bool Ready(bool active, uint id) {
        if (!active) return false;
        if (!initialized || activation != id) { initialized = true; activation = id; consumed = false; }
        return !consumed;
    }
    internal bool TryConsume(bool active, uint id) {
        if (!Ready(active, id)) return false;
        consumed = true;
        return true;
    }
    internal void Clear() { initialized = consumed = false; activation = 0; }
}
