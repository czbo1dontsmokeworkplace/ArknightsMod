using ArknightsMod.Content.Projectiles.Summoner;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;

namespace ArknightsMod.Common.GlobalProjectiles
{
	// 为触手添加生命值和防御
	public class DeepcolorMinionLifeGlobalProj : GlobalProjectile
	{
		public const int DefaultLifeMax = 100;
		public const int DefaultDefense = 10;

		public override bool InstancePerEntity => true;

		public bool useLife;
		public bool drawHealthBar = true;
		public int life = -1;
		public int lifeMax = DefaultLifeMax;
		public int defense = DefaultDefense;
		public float damageReduction = 1f;
		public int hitCooldown = 30;
		// 召唤顺序，用于满员时替换最早的一只触手
		public int spawnOrder;

		private static readonly Dictionary<int, int> NextSpawnOrderByPlayer = new();

		private int cooldown;

		public override void PostAI(Projectile projectile) {
			if (!useLife || projectile.type != ModContent.ProjectileType<DeepcolorMinion>())
				return;

			if (life < 0)
				life = lifeMax;

			if (cooldown > 0)
				cooldown--;

			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.friendly && !npc.dontTakeDamage && projectile.Hitbox.Intersects(npc.Hitbox) && cooldown <= 0) {
					TakeDamage(projectile, npc.damage);
					cooldown = hitCooldown;
					break;
				}
			}

			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.whoAmI == projectile.whoAmI)
					continue;
				if ((other.hostile || !other.friendly) && projectile.Hitbox.Intersects(other.Hitbox) && cooldown <= 0) {
					TakeDamage(projectile, other.damage);
					cooldown = hitCooldown;
					break;
				}
			}

			if (life <= 0) {
				DeepcolorMinion.SpawnDeathParticles(projectile);
				projectile.Kill();
			}
		}

		public void TakeDamage(Projectile projectile, int damage) {
			damage = (int)((damage - defense) * damageReduction);
			if (damage < 1)
				damage = 1;

			life -= damage;
			CombatText.NewText(projectile.getRect(), Color.Red, damage);
			SoundEngine.PlaySound(SoundID.NPCHit1, projectile.Center);

			if (life <= 0)
				SoundEngine.PlaySound(SoundID.NPCDeath4, projectile.Center);
		}

		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			if (projectile.type != ModContent.ProjectileType<DeepcolorMinion>())
				return;

			useLife = true;
			lifeMax = DefaultLifeMax;
			life = lifeMax;
			defense = DefaultDefense;
			drawHealthBar = true;

			if (!NextSpawnOrderByPlayer.TryGetValue(projectile.owner, out int nextOrder))
				nextOrder = 0;
			spawnOrder = nextOrder;
			NextSpawnOrderByPlayer[projectile.owner] = nextOrder + 1;
		}

		public override void PostDraw(Projectile projectile, Player player, Color lightColor) {
			if (!useLife || !drawHealthBar || projectile.type != ModContent.ProjectileType<DeepcolorMinion>())
				return;

			if (Main.netMode == NetmodeID.Server)
				return;

			DrawHealthBar(projectile);
			ShowHealthOnHover(projectile);
		}

		private void DrawHealthBar(Projectile projectile) {
			// 原版的血条样式
			float footY = projectile.Bottom.Y + DeepcolorMinion.GroundOverlapPixels;
			float barY = footY + 10f + projectile.gfxOffY;
			float alpha = Lighting.Brightness(
				(int)(projectile.Center.X / 16f),
				(int)((projectile.Center.Y + projectile.gfxOffY) / 16f));

			Main.instance.DrawHealthBar(projectile.Center.X, barY, life, lifeMax, alpha, 1f);
		}

		private void ShowHealthOnHover(Projectile projectile) {
			Vector2 mouse = Main.MouseWorld;
			if (!new Rectangle((int)mouse.X - 10, (int)mouse.Y - 10, 20, 20).Intersects(projectile.getRect()))
				return;

			float textWorldY = projectile.Bottom.Y + DeepcolorMinion.GroundOverlapPixels + 26f + projectile.gfxOffY;
			Vector2 textPos = new Vector2(projectile.Center.X, textWorldY) - Main.screenPosition;
			string text = $"{life}/{lifeMax}";
			Vector2 origin = FontAssets.MouseText.Value.MeasureString(text) * 0.5f;
			ChatManager.DrawColorCodedStringWithShadow(
				Main.spriteBatch,
				FontAssets.MouseText.Value,
				text,
				textPos,
				Color.White,
				0f,
				origin,
				Vector2.One * 0.85f);
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) {
			binaryWriter.Write(useLife);
			if (!useLife)
				return;
			binaryWriter.Write(drawHealthBar);
			binaryWriter.Write(lifeMax);
			binaryWriter.Write(life);
			binaryWriter.Write(defense);
			binaryWriter.Write(damageReduction);
			binaryWriter.Write(hitCooldown);
			binaryWriter.Write(cooldown);
			binaryWriter.Write(spawnOrder);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) {
			useLife = binaryReader.ReadBoolean();
			if (!useLife)
				return;
			drawHealthBar = binaryReader.ReadBoolean();
			lifeMax = binaryReader.ReadInt32();
			life = binaryReader.ReadInt32();
			defense = binaryReader.ReadInt32();
			damageReduction = binaryReader.ReadSingle();
			hitCooldown = binaryReader.ReadInt32();
			cooldown = binaryReader.ReadInt32();
			spawnOrder = binaryReader.ReadInt32();
		}
	}

	public static class DeepcolorMinionLifeExtensions
	{
		public static DeepcolorMinionLifeGlobalProj MinionLife(this Projectile projectile)
			=> projectile.GetGlobalProjectile<DeepcolorMinionLifeGlobalProj>();
	}
}
