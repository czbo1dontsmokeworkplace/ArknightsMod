using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Players;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

// A player clock shared by all five Lords: attack speed and weapon swapping
// cannot create an extra parent wave. The wave's six children belong to it.
public sealed class LordShotClock : ModPlayer
{
    private ulong nextShot;
    internal bool TryRelease(ulong tick)
    {
        if(tick<nextShot)return false;
        nextShot=tick+BladeRouteRules.LordShotInterval;return true;
    }
}

internal static class BladeRouteCombat
{
    internal static bool Swinging(Player player)
    {
        foreach(var p in Main.ActiveProjectiles)if(p.owner==player.whoAmI && p.ModProjectile is OperatorBladeSwing)return true;
        return false;
    }
    internal static void Swing(Player player,IEntitySource source,int kind,int damage,float knockback)
    {
        if(player.whoAmI!=Main.myPlayer)return;
        Vector2 direction=new(player.direction,0);
        float duration=Math.Max(1,player.itemAnimationMax),scale=player.GetAdjustedItemScale(player.HeldItem);
        var skills=player.GetModPlayer<WeaponPlayer>();
        if(kind==7)
        {
            damage=(int)(damage*1.12f);
            if(skills.SkillActive){if(skills.Skill==0)damage=(int)(damage*2.9f);else if(skills.Skill==2){damage*=3;scale*=1.35f;}}
        }
        Projectile.NewProjectile(source,player.MountedCenter,direction,ModContent.ProjectileType<OperatorBladeSwing>(),damage,knockback,player.whoAmI,kind,duration,scale);
        Projectile.NewProjectile(source,player.MountedCenter,direction,ModContent.ProjectileType<OperatorBladeAura>(),damage,knockback,player.whoAmI,kind,duration,scale);
    }
    internal static void FrostleafHit(Player player,NPC target)
    {
        var skills=player.GetModPlayer<WeaponPlayer>();
        if(!skills.SkillActive)return;
        if(skills.Skill==0){target.AddBuff(BuffID.Frozen,180);skills.SkillActive=false;}
        else if(skills.Skill==1){target.AddBuff(BuffID.Slow,180);if(Main.rand.NextFloat()<.4f)target.AddBuff(BuffID.Frozen,120);}
    }
}
