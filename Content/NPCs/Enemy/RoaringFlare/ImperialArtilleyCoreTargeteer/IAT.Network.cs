// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public partial class IAT
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(expertHealthFrac);
        writer.Write(bartimer);
        writer.Write(barscale);
        writer.Write(crashtimer);
        writer.Write(bardistance);
        writer.Write(stage1healthscale);
        writer.Write(atkscale);
        writer.Write(MISSChance);
        writer.Write(hpdecreasecooldown);
        writer.Write(npctimer);
        writer.Write(lightsOn);
        writer.Write(normalatkcooldown);
        writer.Write(aimheight);
        writer.Write(aimX);
        writer.Write(basedistance);
        writer.Write(truestage1to2);
        writer.Write(truestage2);
        writer.Write(timer2);
        writer.Write(stage);
        writer.Write(deathtimer);
        writer.Write(deathcheck);
        writer.Write(downAcceleration);
        writer.Write(timer1to2);
        writer.Write(targetX);
        writer.Write(targetY);
        writer.Write(vx);
        writer.Write(vy);
        writer.Write(stage1to2r);
        writer.Write(stage1to2atkspeed);
        writer.Write(stage1to2locktime);
        writer.Write(IATcrashed);
        writer.Write(movetimer);
        writer.Write(diffX);
        writer.Write(diffY);
        writer.Write(stg1to2movementsafe);
        writer.Write(stg1to2safetimer);
        writer.Write(redlightscale);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        expertHealthFrac = reader.ReadSingle();
        bartimer = reader.ReadInt32();
        barscale = reader.ReadSingle();
        crashtimer = reader.ReadInt32();
        bardistance = reader.ReadSingle();
        stage1healthscale = reader.ReadSingle();
        atkscale = reader.ReadSingle();
        MISSChance = reader.ReadSingle();
        hpdecreasecooldown = reader.ReadInt32();
        npctimer = reader.ReadSingle();
        lightsOn = reader.ReadBoolean();
        normalatkcooldown = reader.ReadInt32();
        aimheight = reader.ReadSingle();
        aimX = reader.ReadSingle();
        basedistance = reader.ReadSingle();
        truestage1to2 = reader.ReadInt32();
        truestage2 = reader.ReadInt32();
        timer2 = reader.ReadInt32();
        stage = reader.ReadSingle();
        deathtimer = reader.ReadInt32();
        deathcheck = reader.ReadInt32();
        downAcceleration = reader.ReadSingle();
        timer1to2 = reader.ReadSingle();
        targetX = reader.ReadSingle();
        targetY = reader.ReadSingle();
        vx = reader.ReadSingle();
        vy = reader.ReadSingle();
        stage1to2r = reader.ReadSingle();
        stage1to2atkspeed = reader.ReadSingle();
        stage1to2locktime = reader.ReadSingle();
        IATcrashed = reader.ReadBoolean();
        movetimer = reader.ReadSingle();
        diffX = reader.ReadSingle();
        diffY = reader.ReadSingle();
        stg1to2movementsafe = reader.ReadBoolean();
        stg1to2safetimer = reader.ReadSingle();
        redlightscale = reader.ReadSingle();
    }
}
