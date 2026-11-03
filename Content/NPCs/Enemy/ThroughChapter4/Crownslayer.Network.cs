// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class Crownslayer
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write((int)LastSkill);
        writer.Write(FogSkillCooldown);
        writer.Write(ExSkillCooldown);
        writer.Write(UtSkillCooldown);
        writer.Write(DaggerSkillCooldown);
        writer.Write(PhaseLevel);
        writer.Write(phaseOffset.X); writer.Write(phaseOffset.Y);
        writer.Write((ushort)MinionWhoAmIs.Count); foreach (int value in MinionWhoAmIs) writer.Write(value);
        writer.Write(grayScaleIntensity);
        writer.Write(skill3DashStart.X); writer.Write(skill3DashStart.Y);
        writer.Write(skill3RetreatVisualStep);
        writer.Write(skill3RetreatTick);
        writer.Write(suppressSkill1AfterSkill3);
        writer.Write(skill6Weakened);
        writer.Write(skill6FarRangeTimer);
        writer.Write((int)pendingSkill);
        writer.Write(skillWindupTimer);
        writer.Write(spawnIntroTimer);
        writer.Write(skill9DashStart.X); writer.Write(skill9DashStart.Y);
        writer.Write(skill9DashGoal.X); writer.Write(skill9DashGoal.Y);
        writer.Write(skill7VApex.X); writer.Write(skill7VApex.Y);
        writer.Write(skill7SecondCorner.X); writer.Write(skill7SecondCorner.Y);
        writer.Write(skill7CycleStep);
        writer.Write(skill7StepTimer);
        writer.Write(skill7WillThrowDagger);
        writer.Write(damage);
        writer.Write((int)CurrentAnimation);
        writer.Write(leaveCleared);
        writer.Write(moveStuckTicks);
        writer.Write(moveWallBumpTicks);
        writer.Write(moveWallEmbedTicks);
        writer.Write(moveUnstuckCooldown);
        writer.Write(moveStuckAnchor.X); writer.Write(moveStuckAnchor.Y);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        LastSkill = (AIState)reader.ReadInt32();
        FogSkillCooldown = reader.ReadInt32();
        ExSkillCooldown = reader.ReadInt32();
        UtSkillCooldown = reader.ReadInt32();
        DaggerSkillCooldown = reader.ReadInt32();
        PhaseLevel = reader.ReadInt32();
        phaseOffset = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        { int count = reader.ReadUInt16(); if (count > 200) throw new System.IO.InvalidDataException(); MinionWhoAmIs.Clear(); for (int i = 0; i < count; i++) MinionWhoAmIs.Add(reader.ReadInt32()); }
        grayScaleIntensity = reader.ReadSingle();
        skill3DashStart = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        skill3RetreatVisualStep = reader.ReadInt32();
        skill3RetreatTick = reader.ReadInt32();
        suppressSkill1AfterSkill3 = reader.ReadInt32();
        skill6Weakened = reader.ReadBoolean();
        skill6FarRangeTimer = reader.ReadInt32();
        pendingSkill = (AIState)reader.ReadInt32();
        skillWindupTimer = reader.ReadInt32();
        spawnIntroTimer = reader.ReadInt32();
        skill9DashStart = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        skill9DashGoal = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        skill7VApex = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        skill7SecondCorner = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        skill7CycleStep = reader.ReadInt32();
        skill7StepTimer = reader.ReadInt32();
        skill7WillThrowDagger = reader.ReadBoolean();
        damage = reader.ReadInt32();
        CurrentAnimation = (NPCState)reader.ReadInt32();
        leaveCleared = reader.ReadBoolean();
        moveStuckTicks = reader.ReadInt32();
        moveWallBumpTicks = reader.ReadInt32();
        moveWallEmbedTicks = reader.ReadInt32();
        moveUnstuckCooldown = reader.ReadInt32();
        moveStuckAnchor = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
}
