using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// SupernovaBomb's flight-only composition: offset copies of the actual item,
// short sprite afterimages and detached points/sparks. The three spinning items
// additionally carry Subsuming Vortex's historical-path streak trail.
internal static class MedicSpriteFlightVisuals
{
    internal static bool Handles(int id) => id is 0 or 1 or 3 or 9 or 16;
    internal static bool Spins(int id) => id is 0 or 1 or 3;
    // Age is measured in real frames and is already synchronized by MedicArt.
    // Keep the projectile's logical rotation/trajectory independent of this spin.
    internal static float SpriteRotation(int id,float age,float direction) => Spins(id)
        ? age*.22f
        : direction+(id==16?MathHelper.PiOver2:0);
    internal static string SpritePath(int id) => id switch
    {
        0 => "ArknightsMod/Content/Projectiles/Medic/Arts/AnselHealingArt",
        1 => "ArknightsMod/Content/Projectiles/Medic/Arts/MyrrhHealingArt",
        3 => "ArknightsMod/Content/Projectiles/Medic/Arts/SilenceHealingArt",
        9 => "ArknightsMod/Content/Projectiles/Medic/Arts/LumenHealingArt",
        _ => "Terraria/Images/Projectile_1"
    };

    internal static void Draw(MedicArt art,float opacity)
    {
        if(Main.dedServ)return;
        var q=art.Projectile;int id=art.ProfileId;
        string path=SpritePath(id);
        if(!MedicArtsVfx.Materials.TryGetValue(path,out Texture2D texture) || texture==null)
            MedicArtsVfx.Materials[path]=texture=ModContent.Request<Texture2D>(path,AssetRequestMode.ImmediateLoad).Value;
        float rotation=SpriteRotation(id,art.Age,q.rotation);
        if(Spins(id))MedicVortexVisuals.Draw(art,opacity);
        Vector2 origin=texture.Size()*.5f;
        // The pistol lives in the player's hand. Only the vanilla bullet texture flies.
        Vector2 scale=id==16?new Vector2(4,10)/texture.Size():Vector2.One;
        Vector2 center=q.Center-Main.screenPosition;
        Color edge=Color.Lerp(art.Profile.Body,Color.White,.45f);
        int ghosts=0;
        for(int i=1;i<q.oldPos.Length && ghosts<4;i+=2)
        {
            if(q.oldPos[i]==Vector2.Zero)break;
            Vector2 previous=q.oldPos[i]+q.Size*.5f;
            if(Vector2.DistanceSquared(previous,q.Center)>36*36)break;
            float fade=(1-ghosts/4f)*.16f*opacity;
            float oldRotation=SpriteRotation(id,art.Age-i/(float)q.MaxUpdates,q.rotation);
            Main.EntitySpriteDraw(texture,previous-Main.screenPosition,null,edge*fade,oldRotation,origin,scale,SpriteEffects.None);
            ghosts++;
        }
        // Seven orbiting offset copies preserve the authored silhouette, including
        // the small vial's details. A narrow edge is intentional at native pixel size.
        float radius=(id==16?.65f:1.05f)+MathF.Sin(art.Age*.08f)*.18f;
        for(int i=0;i<7;i++)
        {
            Vector2 offset=(i*MathHelper.TwoPi/7+art.Age*.04f).ToRotationVector2()*radius;
            Main.EntitySpriteDraw(texture,center+offset,null,edge*(.24f*opacity),rotation,origin,scale,SpriteEffects.None);
        }
        Main.EntitySpriteDraw(texture,center,null,(id==16?art.Profile.Core:Color.White)*opacity,rotation,origin,scale,SpriteEffects.None);
    }

    internal static void Flight(MedicArt art,Vector2 previous)
    {
        if(Main.dedServ || ParticleManager.activeParticles.Count>=1200)return;
        var q=art.Projectile;int id=art.ProfileId;
        Vector2 axis=q.velocity.SafeNormalize(Vector2.UnitX),side=axis.RotatedBy(MathHelper.PiOver2);
        Vector2 rear=q.Center-axis*(id==16?5:id==1?3:9);
        // Sample between the last real-frame positions. ExtraUpdates never doubles
        // emission; MedicArt invokes this only on its last sub-update.
        Vector2 at=Vector2.Lerp(previous-axis*4,rear,Main.rand.NextFloat(.45f,1f))
            +side*Main.rand.NextFloat(-2,2);
        Emit(at,-q.velocity*.09f+Main.rand.NextVector2Circular(.25f,.25f),id,art.Profile.Core,false,12,1.6f);
        if((int)art.Age%2==0)
            Emit(rear+side*Main.rand.NextFloat(-3,3),-q.velocity*.15f+side*Main.rand.NextFloat(-.45f,.45f),
                id,art.Profile.Body,true,18,Main.rand.NextFloat(2.5f,4.5f));
    }

    internal static void Muzzle(Vector2 position,Vector2 direction)
    {
        for(int i=0;i<4;i++)
            Emit(position,direction.RotatedBy(Main.rand.NextFloat(-.35f,.35f))*Main.rand.NextFloat(1,3),
                16,new Color(191,255,226),true,8,3);
    }

    private static void Emit(Vector2 at,Vector2 velocity,int profile,Color color,bool spark,int lifetime,float size)
    {
        if(Main.dedServ || ParticleManager.activeParticles.Count>=1200)return;
        new T_Medic_Item_Spark(at,velocity,profile,color,spark,lifetime,size).Spawn();
    }
}
