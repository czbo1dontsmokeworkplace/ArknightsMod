// Fixed, bounded encounter snapshot. Keep sender and receiver order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer) {
        base.SendExtraAI(writer);
        writer.Write(BirthTimer);
        writer.Write(ReBirthTimer);
        writer.Write(escapetimer);
        writer.Write(DeathTimer);
        writer.Write(FNShaderRingTimer);
        writer.Write(FNStage);
        writer.Write(FNBirth);
        writer.Write(FNBirthEnd);
        writer.Write(FNRevival);
        writer.Write(FNRevivalStart);
        writer.Write(FNRevivalEnd);
        writer.Write(FNEnd);
        writer.Write(FNDeathStart);
        writer.Write(FNATKTimer);
        writer.Write(FNOnATK);
        writer.Write(FNATKFrameInitialized);
        writer.Write(FNSkillTimer);
        writer.Write(FNOnSkill);
        writer.Write(FNSkillFrameInitialized);
        writer.Write(encounterToken);
        writer.Write(combatTimer);
        writer.Write(attackSerial);
        writer.Write(supportTimer);
        writer.Write(attackInitialized);
        writer.Write(attackAnchor.X); writer.Write(attackAnchor.Y);
        writer.Write(attackAim.X); writer.Write(attackAim.Y);
        writer.Write(dashDirection.X); writer.Write(dashDirection.Y);
        writer.Write(NPC.width); writer.Write(NPC.height);
        writer.Write(revivalAnchorSet);
        writer.Write(revivalAnchor.X); writer.Write(revivalAnchor.Y);
        writer.Write(relocationStart);
        writer.Write(relocationArmed);
        writer.Write(relocationOrigin.X); writer.Write(relocationOrigin.Y);
        writer.Write(relocationTarget.X); writer.Write(relocationTarget.Y);
        writer.Write(domainReady);writer.Write(domainTravel);
        writer.Write(domainCenter.X);writer.Write(domainCenter.Y);
        writer.Write(domainOrigin.X);writer.Write(domainOrigin.Y);
        writer.Write(domainDestination.X);writer.Write(domainDestination.Y);
    }
    public override void ReceiveExtraAI(System.IO.BinaryReader reader) {
        base.ReceiveExtraAI(reader);
        BirthTimer = reader.ReadSingle();
        ReBirthTimer = reader.ReadSingle();
        escapetimer = reader.ReadSingle();
        DeathTimer = reader.ReadSingle();
        FNShaderRingTimer = reader.ReadSingle();
        FNStage = reader.ReadSingle();
        FNBirth = reader.ReadBoolean();
        FNBirthEnd = reader.ReadBoolean();
        FNRevival = reader.ReadBoolean();
        FNRevivalStart = reader.ReadBoolean();
        FNRevivalEnd = reader.ReadBoolean();
        FNEnd = reader.ReadBoolean();
        FNDeathStart = reader.ReadBoolean();
        FNATKTimer = reader.ReadSingle();
        FNOnATK = reader.ReadBoolean();
        FNATKFrameInitialized = reader.ReadBoolean();
        FNSkillTimer = reader.ReadSingle();
        FNOnSkill = reader.ReadBoolean();
        FNSkillFrameInitialized = reader.ReadBoolean();
        encounterToken = reader.ReadInt32();
        combatTimer = reader.ReadInt32();
        attackSerial = reader.ReadInt32();
        supportTimer = reader.ReadInt32();
        attackInitialized = reader.ReadBoolean();
        attackAnchor = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        attackAim = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        dashDirection = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        NPC.width = System.Math.Clamp(reader.ReadInt32(), 40, ShieldTargetSize);
        NPC.height = System.Math.Clamp(reader.ReadInt32(), 58, ShieldTargetSize);
        revivalAnchorSet = reader.ReadBoolean();
        revivalAnchor = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        relocationStart = reader.ReadInt32();
        relocationArmed = reader.ReadBoolean();
        relocationOrigin = new(reader.ReadSingle(), reader.ReadSingle());
        relocationTarget = new(reader.ReadSingle(), reader.ReadSingle());
        domainReady=reader.ReadBoolean();domainTravel=System.Math.Clamp(reader.ReadInt32(),0,22);
        domainCenter=new(reader.ReadSingle(),reader.ReadSingle());domainOrigin=new(reader.ReadSingle(),reader.ReadSingle());
        domainDestination=new(reader.ReadSingle(),reader.ReadSingle());
    }
}
