// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class Drone
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(targetHeight);
        writer.Write(moveSpeed);
        writer.Write(lastPosition.X); writer.Write(lastPosition.Y);
        writer.Write(attackCooldown);
        writer.Write(isInAttackPhase);
        writer.Write(shootTimer);
        writer.Write(stuckTimer);
        writer.Write(frame);
        writer.Write(frameCounter);
        writer.Write(heightAlarmTimer);
        writer.Write((int)state);
        writer.Write(homeAnchor.X); writer.Write(homeAnchor.Y);
        writer.Write(turnCooldown);
        writer.Write(platformDropTimer);
        writer.Write(idlePhase);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        targetHeight = reader.ReadSingle();
        moveSpeed = reader.ReadSingle();
        lastPosition = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        attackCooldown = reader.ReadInt32();
        isInAttackPhase = reader.ReadBoolean();
        shootTimer = reader.ReadInt32();
        stuckTimer = reader.ReadInt32();
        frame = reader.ReadInt32();
        frameCounter = reader.ReadInt32();
        heightAlarmTimer = reader.ReadInt32();
        state = (AIState)reader.ReadInt32();
        homeAnchor = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        turnCooldown = reader.ReadInt32();
        platformDropTimer = reader.ReadInt32();
        idlePhase = reader.ReadSingle();
    }
}
