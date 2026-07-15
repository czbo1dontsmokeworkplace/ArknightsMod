using System.Collections.Generic;
using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	internal class TyphonSetPlayer : ArknightsArmorPlayer
	{
		public bool TyphonHelmetActive;
		public bool TyphonSetActive;

		private readonly HashSet<int> skillFirstHitTargets = new();
		private bool wasSkillActive;
		private int defIgnoreStacks;
		private int rangedIdleTimer;

		public override void ResetEffects() {
			TyphonHelmetActive = false;
			TyphonSetActive = false;
		}

		public override void PostUpdateEquips() {
			TyphonHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<TyphonHead>());
			TyphonSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<TyphonHead>(),
				ModContent.ItemType<TyphonBody>(),
				ModContent.ItemType<TyphonLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, TyphonSetActive, "Mods.ArknightsMod.ArmorSets.Typhon.SetBonus");
		}

		public override void PostUpdate() {
			bool skillActive = Player.GetModPlayer<WeaponPlayer>().SkillActive;
			if (!skillActive && wasSkillActive)
				skillFirstHitTargets.Clear();

			wasSkillActive = skillActive;

			if (TyphonSetActive) {
				rangedIdleTimer++;
				if (rangedIdleTimer >= 8 * 60) {
					rangedIdleTimer = 8 * 60;
					defIgnoreStacks = 0;
				}
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			ApplyHelmetFirstHit(item, target, ref modifiers);
			ApplySetDefIgnore(item, ref modifiers);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			ApplyHelmetFirstHit(proj, target, ref modifiers);
			ApplySetDefIgnore(proj, ref modifiers);
		}

		private void ApplyHelmetFirstHit(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (!TyphonHelmetActive || !Player.GetModPlayer<WeaponPlayer>().SkillActive)
				return;

			if (!item.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (skillFirstHitTargets.Contains(target.whoAmI))
				return;

			skillFirstHitTargets.Add(target.whoAmI);
			modifiers.SourceDamage *= 1.6f;

			if (Main.netMode != NetmodeID.MultiplayerClient)
				target.AddBuff(ModContent.BuffType<TyphonHelmetSlowDebuff>(), 3 * 60);
		}

		private void ApplyHelmetFirstHit(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (!TyphonHelmetActive || !Player.GetModPlayer<WeaponPlayer>().SkillActive)
				return;

			if (!proj.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (skillFirstHitTargets.Contains(target.whoAmI))
				return;

			skillFirstHitTargets.Add(target.whoAmI);
			modifiers.SourceDamage *= 1.6f;

			if (Main.netMode != NetmodeID.MultiplayerClient)
				target.AddBuff(ModContent.BuffType<TyphonHelmetSlowDebuff>(), 3 * 60);
		}

		private void ApplySetDefIgnore(Item item, ref NPC.HitModifiers modifiers) {
			if (!TyphonSetActive || !item.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			rangedIdleTimer = 0;
			modifiers.ScalingArmorPenetration += 0.1f * defIgnoreStacks;
			defIgnoreStacks = System.Math.Min(5, defIgnoreStacks + 1);
		}

		private void ApplySetDefIgnore(Projectile proj, ref NPC.HitModifiers modifiers) {
			if (!TyphonSetActive || !proj.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			rangedIdleTimer = 0;
			modifiers.ScalingArmorPenetration += 0.1f * defIgnoreStacks;
			defIgnoreStacks = System.Math.Min(5, defIgnoreStacks + 1);
		}
	}
}
