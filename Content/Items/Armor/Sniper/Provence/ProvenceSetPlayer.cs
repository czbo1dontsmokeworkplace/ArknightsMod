using ArknightsMod.Content.Items.Armor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Provence
{
	internal class ProvenceSetPlayer : ArknightsArmorPlayer
	{
		public bool ProvenceHelmetActive;
		public bool ProvenceSetActive;

		private const float CloseRange = 300f;

		public override void ResetEffects() {
			ProvenceHelmetActive = false;
			ProvenceSetActive = false;
		}

		public override void PostUpdateEquips() {
			ProvenceHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<ProvenceHead>());
			ProvenceSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<ProvenceHead>(),
				ModContent.ItemType<ProvenceBody>(),
				ModContent.ItemType<ProvenceLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, ProvenceSetActive, "Mods.ArknightsMod.ArmorSets.Provence.SetBonus");
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			TryCloseRangeCrit(item, target, ref modifiers);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			TryCloseRangeCrit(proj, target, ref modifiers);
		}

		private void TryCloseRangeCrit(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (!ProvenceHelmetActive || !item.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (Vector2.Distance(Player.Center, target.Center) > CloseRange)
				return;

			float chance = ProvenceSetActive ? 0.5f : 0.2f;
			if (Main.rand.NextFloat() < chance)
				modifiers.SourceDamage *= 1.8f;
		}

		private void TryCloseRangeCrit(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (!ProvenceHelmetActive || !proj.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (Vector2.Distance(Player.Center, target.Center) > CloseRange)
				return;

			float chance = ProvenceSetActive ? 0.5f : 0.2f;
			if (Main.rand.NextFloat() < chance)
				modifiers.SourceDamage *= 1.8f;
		}
	}
}
