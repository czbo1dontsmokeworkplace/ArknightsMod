using System.IO;
using InnoVault.StateMachines;

namespace ArknightsMod.Content.NPCs.Enemy.W;

// InnoVault synchronizes state IDs; its Timer/Counter are deliberately local.
// W also needs the current progress and recorded attack points on a late join.
public abstract class WNetworkState : VaultState<WBoss>
{
    public virtual void WriteSnapshot(BinaryWriter writer) { writer.Write(Timer); writer.Write(Counter); }
    public virtual void ReadSnapshot(BinaryReader reader) { Timer = reader.ReadInt32(); Counter = reader.ReadInt32(); }
}
