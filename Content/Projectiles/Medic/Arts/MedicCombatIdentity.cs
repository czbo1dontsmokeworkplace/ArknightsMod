using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// Offensive equipment kept in the Medic directory is covered as well. No damage/skill grants here.
public sealed class MedicCombatIdentity : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private float age;
    private int target=-1;
    private readonly Vector2[] history=new Vector2[24];
    internal static int Style(Projectile q) => q.ModProjectile?.Name switch {
        "ReedFlameShadowFlame" or "ReedFlameShadowOrb" => 24,
        "ClosureScanShotProjectile"=>25,_=>-1};
    public override bool AppliesToEntity(Projectile entity,bool lateInstantiation) => lateInstantiation && Style(entity)>=0;
    public override void SendExtraAI(Projectile q,BitWriter bitWriter,BinaryWriter writer)
    {writer.Write(age);writer.Write(target);}
    public override void ReceiveExtraAI(Projectile q,BitReader bitReader,BinaryReader reader)
    {float value=reader.ReadSingle();age=float.IsFinite(value)?Math.Clamp(value,0,3600):0;int next=reader.ReadInt32();target=Main.npc.IndexInRange(next)?next:-1;}
    public override void PostAI(Projectile q)
    {
        float step=1f/q.MaxUpdates;age+=step;
        if(q.numUpdates==0)
        {
            for(int i=history.Length-1;i>0;i--)history[i]=history[i-1];
            history[0]=q.position;
            MedicMaterialVfx.Flight(q,Style(q),age);
        }
        if(q.ModProjectile?.Name=="ReedFlameShadowOrb")return; // Its orbit / attack / return contract is retained.
        if(age<(Style(q)==24?12:8))return;
        if(q.owner==Main.myPlayer && (target<0 || q.numUpdates==0&&(int)age%12==0))
        {
            int next=-1;float best=Style(q)==24?460*460:320*320;
            foreach(NPC n in Main.ActiveNPCs)
            {
                Vector2 delta=n.Center-q.Center;
                if(!n.CanBeChasedBy(q) || Vector2.Dot(delta.SafeNormalize(Vector2.UnitX),q.velocity.SafeNormalize(Vector2.UnitX))<.1f
                    || !Collision.CanHitLine(q.Center,1,1,n.Center,1,1))continue;
                if(delta.LengthSquared()<best){best=delta.LengthSquared();next=n.whoAmI;}
            }
            if(next!=target){target=next;q.netUpdate=true;}
        }
        if(!Main.npc.IndexInRange(target)||!Main.npc[target].CanBeChasedBy(q))return;
        // Reed curls organically; Closure makes short measured course corrections in packets.
        if(Style(q)==25 && (int)age%8>2)return;
        float angle=q.velocity.ToRotation(),wanted=(Main.npc[target].Center-q.Center).ToRotation();
        float turn=(Style(q)==24?.035f:.065f)*step;
        q.velocity=(angle+MathHelper.Clamp(MathHelper.WrapAngle(wanted-angle),-turn,turn)).ToRotationVector2()*q.velocity.Length();
        q.rotation=q.velocity.ToRotation();
    }
    public override bool PreDraw(Projectile q,ref Color lightColor)
    {
        if(Main.dedServ)return false;
        int id=Style(q);Color body=id==24?new(248,127,54):new(243,83,162),core=id==24?new(255,249,198):new(250,232,255);
        float fade=Math.Min(MathHelper.Clamp(age/4,0,1),MathHelper.Clamp(q.timeLeft/12f,0,1));
        var wake=new MedicFlight("Combat",1,0,0,0,0,0,0,1,id==24?145:105,id==24?6:2);
        // Use this renderer's independent history; don't replace Reed's old private history in its AI.
        var saved=q.oldPos;q.oldPos=history;
        try {MedicIdentityVisuals.DrawWake(q,id,body,core,wake,age,fade);}
        finally {q.oldPos=saved;}
        MedicIdentityVisuals.DrawBody(id,q.Center,MedicIdentityVisuals.Size(id),q.rotation,age,body,core,fade);
        return false;
    }
}
