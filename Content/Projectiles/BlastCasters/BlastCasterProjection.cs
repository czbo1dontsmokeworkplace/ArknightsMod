using Microsoft.Xna.Framework;
using Terraria;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

internal readonly record struct BlastCasterShotFrame(Vector2 Origin,Vector2 Axis)
{
    internal Vector2 Direction(Vector2 local)=>Axis*local.X+new Vector2(-Axis.Y,Axis.X)*local.Y;
    internal Vector2 Position(Vector2 local)=>Origin+Direction(local);
}

internal static class BlastCasterProjection
{
    internal static BlastCasterShotFrame Nozzle(int ownerId,BlastCasterKind kind,int mode,Vector2 bornAt,Vector2 bornOwner,Vector2 bornAxis)
    {
        Player owner=Main.player[ownerId];Vector2 axis=bornAxis.SafeNormalize(Vector2.UnitX);
        bool hasHoldout=false;
        if(owner.heldProj>=0 && owner.heldProj<Main.maxProjectiles)
        {
            var held=Main.projectile[owner.heldProj];
            if(held.active && held.owner==ownerId && held.ModProjectile is BlastCasterHoldout h && h.Kind==kind)
            {axis=held.velocity.SafeNormalize(axis);hasHoldout=true;}
        }
        if(owner.HeldItem.ModItem is not BlastCasterWeapon weapon || weapon.Kind!=kind)
            return new(bornAt+owner.MountedCenter-bornOwner,axis);
        if(!hasHoldout)
            axis=(owner.itemRotation.ToRotationVector2()*owner.direction).SafeNormalize(axis);
        Vector2 muzzle=BlastCasterHoldout.Muzzle(owner,axis,kind);
        if(kind==BlastCasterKind.Ifrit && mode==3)
        {
            int size=(int)System.MathF.Ceiling(BlastCasterRules.FluxContactRadius*2*BlastCasterRules.ShotSize(kind,mode));
            muzzle=owner.MountedCenter+Collision.TileCollision(owner.MountedCenter-new Vector2(size*.5f),muzzle-owner.MountedCenter,size,size,true,true);
        }
        return new(muzzle,axis);
    }
    internal static BlastCasterShotFrame Relay(int ownerId,int identity,Vector2 origin,Vector2 axis)
    {
        var relay=AphrissaRelay.Find(Main.player[ownerId]);
        if(relay!=null && relay.identity==identity)
            return new(relay.Center,relay.localAI[0]>0?relay.rotation.ToRotationVector2():axis);
        return new(origin,axis);
    }
    internal static PhoenixFxStyle Style(int owner,BlastCasterKind kind,int mode,float scale,Vector2 origin,Vector2 pivot,Vector2 axis,int relayIdentity=-1)
    {
        var style=PhoenixFx.BlastCaster(BlastCasterRules.Tint(kind),BlastCasterRules.FluxAccent(kind),scale);
        style.MonochromeTexture=kind==BlastCasterKind.Ifrit;
        BlastCasterShotFrame frame=default;ulong tick=ulong.MaxValue;
        BlastCasterShotFrame Current()
        {
            if(tick!=Main.GameUpdateCount)
            {
                tick=Main.GameUpdateCount;
                frame=relayIdentity>=0?Relay(owner,relayIdentity,origin,axis):Nozzle(owner,kind,mode,origin,pivot,axis);
            }
            return frame;
        }
        style.ProjectPosition=p=>Current().Position(p);style.ProjectDirection=d=>Current().Direction(d);
        return style;
    }
}
