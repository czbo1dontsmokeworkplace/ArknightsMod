using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

internal static class VectorVisualEffects
{
    private static ulong budgetFrame;
    private static int emitted;
    private static bool Visible(Vector2 p) => !Main.dedServ&&p.X>Main.screenPosition.X-180&&p.Y>Main.screenPosition.Y-180
        &&p.X<Main.screenPosition.X+Main.screenWidth+180&&p.Y<Main.screenPosition.Y+Main.screenHeight+180;
    private static void Emit(Vector2 at,Vector2 velocity,Color color,int variant,float size,int life,float rotation=0,float gravity=0,float opacity=1,float spin=0)
    {
        if(!Visible(at))return;
        if(budgetFrame!=Main.GameUpdateCount){budgetFrame=Main.GameUpdateCount;emitted=0;}
        if(emitted>=240||ParticleManager.activeParticles.Count>=1800)return;
        emitted++;
        new T_Vector_Material(at,velocity,color,variant,size,life,rotation,gravity,opacity,spin).Spawn();
    }
    internal static void Flight(Vector2 at,Vector2 velocity,int sample)
    {
        Emit(at,-velocity*.025f,VectorArt.RoundColor,0,30,12,velocity.ToRotation());
        if(sample%7==0)Emit(at,-velocity*.015f,VectorArt.RoundColor,1,6,8,velocity.ToRotation(),opacity:.65f);
    }
    internal static void Contact(Vector2 at,Vector2 reverse,bool wall)
    {
        // Amendment = precision kinetic: 14 directed sparks, 8..24 size, 9..24 ticks, gravity .13.
        Color color=new(255,195,103);
        for(int i=0;i<14;i++) {
            Vector2 velocity=reverse.RotatedBy(Main.rand.NextFloat(-1.2f,1.2f))*Main.rand.NextFloat(2,11);
            Emit(at,velocity,i%5==0?Color.Lerp(color,Color.White,.7f):color,1,
                Main.rand.NextFloat(8,24),Main.rand.Next(9,25),gravity:.13f);
        }
        if(wall)Emit(at,-reverse*.5f,Color.White,4,27,26,Main.rand.Next(4));
    }
    internal static void Wings(Player owner,int mode,VectorWingPose pose)
    {
        if(pose.Pieces==null||!Visible(owner.MountedCenter))return;
        foreach(var p in pose.Pieces)if(p.Active&&p.FadePower>.02f) {
            bool flash=p.Time==12 && p.Index==0&&!p.Retiring;
            for(int side=-1;side<=1;side+=2) {
                Vector2 root=pose.World(side>0?p.Front:p.Back);
                Vector2 tip=VectorArt.WingTip(owner.MountedCenter,side,p,pose);
                if(flash)Emit(root,(tip-root).SafeNormalize(Vector2.UnitX)*2,new Color(255,222,144),1,14,14,opacity:p.FadePower);
                if(p.Index!=0&&p.Index!=6)continue;
                if(Main.GameUpdateCount%8==0)Emit(tip,new Vector2(side*.35f,-.25f),new Color(255,225,162),2,
                    Main.rand.NextFloat(9,14),Main.rand.Next(24,36),Main.rand.NextFloat(MathHelper.TwoPi),.014f,p.FadePower,side*.035f);
                if(p.MotionGlow>.3f&&Main.GameUpdateCount%2==0) {
                    Emit(tip,owner.velocity*Main.rand.NextFloat(-.5f,-.2f),new Color(255,181,67),0,22,12,
                        (tip-root).ToRotation(),opacity:p.FadePower*p.MotionGlow*.65f);
                    Emit(root,new Vector2(0,-.5f),new Color(255,190,95),3,6,18,opacity:p.FadePower*p.MotionGlow*.7f);
                }
            }
        }
    }
}
