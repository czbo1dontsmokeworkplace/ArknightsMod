using System.IO;
using Microsoft.Xna.Framework;
namespace ArknightsMod.Content.NPCs.Enemy.W;

public partial class WSpawnState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(walkDir); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); walkDir = r.ReadInt32(); }
}

public partial class WSmokeTeleportState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(arriveTick); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); arriveTick = r.ReadInt32(); }
}

public partial class WPhaseTransitState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(arriveTick); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); arriveTick = r.ReadInt32(); }
}

public partial class WDodgeTeleportState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(arriveTick); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); arriveTick = r.ReadInt32(); }
}

public partial class WHopTossState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(hopDir); w.Write(airborne); w.Write(released); w.Write(landed); w.Write(releaseTimer); w.Write(landTimer); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); hopDir = r.ReadInt32(); airborne = r.ReadBoolean(); released = r.ReadBoolean(); landed = r.ReadBoolean(); releaseTimer = r.ReadInt32(); landTimer = r.ReadInt32(); }
}

public partial class WMineRunState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(arriveTick); w.Write(pressed); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); arriveTick = r.ReadInt32(); pressed = r.ReadBoolean(); }
}

public abstract partial class WAerialState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write((int)Phase); w.Write(PhaseTimer); w.Write(airborne); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); Phase = (AirPhase)r.ReadInt32(); PhaseTimer = r.ReadInt32(); airborne = r.ReadBoolean(); }
}

public partial class WAirStrafeState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(sweepDir); }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); sweepDir = r.ReadInt32(); }
}

public partial class WCarpetState
{
    public override void WriteSnapshot(BinaryWriter w) { base.WriteSnapshot(w); w.Write(planned); foreach (var p in points) { w.Write(p.X); w.Write(p.Y); } }
    public override void ReadSnapshot(BinaryReader r) { base.ReadSnapshot(r); planned = r.ReadBoolean(); for(int i=0;i<points.Length;i++) points[i]=new Vector2(r.ReadSingle(),r.ReadSingle()); }
}

