using System;
using Microsoft.Xna.Framework;
using Terraria;
namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class Isharmla
{
    // Motion/trigger references: local Homeward Journey Trial_Forest phase 2
    // (sliding columns), Trial_Snow phase 2 (travelling safe channel), phase 3
    // (brake, suspend, relaunch), Trial_Hallow phase 1 (linked clusters).
    // No reference art, forced arena, survival-only health or collision is copied.
    private void RunTrialScore(Player player,int time,int phrase,bool fierce,Vector2 anchor,Vector2 emitter)
    {
        if(!Authority||time<48||time>340)return;
        Vector2 locked=IsharmlaRules.BoundedAim(player.Center+player.velocity*12);
        int damage=fierce?120:100;
        switch(phrase%4) {
            case 0:
                if(time%24==0) {
                    float shift=(time/24%5)*25-50;
                    float channel=anchor.X+MathF.Sin(time*.012f)*310;
                    for(int i=-4;i<=4;i++) {
                        float x=anchor.X+i*155+shift;
                        if(Math.Abs(x-channel)<110)continue;
                        SpawnShot(TideShot.Harpoon,new Vector2(x,anchor.Y-630),new Vector2(0,fierce?9.4f:8),damage,36,duration:155);
                    }
                }
                break;
            case 1:
                if(time%28==0) {
                    float channel=anchor.X+MathF.Sin(time*.014f)*470;
                    for(int i=-5;i<=5;i++) {
                        float x=anchor.X+i*125+(time%84)/84f*55;
                        if(Math.Abs(x-channel)<145)continue;
                        SpawnShot(TideShot.Harpoon,new Vector2(x,anchor.Y+610),new Vector2(0,fierce?-9:-7.7f),damage,40,duration:170);
                    }
                }
                if(time%90==0) {
                    int side=time%180==0?1:-1;Vector2 at=anchor+new Vector2(side*800,-220);
                    for(int i=-1;i<=1;i++)SpawnShot(TideShot.TideRay,at,(locked-at).SafeNormalize(Vector2.UnitY).RotatedBy(i*.15f)*(fierce?10:8),damage,40,duration:160,target:locked);
                }
                break;
            case 2:
                if(time%88==0) {
                    Vector2 at=Vector2.DistanceSquared(emitter,locked)>320*320?emitter:anchor-new Vector2(0,550);
                    int count=fierce?12:9;
                    for(int i=0;i<count;i++)SpawnShot(TideShot.TideSeed,at,(i*MathHelper.TwoPi/count+time*.007f).ToRotationVector2()*4.8f,damage,30,duration:220,target:locked);
                    IsharmlaPresentation.Cue(5,at);
                }
                break;
            case 3:
                if(time%66==0) {
                    int wave=time/66;
                    for(int side=-1;side<=1;side+=2) {
                        Vector2 at=anchor+new Vector2(side*770,wave%2==0?-480:400);
                        for(int i=-1;i<=1;i++)SpawnShot(TideShot.TideRay,at,(locked-at).SafeNormalize(Vector2.UnitY).RotatedBy(i*.18f)*(fierce?10:8.4f),damage,42,duration:180,style:side,target:locked);
                    }
                }
                break;
        }
    }
}
public sealed partial class IsharmlaDragon
{
    private void LaserWallAssault(Isharmla boss,Player player)
    {
        ContactActive=false;NPC.damage=0;NPC.dontTakeDamage=true;
        // Keep swimming outside the combat centre while body and rider fade away.
        Navigate(Aim+new Vector2(AttackSide*1300,850),28,.038f);
        Charge=IsharmlaRules.Envelope(LocalTimer,1,55,20);
        if(LocalTimer>=108&&LocalTimer<468&&(LocalTimer-108)%60==0) {
            IsharmlaPresentation.Cue(3,Aim);
            if(!Main.dedServ){IsharmlaPresentation.Impact(Aim,2.1f);IsharmlaVisuals.Shake(5);}
        }
        int waveTime=LocalTimer-60;
        if(waveTime<0||waveTime>=360||waveTime%60!=0)return;
        int wave=waveTime/60;
        // A fixed corridor is announced for 48 ticks; it never tracks the player
        // after a wall is emitted. Alternating axes force decisive movement.
        bool vertical=wave%2==0;
        Vector2 anchor=Aim;
        float corridor=(wave%3-1)*190;
        for(int lane=-5;lane<=5;lane++) {
            float offset=lane*145;
            if(Math.Abs(offset-corridor)<165)continue;
            Vector2 at=anchor+(vertical?new Vector2(offset,-1150):new Vector2(-1450,offset));
            boss.SpawnShot(TideShot.Lattice,at,vertical?Vector2.UnitY:Vector2.UnitX,145,48,vertical?2300:2900,32,32,style:wave);
        }
        IsharmlaPresentation.Cue(5,anchor);
        if(!Main.dedServ){IsharmlaVisuals.Shake(3.5f);IsharmlaPresentation.Impact(anchor+new Vector2(0,-450),1.3f);}
    }
}
