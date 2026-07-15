using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Mlynar
{
	internal class MlynarSetPlayer : ArknightsArmorPlayer
	{
		public bool MlynarHelmetActive;
		public bool MlynarSetActive;

		public override void ResetEffects() {
			MlynarHelmetActive = false;
			MlynarSetActive = false;
		}

		public override void PostUpdateEquips() {
			MlynarHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MlynarHead>());
			MlynarSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MlynarHead>(),
				ModContent.ItemType<MlynarBody>(),
				ModContent.ItemType<MlynarLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MlynarSetActive, "Mods.ArknightsMod.ArmorSets.Mlynar.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!MlynarHelmetActive || !item.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			bool boosted = OperatorTagHelper.CountHostileEnemies() >= 3 || OperatorSetBossHelper.AnyBossActive();
			damage *= boosted ? 1.15f : 1.1f;
		}

		public override void PostUpdate() {
			if (MlynarHelmetActive) {
				bool boosted = OperatorTagHelper.CountHostileEnemies() >= 3 || OperatorSetBossHelper.AnyBossActive();
				if (boosted)
					extraDefenseBonus += 0.1f;
			}

			if (MlynarSetActive)
				Player.aggro += 750;
		}

		public static bool TryGetActiveMlynarSetPlayer(out Player player, out MlynarSetPlayer modPlayer) {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player p = Main.player[i];
				if (!p.active || p.dead)
					continue;

				MlynarSetPlayer mp = p.GetModPlayer<MlynarSetPlayer>();
				if (mp.MlynarSetActive) {
					player = p;
					modPlayer = mp;
					return true;
				}
			}

			player = null;
			modPlayer = null;
			return false;
		}
	}

	internal class MlynarKazimierzReflectPlayer : ModPlayer
	{
		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!OperatorTagHelper.PlayerHasFaction(Player, OperatorFaction.Kazimierz))
				return;

			if (!MlynarSetPlayer.TryGetActiveMlynarSetPlayer(out Player mlynar, out _))
				return;

			modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
				if (!info.DamageSource.TryGetCausingEntity(out var entity) || entity is not NPC npc)
					return;

				int baseAttack = mlynar.HeldItem?.damage ?? 10;
				int damage = (int)(mlynar.GetTotalDamage(DamageClass.Melee).ApplyTo(baseAttack) * 0.15f);
				if (damage <= 0)
					return;

				npc.SimpleStrikeNPC(damage, Player.Center.X > npc.Center.X ? 1 : -1, false, 0, DamageClass.Generic);
			};
		}
	}
}
