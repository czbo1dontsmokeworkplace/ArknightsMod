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
    }
}
