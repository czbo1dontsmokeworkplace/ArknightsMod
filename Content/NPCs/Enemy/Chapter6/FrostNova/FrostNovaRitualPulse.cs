using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;
// Content identity retained; now an invisible, harmless straight-salvo coordinator.
public sealed class FrostNovaRitualPulse : ModProjectile
{
    internal const int Warning=60,Interval=40,Waves=3;
    private int age;private Vector2 lockedAim;private bool prepared;
    internal int VisualAge=>age;
    public override string Texture=>"ArknightsMod/Assets/null";
    public override void SetDefaults(){Projectile.width=Projectile.height=2;Projectile.hostile=false;Projectile.damage=0;Projectile.penetrate=-1;Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.timeLeft=Warning+Interval*Waves;Projectile.netImportant=true;}
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>false;
    public override void AI()
    {
        age++;
        var owner=Projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>();
        if(!prepared&&Main.netMode!=NetmodeID.MultiplayerClient)
        {
            if(!FrostNovaOwnedProjectile.Resolve(owner.Host,owner.Token,out FrostNova boss))return;
            Player target=Main.player[boss.NPC.target];lockedAim=target.Center+target.velocity*12;prepared=true;
            for(int wave=0;wave<Waves;wave++)for(int lane=-2;lane<=2;lane++)
            {
                Vector2 at,velocity;
                if(wave==2){at=lockedAim+new Vector2(lane*125,-430);velocity=Vector2.UnitY*7.5f;}
                else{int side=wave==0?-1:1;at=lockedAim+new Vector2(side*550,lane*95-120);velocity=new Vector2(-side*7,0);}
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),at,velocity,ModContent.ProjectileType<FrostNovaBarrage>(),FrostNovaCombatRules.ShotDamage(boss.AirbornePhase?2:1),0,Main.myPlayer,owner.Host,0,Warning+wave*Interval);
            }
            Projectile.netUpdate=true;
        }
        if(age==1||age==Warning)FrostNovaColdMistSystem.Begin(Projectile.Center);
    }
    public override bool? Colliding(Rectangle projectileHitbox,Rectangle targetHitbox)=>false;
    public override bool PreDraw(ref Color lightColor)=>false;
    public override void SendExtraAI(BinaryWriter w){w.Write(age);w.Write(prepared);w.Write(lockedAim.X);w.Write(lockedAim.Y);}
    public override void ReceiveExtraAI(BinaryReader r){age=r.ReadInt32();prepared=r.ReadBoolean();lockedAim=new(r.ReadSingle(),r.ReadSingle());}
}
