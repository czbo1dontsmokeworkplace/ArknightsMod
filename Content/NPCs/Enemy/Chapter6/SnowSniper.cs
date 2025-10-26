using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Systems.Gameplay.Enums.Damageclasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using Terraria.WorldBuilding;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6
{
	internal class SnowSniper : ModNPC
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 27;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() { // Influences how the NPC looks in the Bestiary
				Velocity = 1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}
		public override void SetDefaults() {
			NPC.width = 17;
			NPC.height = 40;
			NPC.damage = 14;
			NPC.defense = 16;
			NPC.lifeMax = 500;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.BlizzardInsideBuildingLoop;
			NPC.value = 100f;
			NPC.knockBackResist = 0.50f;
			NPC.aiStyle = -1;
			NPC.scale = 1f;

		}
		private int AttackCD = 0;
		private bool attack;
		private bool walk = true;
		private int Framespeed = 9;
		private int framecounter;
		private int attackframeY;
		private float maxspeed = 0.9f;
		private int jumpCD = 0;
		private int directionchoose;

		public override void FindFrame(int frameHeight) {

			attackframeY = 14 * frameHeight;
			NPC.TargetClosest(true);
			framecounter++;
			if (framecounter >= Framespeed) {
				NPC.frame.Y += frameHeight;
				framecounter = 0;
			}
			if (walk==true) {
				if (NPC.velocity == Vector2.Zero)
				{
					if(NPC.frame.Y >= (27 * frameHeight))
					{
						NPC.frame.Y = 23 * frameHeight;
					}
				}
				else
				{
					if (NPC.frame.Y >= attackframeY)
					{
						NPC.frame.Y = 0;
					}
				}
			}
			if (attack == true && (NPC.frame.Y < (attackframeY) || NPC.frame.Y > (22 * frameHeight)))
			{
				NPC.frame.Y = attackframeY;
			}

		}
		public override void AI() {

			Player p = Main.player[NPC.target];
			directionchoose = p.Center.X - NPC.Center.X >= 0 ? 1 : -1;
			float angle = (float)Math.Atan((p.Center.Y - NPC.Center.Y) / (p.Center.X - NPC.Center.X));
			if (walk == true) {
				NPC.spriteDirection = -NPC.direction;
				AttackCD++;
				if (NPC.position.X - p.position.X < -200 || (0 < NPC.position.X - p.position.X && NPC.position.X - p.position.X < 150)) {
					if (NPC.velocity.X < maxspeed) {
						NPC.velocity.X += 0.3f;
					}
					if (NPC.velocity.X >= maxspeed) {
						NPC.velocity.X = maxspeed;
					}
				}

				if (NPC.position.X - p.position.X > 200 || (0 > NPC.position.X - p.position.X && NPC.position.X - p.position.X > -150)) {
					if (NPC.velocity.X > -maxspeed) {
						NPC.velocity.X += -0.3f;
					}
					if (NPC.velocity.X <= -maxspeed) {
						NPC.velocity.X = -maxspeed;
					}
				}

				if (Math.Abs(NPC.velocity.X) <= 0.5f && attack == false) {
					jumpCD++;
				}
				if (jumpCD >= 400) {
					jumpCD = 0;
					NPC.velocity.Y = -7.2f;
				}
				if (AttackCD >= 280 && Math.Abs(NPC.position.X - p.position.X) <= 400 && !attack && Math.Abs(NPC.position.Y - p.position.Y) <= 200) {
					walk = false;
					attack = true;
					AttackCD = 0;
				}
			}
			if (attack == true) {
				NPC.velocity.X = 0;
				AttackCD++;
				if (AttackCD == 18) {
					Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.position.X, NPC.position.Y + 20), new Vector2(directionchoose * 10f, 0).RotatedBy(angle), ModContent.ProjectileType<SnowSniper_Shot>(),14, 0.8f);
				}
				if (AttackCD > 45) {
					attack = false;
					walk = true;
					AttackCD = 0;

				}
			}

		}
		public override bool? CanFallThroughPlatforms() {
			Player player = Main.player[NPC.target];
			return (player.position.Y + player.height) - (NPC.position.Y + NPC.height) > 0;
		}
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			Player p = Main.player[NPC.target];
			if (p.frozen == true) {
				modifiers.SourceDamage *= 1.5f;

			}
			if (Main.expertMode)
				modifiers.SourceDamage *= 1.5f; // 专家模式伤害 ×1.5
			if (Main.masterMode)
				modifiers.SourceDamage *= 2f;   // 大师模式伤害 ×2
		}
	}
		public class SnowSniper_Shot : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 32;
			Projectile.height = 6;
			Projectile.penetrate = 1;
			Projectile.hostile = true;

		}
		private float gravity = 0.15f;

		public override void AI()
		{
			Projectile.position.Y+=gravity;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, 0.833f * Projectile.velocity, 0.01f);
			Projectile.rotation = Projectile.velocity.ToRotation();
			Dust dust;
			Vector2 position = Projectile.Center + new Vector2(0, 3);
			dust = Terraria.Dust.NewDustPerfect(position, DustID.SilverFlame, new Vector2(0f, 0f), 0, new Color(255, 255, 255), 1f);
		}
		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			Projectile.playerImmune[target.whoAmI] = 10;
		}
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			if (target.frozen == true) {
				modifiers.SourceDamage *= 1.5f;
			}
		}
		
	}
}
