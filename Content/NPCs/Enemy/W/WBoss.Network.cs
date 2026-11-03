using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.NPCs.Enemy.W;

public partial class WBoss
{
    protected override bool PredictClientAI => true;

    private void WriteCombatSnapshot(BinaryWriter w)
    {
        int stateId = Machine?.CurrentState?.StateId ?? -1;
        w.Write(stateId);
        if (stateId >= 0) ((WNetworkState)Machine.CurrentState).WriteSnapshot(w);
        w.Write((int)TeleportMode); w.Write(Dashing); w.Write(DashFrom.X); w.Write(DashFrom.Y); w.Write(DashTicks); w.Write(DashTimer);
        w.Write(NukeTarget.HasValue); if (NukeTarget.HasValue) { w.Write(NukeTarget.Value.X); w.Write(NukeTarget.Value.Y); }
        w.Write(NukePending); w.Write(NukeUnlocked); w.Write(JackpotPending); w.Write(FaceAwayTicks); w.Write(DeathRealKill);
        w.Write(ShowLauncher); w.Write(AnimOverrideRow); w.Write(AimOverride.HasValue); if (AimOverride.HasValue) w.Write(AimOverride.Value);
        w.Write(AimRotation); w.Write(TelegraphLaser); w.Write(LauncherVis); w.Write(CardAlpha); w.Write(CardFlip);
        w.Write(PickDelay); w.Write(LastAttackStateId); w.Write(SinceSpectacle); w.Write(SinceAttack); w.Write(lastWasFiller);
        w.Write(StuckTicks); w.Write(stuckAnchor.X); w.Write(stuckAnchor.Y);
        w.Write((byte)ExtraLasers.Count); foreach (var p in ExtraLasers) { w.Write(p.X); w.Write(p.Y); }
        w.Write((byte)ExtraMarkers.Count); foreach (var p in ExtraMarkers) { w.Write(p.X); w.Write(p.Y); }
    }

    private void ReadCombatSnapshot(BinaryReader r)
    {
        int stateId = r.ReadInt32();
        if (stateId >= 0) {
            Machine ??= BuildMachine();
            if (Machine.CurrentState.StateId != stateId) Machine.ChangeState(stateId);
            if (Machine.CurrentState.StateId != stateId || Machine.CurrentState is not WNetworkState state) throw new InvalidDataException("Unknown W state snapshot");
            state.ReadSnapshot(r);
        }
        // Apply context after OnEnter, which initializes these same fields.
        TeleportMode = (TeleportKind)r.ReadInt32(); Dashing = r.ReadBoolean(); DashFrom = new Vector2(r.ReadSingle(), r.ReadSingle()); DashTicks = r.ReadInt32(); DashTimer = r.ReadInt32();
        NukeTarget = r.ReadBoolean() ? new Vector2(r.ReadSingle(), r.ReadSingle()) : null;
        NukePending = r.ReadBoolean(); NukeUnlocked = r.ReadBoolean(); JackpotPending = r.ReadBoolean(); FaceAwayTicks = r.ReadInt32(); DeathRealKill = r.ReadBoolean();
        ShowLauncher = r.ReadBoolean(); AnimOverrideRow = r.ReadInt32(); AimOverride = r.ReadBoolean() ? r.ReadSingle() : null;
        AimRotation = r.ReadSingle(); TelegraphLaser = r.ReadSingle(); LauncherVis = r.ReadSingle(); CardAlpha = r.ReadSingle(); CardFlip = r.ReadSingle();
        PickDelay = r.ReadInt32(); LastAttackStateId = r.ReadInt32(); SinceSpectacle = r.ReadInt32(); SinceAttack = r.ReadInt32(); lastWasFiller = r.ReadBoolean();
        StuckTicks = r.ReadInt32(); stuckAnchor = new Vector2(r.ReadSingle(), r.ReadSingle());
        ReadPoints(r, ExtraLasers); ReadPoints(r, ExtraMarkers);
    }

    private static void ReadPoints(BinaryReader reader, System.Collections.Generic.List<Vector2> points)
    {
        int count = reader.ReadByte(); if (count > 32) throw new InvalidDataException();
        points.Clear(); for (int i = 0; i < count; i++) points.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle()));
    }
}
