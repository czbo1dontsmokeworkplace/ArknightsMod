using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Specter;
using ArknightsMod.Content.Projectiles.Guard.Blaze;
using ArknightsMod.Content.Projectiles.BasePROJ;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Specter
{
	/// <summary>
	/// 持续工作的长柄圆锯：每个七帧咬合周期内，所有与锯身接触的敌人各受一次伤害。
	/// </summary>
	public sealed class SpecterBoneSawHoldout : ModProjectile
	{
		private const float Reach = 92f;
		private const float SawWidth = 40f;
		private const int BiteCycleTicks = 7;

		private readonly HashSet<int> cycleTargets = [];
		private int age;
		private float aimAngle;
		private float angularVelocity;
		private float spool;
		private float chainPhase;
		private bool initialized;
		private bool ejectaSpawnedThisCycle;
		private Vector2 handWorld;
		private Vector2 drawHandWorld;
		private Vector2 tipWorld;

		private Player Owner => Main.player[Projectile.owner];
		private int SkillMode => (int)Projectile.ai[1];

		public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Specter/SpecterBoneSaw";

		public override void SetDefaults() {
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ownerHitCheck = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = -1;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.netImportant = true;
			Projectile.timeLeft = 2;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
			aimAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
			initialized = true;
		}

		public override void AI() {
			Player player = Owner;
			if (!player.active || player.dead || player.noItems || player.CCed
				|| player.HeldItem.ModItem is not SpecterBoneSaw) {
				Projectile.Kill();
				return;
			}

			if (player.whoAmI == Main.myPlayer && !player.channel) {
				Projectile.Kill();
				return;
			}

			Projectile.timeLeft = 2;
			if (!initialized) {
				aimAngle = Projectile.velocity.SafeNormalize(new Vector2(player.direction, 0f)).ToRotation();
				initialized = true;
			}

			UpdateAim(player);
			UpdateSkillAndDamage(player);
			spool = MathHelper.Lerp(spool, 1f, SkillMode == 2 ? 0.18f : 0.12f);
			chainPhase += MathHelper.Lerp(0.16f, SkillMode == 2 ? 0.98f : 0.76f, spool);

			Vector2 direction = aimAngle.ToRotationVector2();
			Vector2 normal = new(-direction.Y, direction.X);
			float vibrationStrength = SkillMode == 2 ? 1.8f : 1.25f;
			float vibration = MathF.Sin(age * (1.54f + spool * 0.38f))
				* (0.45f + spool * vibrationStrength);
			handWorld = player.MountedCenter + direction * 4f;
			drawHandWorld = handWorld + direction * vibration;
			tipWorld = drawHandWorld + direction * Reach;

			Projectile.Center = (handWorld + tipWorld) * 0.5f;
			Projectile.velocity = direction;
			Projectile.rotation = aimAngle;
			Projectile.spriteDirection = direction.X < 0f ? -1 : 1;
			player.ChangeDir(direction.X < 0f ? -1 : 1);
			player.itemTime = 2;
			player.itemAnimation = 2;
			BaseHeldMeleeSupport.HoldAndPose(player, Projectile, handWorld, direction.X < 0f);

			UpdateBiteCycle();
			UpdateMotorEffects(direction, normal);
			Vector3 light = SkillMode == 2
				? new Vector3(0.62f, 0.035f, 0.08f)
				: SkillMode == 1
					? new Vector3(0.20f, 0.55f, 0.68f)
					: new Vector3(0.08f, 0.25f, 0.34f);
			Lighting.AddLight(Vector2.Lerp(handWorld, tipWorld, 0.72f), light * (0.55f + spool * 0.55f));
			age++;
		}

		private void UpdateSkillAndDamage(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return;

			int desiredMode = player.GetModPlayer<SpecterBoneSawPlayer>().ActiveSkillMode;
			int desiredDamage = player.GetWeaponDamage(player.HeldItem);
			bool changed = false;
			if ((int)Projectile.ai[1] != desiredMode) {
				Projectile.ai[1] = desiredMode;
				changed = true;
			}
			if (Projectile.damage != desiredDamage) {
				Projectile.damage = Math.Max(1, desiredDamage);
				Projectile.originalDamage = Projectile.damage;
				changed = true;
			}
			if (changed)
				Projectile.netUpdate = true;
		}

		private void UpdateAim(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return;

			Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
			if (toMouse.LengthSquared() < 16f)
				toMouse = new Vector2(player.direction, 0f);
			float targetAngle = toMouse.ToRotation();
			float difference = MathHelper.WrapAngle(targetAngle - aimAngle);
			float distance = MathF.Abs(difference);

			// 和煌使用同一套有重量的转向响应，保证两把链锯的操控质感一致。
			float response = MathHelper.Lerp(0.075f, 0.23f,
				1f - MathHelper.Clamp(distance / 1.15f, 0f, 1f));
			float desiredVelocity = MathHelper.Clamp(difference * 0.34f, -0.145f, 0.145f);
			angularVelocity = MathHelper.Lerp(angularVelocity, desiredVelocity, response);
			angularVelocity *= distance < 0.025f ? 0.55f : 0.94f;
			aimAngle += angularVelocity;

			if (age % 6 == 0 || distance > 0.18f)
				Projectile.netUpdate = true;
		}

		private void UpdateBiteCycle() {
			if (age % BiteCycleTicks != 0)
				return;
			cycleTargets.Clear();
			ejectaSpawnedThisCycle = false;
			Projectile.ResetLocalNPCHitImmunity();
		}

		private void UpdateMotorEffects(Vector2 direction, Vector2 normal) {
			if (Main.dedServ)
				return;

			if (age % 7 == 0) {
				SoundEngine.PlaySound(SpecterVisuals.MotorSound with {
					Volume = 0.31f + spool * 0.18f,
					Pitch = SkillMode == 2 ? -0.26f : SkillMode == 1 ? 0.18f : -0.04f,
					MaxInstances = 4
				}, handWorld);
			}

			if (age % 2 == 0 && spool > 0.35f) {
				Vector2 position = Vector2.Lerp(handWorld, tipWorld, Main.rand.NextFloat(0.36f, 1f));
				Vector2 tangent = normal * (Main.rand.NextBool() ? 1f : -1f) + direction * 0.24f;
				SpecterVisuals.SpawnSawMotes(position, tangent, SkillMode, spool,
					SkillMode == 2 ? 3 : 2);
			}
		}

		public override bool? CanDamage() => spool >= 0.32f ? null : false;

		public override bool? CanHitNPC(NPC target) => cycleTargets.Contains(target.whoAmI) ? false : null;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (spool < 0.32f)
				return false;
			// Native ownerHitCheck handles terrain; this hook only tests the rotating saw blade.
			Vector2 direction = aimAngle.ToRotationVector2();
			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				handWorld + direction * 17f, tipWorld, SawWidth, ref collisionPoint);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (damageDone > 0)
				CenturionHitVisuals.BloodSpray(target, aimAngle.ToRotationVector2(),
					CenturionHitVisuals.SpecterIntensity, cycleTargets.Count, CenturionHitVisuals.SpecterColor);
			cycleTargets.Add(target.whoAmI);
			Vector2 direction = aimAngle.ToRotationVector2();
			if (damageDone > 0 && !ejectaSpawnedThisCycle) {
				ejectaSpawnedThisCycle = true;
				CenturionHitVisuals.SawEjecta(Projectile, Vector2.Lerp(handWorld, tipWorld, 0.82f), direction);
			}
			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.NPCHit18 with {
					Volume = SkillMode == 2 ? 0.72f : 0.46f,
					Pitch = SkillMode == 2 ? -0.32f : -0.02f,
					MaxInstances = 6
				}, target.Center);
				if (Owner.whoAmI == Main.myPlayer)
					Owner.GetModPlayer<BlazeImpactShakePlayer>().AddImpact(
						SkillMode == 2 ? 5 : 3, SkillMode == 2 ? 3.9f : 2.1f, specter: true);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			if (Main.dedServ)
				return false;

			Texture2D weapon = TextureAssets.Item[ModContent.ItemType<SpecterBoneSaw>()].Value;
			bool faceLeft = MathF.Cos(aimAngle) < 0f;
			int frame = age / 4 % 3;
			BaseHeldMeleeSupport.BeginAdditive(Main.spriteBatch);
			Color edge = SkillMode == 2 ? new Color(218, 20, 60, 155) : new Color(92, 196, 220, 145);
			float pulse = 0.9f + MathF.Sin((float)Main.GlobalTimeWrappedHourly * 7f) * 0.1f;
			for (int i = 0; i < 8; i++) {
				Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (SkillMode == 2 ? 2.4f : 1.8f);
				DrawSaw(weapon, faceLeft, edge * (0.65f * spool * pulse * 1.5f), frame, offset);
			}
			BaseHeldMeleeSupport.EndAdditive(Main.spriteBatch);
			DrawSaw(weapon, faceLeft, lightColor, frame);

			BaseHeldMeleeSupport.BeginAdditive(Main.spriteBatch);
			Color ghostLight = SkillMode == 2
				? new Color(210, 12, 48, 105)
				: new Color(92, 196, 220, 86);
			DrawSaw(weapon, faceLeft, ghostLight * (0.18f + spool * 0.18f), frame);
			SpecterVisuals.DrawSawCurrent(handWorld, tipWorld, spool, chainPhase, SkillMode);
			BaseHeldMeleeSupport.EndAdditive(Main.spriteBatch);
			return false;
		}

		private void DrawSaw(Texture2D sheet, bool faceLeft, Color color, int frame, Vector2 offset = default) {
			const int frameHeight = 72;
			Vector2 grip = new(8f, 65f);
			Vector2 tip = new(59f, 11f);
			float nativeAngle = (tip - grip).ToRotation();
			if (faceLeft)
				grip.Y = frameHeight - grip.Y;
			Main.spriteBatch.Draw(sheet, drawHandWorld + offset - Main.screenPosition,
				new Rectangle(0, frame * frameHeight, 74, frameHeight), color,
				faceLeft ? aimAngle + nativeAngle : aimAngle - nativeAngle,
				grip, Reach / (tip - new Vector2(8f, 65f)).Length(),
				faceLeft ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
		}

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write(aimAngle);
			writer.Write(angularVelocity);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			aimAngle = reader.ReadSingle();
			angularVelocity = reader.ReadSingle();
			initialized = true;
		}
	}
}
