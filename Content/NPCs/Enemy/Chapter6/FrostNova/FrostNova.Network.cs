// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(BirthTimer);
        writer.Write(ReBirthTimer);
        writer.Write(escapetimer);
        writer.Write(DeathTimer);
        writer.Write(JumpTimer);
        writer.Write(FNShaderRingTimer);
        writer.Write(FNRevivalShaderRingTimer);
        writer.Write(FNShaderRingIntensity);
        writer.Write(FNShaderRingOpacity);
        writer.Write(FNShaderRingProgress);
        writer.Write(FNShaderRingColorR);
        writer.Write(FNShaderRingColorG);
        writer.Write(FNShaderRingColorB);
        writer.Write(FNTPOpacity);
        writer.Write(FNStage);
        writer.Write(FNBirthEnd);
        writer.Write(FNRevival);
        writer.Write(FNRevivalStart);
        writer.Write(FNRevivalEnd);
        writer.Write(FNEnd);
        writer.Write(FNDeathStart);
        writer.Write(isstuck);
        writer.Write(isstand);
        writer.Write(jumpstage);
        writer.Write(jumptimes);
        writer.Write(statplrposX);
        writer.Write(statplrposY);
        writer.Write(FNATKTimer);
        writer.Write(FNOnATK);
        writer.Write(FNATKFrameInitialized);
        writer.Write(FNSkillTimer);
        writer.Write(FNOnSkill);
        writer.Write(FNSkillFrameInitialized);
        writer.Write(FNSkillChoice);
        writer.Write(FNAttackTimes);
        writer.Write((ushort)Stage2BorderCoordX.Length); foreach (var value in Stage2BorderCoordX) writer.Write(value);
        writer.Write(deltaX);
        writer.Write(slice);
        writer.Write((ushort)IceFloorCandidate.Length); foreach (var value in IceFloorCandidate) writer.Write(value);
        writer.Write(IndexOfFNBF);
        writer.Write(FNBFChecker);
        writer.Write(KillPlayerBecauseAllFloorAreBanned);
        writer.Write(NumOfBanFloorOneTime);
        writer.Write(summontime);
        writer.Write(summontimes);
        writer.Write(summonCD);
        writer.Write(isnpcdefeated);
        writer.Write(isallnpcdefeated);
        writer.Write(iceAltarSpawnCooldown);
        writer.Write((ushort)subNPCType.Length); foreach (var value in subNPCType) writer.Write(value);
        writer.Write(SummonTypeChoice);
        writer.Write(LeftSummonCount);
        writer.Write(CurCount);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        BirthTimer = reader.ReadSingle();
        ReBirthTimer = reader.ReadSingle();
        escapetimer = reader.ReadSingle();
        DeathTimer = reader.ReadSingle();
        JumpTimer = reader.ReadSingle();
        FNShaderRingTimer = reader.ReadSingle();
        FNRevivalShaderRingTimer = reader.ReadSingle();
        FNShaderRingIntensity = reader.ReadSingle();
        FNShaderRingOpacity = reader.ReadSingle();
        FNShaderRingProgress = reader.ReadSingle();
        FNShaderRingColorR = reader.ReadSingle();
        FNShaderRingColorG = reader.ReadSingle();
        FNShaderRingColorB = reader.ReadSingle();
        FNTPOpacity = reader.ReadSingle();
        FNStage = reader.ReadSingle();
        FNBirthEnd = reader.ReadBoolean();
        FNRevival = reader.ReadBoolean();
        FNRevivalStart = reader.ReadBoolean();
        FNRevivalEnd = reader.ReadBoolean();
        FNEnd = reader.ReadBoolean();
        FNDeathStart = reader.ReadBoolean();
        isstuck = reader.ReadBoolean();
        isstand = reader.ReadBoolean();
        jumpstage = reader.ReadInt32();
        jumptimes = reader.ReadInt32();
        statplrposX = reader.ReadSingle();
        statplrposY = reader.ReadSingle();
        FNATKTimer = reader.ReadSingle();
        FNOnATK = reader.ReadBoolean();
        FNATKFrameInitialized = reader.ReadBoolean();
        FNSkillTimer = reader.ReadSingle();
        FNOnSkill = reader.ReadBoolean();
        FNSkillFrameInitialized = reader.ReadBoolean();
        FNSkillChoice = reader.ReadSingle();
        FNAttackTimes = reader.ReadInt32();
        { int count = reader.ReadUInt16(); if (count > 256) throw new System.IO.InvalidDataException(); Stage2BorderCoordX = new float[count]; for (int i = 0; i < count; i++) Stage2BorderCoordX[i] = reader.ReadSingle(); }
        deltaX = reader.ReadSingle();
        slice = reader.ReadSingle();
        { int count = reader.ReadUInt16(); if (count > 256) throw new System.IO.InvalidDataException(); IceFloorCandidate = new float[count]; for (int i = 0; i < count; i++) IceFloorCandidate[i] = reader.ReadSingle(); }
        IndexOfFNBF = reader.ReadInt32();
        FNBFChecker = reader.ReadInt32();
        KillPlayerBecauseAllFloorAreBanned = reader.ReadInt32();
        NumOfBanFloorOneTime = reader.ReadInt32();
        summontime = reader.ReadInt32();
        summontimes = reader.ReadInt32();
        summonCD = reader.ReadInt32();
        isnpcdefeated = reader.ReadBoolean();
        isallnpcdefeated = reader.ReadInt32();
        iceAltarSpawnCooldown = reader.ReadInt32();
        { int count = reader.ReadUInt16(); if (count > 256) throw new System.IO.InvalidDataException(); subNPCType = new int[count]; for (int i = 0; i < count; i++) subNPCType[i] = reader.ReadInt32(); }
        SummonTypeChoice = reader.ReadInt32();
        LeftSummonCount = reader.ReadInt32();
        CurCount = reader.ReadInt32();
    }
}
