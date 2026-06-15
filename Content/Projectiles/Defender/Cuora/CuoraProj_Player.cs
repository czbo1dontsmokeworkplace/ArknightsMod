using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Cuora;
using ArknightsMod.Content.Items.Weapons.Defender.Durnar;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Cuora;
public class CuoraProj_Player : ModPlayer
{
	public bool DefensiveStance = false;//龟龟形态！！！
	public override void PostUpdate()
	{
		var it = Player.HeldItem;
		if (it.type == ModContent.ItemType<CuoraWeapon>()) {
			if (Player.ownedProjectileCounts[ModContent.ProjectileType<Cuora_Bat>()] == 0)
				Projectile.NewProjectile(Player.GetSource_FromThis(), Player.MountedCenter - Main.screenPosition,
					Vector2.One, ModContent.ProjectileType<Cuora_Bat>()
					, it.damage, it.knockBack);
			if (Player.ownedProjectileCounts[ModContent.ProjectileType<Cuora_Shield>()] == 0)
				Projectile.NewProjectile(Player.GetSource_FromThis(), Player.MountedCenter - Main.screenPosition,
					Vector2.One, ModContent.ProjectileType<Cuora_Shield>()
					, it.damage, it.knockBack);
		}
	}
	private int time = 0;
	public override void UpdateEquips()
	{
		var it = Player.HeldItem;
		var modPlayer = Player.GetModPlayer<WeaponPlayer>();
		if(it.type == ModContent.ItemType<CuoraWeapon>()&&Main.mouseRight)
		{
			Player.statDefense += 10;
			Player.noKnockback = true;
		}

		if (DefensiveStance && !modPlayer.SkillActive) {
			DefensiveStance = false;
			time = 0;
		}
		if (DefensiveStance) {
			Player.moveSpeed *= 0.9f;
			time++;
			if (time % 60f == 0) {
				Player.statLife += (Player.statLifeMax2+Player.statLifeMax)/100;
				if(Player.statLife > Player.statLifeMax2+Player.statLifeMax)
					Player.statLife = Player.statLifeMax2+Player.statLifeMax;
			}

		}
	}
}