using System;
using System.IO;
using ArknightsMod.Content.Items.Armor.Caster.Necrass;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass
{
	public sealed class LamentingServant : ModProjectile
	{
		private readonly LamentingServantStats stats = new();
		private int hurtCooldown;

		public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.StardustGuardian}";

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = Main.projFrames[ProjectileID.StardustGuardian];
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.StardustGuardian);
			AIType = ProjectileID.StardustGuardian;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 0f;
			Projectile.timeLeft = 2;
		}

		public static NecrassServant FindFor(Player player) {
			foreach (Projectile projectile in Main.ActiveProjectiles) {
				if (projectile.owner == player.whoAmI && projectile.ModProjectile is NecrassServant servant
					&& servant.ArmorServant && !servant.Dying)
					return servant;
			}
			return null;
		}

		public static void SpawnFor(Player player) {
			if (player.whoAmI != Main.myPlayer || player.dead || FindFor(player) != null)
				return;
			int index = Projectile.NewProjectile(player.GetSource_Misc("NecrassArmor"), player.Center,
				Vector2.Zero, ModContent.ProjectileType<NecrassServant>(), 30, 2f, player.whoAmI,
				NecrassServant.ArmorMode);
			if (Main.projectile.IndexInRange(index))
				Main.projectile[index].originalDamage = 30;
		}

		public void ReinforceAndHeal() {
			if (Projectile.owner != Main.myPlayer)
				return;
			stats.ReinforceAndHeal();
			ApplyAttack();
			Projectile.netUpdate = true;
		}

		private void ApplyAttack() {
			Projectile.originalDamage = stats.Attack;
			Player owner = Main.player[Projectile.owner];
			Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(stats.Attack));
		}

		public override bool PreAI() {
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead || stats.Life <= 0
				|| !owner.GetModPlayer<NecrassSetPlayer>().NecrassHelmetActive) {
				Projectile.Kill();
				return false;
			}

			// 原版AI依赖星尘套装标记续命；这里独立续期，不打开原版星尘套装。
			Projectile.timeLeft = 2;
			ApplyAttack();
			return true;
		}

		public override bool MinionContactDamage() => true;
		public override bool? CanCutTiles() => false;

		// 原版的出拳碰撞是按原版弹幕ID特判的，需要给自定义ID补上同样的碰撞。
		public override bool? CanDamage() => Projectile.ai[0] == 2f ? null : false;
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			Rectangle punch = Utils.CenteredRectangle(Projectile.Center + new Vector2(Projectile.direction * 40, 0),
				new Vector2(80, 40));
			return Projectile.ai[0] == 2f && punch.Intersects(targetHitbox);
		}

		public override void PostAI() {
			ApplyAttack();
			if (Projectile.owner != Main.myPlayer)
				return;
			if (hurtCooldown > 0) {
				hurtCooldown--;
				return;
			}

			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.friendly && npc.damage > 0 && !npc.dontTakeDamage
					&& Projectile.Hitbox.Intersects(npc.Hitbox)) {
					TakeDamage(npc.damage);
					return;
				}
			}
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.hostile && other.damage > 0 && other.whoAmI != Projectile.whoAmI
					&& Projectile.Hitbox.Intersects(other.Hitbox)) {
					TakeDamage(other.damage);
					return;
				}
			}
		}

		private void TakeDamage(int rawDamage) {
			int damage = stats.TakeDamage(rawDamage);
			hurtCooldown = 40;
			Projectile.netUpdate = true;
			if (!Main.dedServ) {
				CombatText.NewText(Projectile.Hitbox, Color.OrangeRed, damage);
				SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center);
			}
			if (stats.Life <= 0)
				Projectile.Kill();
		}

		public override void PostDraw(Color lightColor) {
			if (!Main.dedServ)
				Main.instance.DrawHealthBar(Projectile.Center.X, Projectile.Bottom.Y + 10,
					stats.Life, stats.LifeMax, 1f, 1f);
		}

		public override void SendExtraAI(BinaryWriter writer) {
			stats.Write(writer);
			writer.Write(hurtCooldown);
		}
		public override void ReceiveExtraAI(BinaryReader reader) {
			stats.Read(reader);
			hurtCooldown = reader.ReadInt32();
			ApplyAttack();
		}
	}
}
