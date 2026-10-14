using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.Click;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

// One player-side unit owns the whole selection. No per-enemy drone or persistent marker projectile.
public sealed class ClickDrone : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Goldenglow/GoldenglowBeacon";
    private readonly List<(int Index,int Type)> targets = new();
    private int age = -1, batchDamage;
    private float spacing = 12;
    private bool stun;
    internal bool Busy => age >= 0;
    internal static bool VolleyDue(int tick,float interval) =>
        tick==(int)MathF.Ceiling(interval)||tick==(int)MathF.Ceiling(interval*2);
    public override void SetDefaults()
    {
        Projectile.width=14; Projectile.height=24; Projectile.timeLeft=120;
        Projectile.tileCollide=false; Projectile.ignoreWater=true; Projectile.netImportant=true;
    }
    public override bool? CanDamage()=>false;
    internal static ClickDrone Ensure(Player player)
    {
        foreach(Projectile p in Main.ActiveProjectiles)
            if(p.owner==player.whoAmI && p.ModProjectile is ClickDrone drone) return drone;
        int id=Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem),
            player.Center+new Vector2(-34*player.direction,-48),Vector2.Zero,
            ModContent.ProjectileType<ClickDrone>(),0,0,player.whoAmI);
        return id<Main.maxProjectiles ? Main.projectile[id].ModProjectile as ClickDrone : null;
    }
    internal void Begin(List<NPC> selected,int damage,bool applyStun,float speed)
    {
        if(Busy || selected.Count==0) return;
        targets.Clear(); foreach(NPC npc in selected) targets.Add((npc.whoAmI,npc.type));
        age=0;spacing=Math.Max(5,12f/speed);batchDamage=damage;stun=applyStun;
        Fire(false); Projectile.netUpdate=true;
    }
    private bool Valid((int Index,int Type) entry,out NPC npc)
    {
        npc=entry.Index>=0&&entry.Index<Main.maxNPCs?Main.npc[entry.Index]:null;
        return npc!=null && npc.type==entry.Type && npc.CanBeChasedBy() &&
            npc.DistanceSQ(Main.player[Projectile.owner].Center)<1600f*1600f;
    }
    private void Fire(bool laser)
    {
        int fired=0;
        foreach(var target in targets)
            if(Valid(target,out NPC npc))
            {
                ClickTrackerLaser.Spawn(Projectile,npc,batchDamage,laser,stun,targets.Count);
                fired++;
            }
        if(laser&&fired>0)
            SoundEngine.PlaySound(SoundID.Item12 with { MaxInstances=1 },Projectile.Center);
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(age);w.Write(spacing);w.Write((byte)targets.Count);
        foreach(var t in targets){w.Write((short)t.Index);w.Write(t.Type);}
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        age=r.ReadInt32();spacing=Math.Clamp(r.ReadSingle(),5,24);int count=r.ReadByte();targets.Clear();
        for(int i=0;i<count;i++){int index=r.ReadInt16(),type=r.ReadInt32();if(i<80)targets.Add((index,type));}
    }
    public override void AI()
    {
        Player player=Main.player[Projectile.owner];
        if(!player.active||player.dead||player.noItems||player.CCed||player.HeldItem.ModItem is not ClickStaff)
        {Projectile.Kill();return;}
        Projectile.timeLeft=120;
        Vector2 goal=player.Center+new Vector2(-34*player.direction,-48+MathF.Sin(Main.GlobalTimeWrappedHourly*2)*3);
        Projectile.velocity=(goal-Projectile.Center)*.25f;
        Projectile.rotation=Projectile.velocity.X*.01f;
        if(age>=0)
        {
            age++;
            if(Projectile.owner==Main.myPlayer && VolleyDue(age,spacing)) Fire(true);
            if(!Main.dedServ && age%12==0)
                foreach(var t in targets) if(Valid(t,out NPC npc))
                    CasterSparkles.Spawn(npc.Top-new Vector2(0,5),Vector2.Zero,Color.White,.35f,14);
            if(age>spacing*2+15){age=-1;targets.Clear();Projectile.netUpdate=true;}
        }
        if(Projectile.owner==Main.myPlayer&&Main.GameUpdateCount%6==0)Projectile.netUpdate=true;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture=ModContent.Request<Texture2D>(Texture).Value;
        Main.EntitySpriteDraw(texture,Projectile.Center-Main.screenPosition,null,lightColor,
            Projectile.rotation,texture.Size()*.5f,1,SpriteEffects.None);
        MechAccordVfx.DrawLens(Projectile.Center,false,14,age);
        if(age>=0)
        {
            int since=age-(int)MathF.Ceiling(age>=MathF.Ceiling(spacing*2)?spacing*2:spacing);
            if(since>=0&&since<15)
            {
                using var light=new MechAccordVfx.Pass(BlendState.Additive);
                float fade=MathF.Pow(1-since/15f,3);
                MechAccordVfx.Mask("Bloom",Projectile.Center,new Vector2(24),MechAccordVfx.Fade(new Color(243,11,33),fade));
                MechAccordVfx.Mask("Bloom",Projectile.Center,new Vector2(12),MechAccordVfx.Fade(Color.White,.65f*fade));
            }
        }
        return false;
    }
}
