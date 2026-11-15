using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis;

public class RosmontisModNPC : GlobalNPC
{
	public static Dictionary<int,int> tagged = new Dictionary<int,int>();

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) {
		bool specialProjectile = projectile.type == ModContent.ProjectileType<RosmontisProjectileBase>() || projectile.type == ModContent.ProjectileType<RosmontisProjectileExplosionInGround>();
		if ( specialProjectile&& tagged.TryGetValue(npc.whoAmI,out int value) &&value> 0) {
				Projectile.NewProjectileDirect(new EntitySource_OnHit(projectile,npc),npc.Center,Vector2.Zero,ModContent.ProjectileType<RosmontisProjectileExplosionInAir>(),projectile.damage,projectile.knockBack,projectile.owner);
		}
	}

	public void TryAddTagged(NPC npc) {
		if (tagged.TryGetValue(npc.whoAmI,out int value)) {
			tagged[npc.whoAmI] = ++value;
		}
		else {
			tagged.Add(npc.whoAmI, 1);
		}
	}

	public void TryRemoveTagged(NPC npc) {
		if (tagged.TryGetValue(npc.whoAmI,out int value)) {
			if (value > 1) {
				tagged[npc.whoAmI] = --value;
			}
			else {
				tagged.Remove(npc.whoAmI);
			}
		}
	}

	public override void OnKill(NPC npc) {
		tagged.Remove(npc.whoAmI);
	}
}