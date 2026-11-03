// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.TillChapter7;

public partial class InsaneZombieL
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(diffX);
        writer.Write(diffY);
        writer.Write(distance);
        writer.Write(fadeTimer);
        writer.Write(InMove);
        writer.Write(maxspeed);
        writer.Write(SpellResist);
        writer.Write(Inattack);
        writer.Write(jumpCD);
        writer.Write(attackCD);
        writer.Write(attackCDMax);
        writer.Write(AttackDamage);
        writer.Write(Attackrange);
        writer.Write(isJumping);
        writer.Write(jumpAttackInertia);
        writer.Write(InDeath);
        writer.Write(fadeTime);
        writer.Write(fadeOutTimer);
        writer.Write(AttackStartFrame);
        writer.Write(AttackEndFrame);
        writer.Write(DeathEndFrame);
        writer.Write(FrameSpeed);
        writer.Write(blooding);
        writer.Write(blooding2);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        diffX = reader.ReadSingle();
        diffY = reader.ReadSingle();
        distance = reader.ReadSingle();
        fadeTimer = reader.ReadInt32();
        InMove = reader.ReadBoolean();
        maxspeed = reader.ReadSingle();
        SpellResist = reader.ReadInt32();
        Inattack = reader.ReadBoolean();
        jumpCD = reader.ReadInt32();
        attackCD = reader.ReadInt32();
        attackCDMax = reader.ReadInt32();
        AttackDamage = reader.ReadInt32();
        Attackrange = reader.ReadInt32();
        isJumping = reader.ReadBoolean();
        jumpAttackInertia = reader.ReadSingle();
        InDeath = reader.ReadBoolean();
        fadeTime = reader.ReadInt32();
        fadeOutTimer = reader.ReadInt32();
        AttackStartFrame = reader.ReadInt32();
        AttackEndFrame = reader.ReadInt32();
        DeathEndFrame = reader.ReadInt32();
        FrameSpeed = reader.ReadInt32();
        blooding = reader.ReadSingle();
        blooding2 = reader.ReadSingle();
    }
}
