using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Kroos
{
	internal class KroosSetPlayer : ArknightsArmorPlayer
	{
		public bool KroosHelmetActive;
		public bool KroosSetActive;

		public override void ResetEffects() {
			KroosHelmetActive = false;
			KroosSetActive = false;
		}

		public override void PostUpdateEquips() {
			KroosHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<KroosHead>());
			KroosSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<KroosHead>(),
				ModContent.ItemType<KroosBody>(),
				ModContent.ItemType<KroosLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, KroosSetActive, "Mods.ArknightsMod.ArmorSets.Kroos.SetBonus");
		}

		public override bool CanConsumeAmmo(Item weapon, Item ammo) {
			if (KroosHelmetActive
				&& weapon.DamageType.CountsAsClass(DamageClass.Ranged)
				&& Main.rand.NextFloat() < 0.4f) {
				return false;
			}

			return base.CanConsumeAmmo(weapon, ammo);
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (KroosSetActive && item.DamageType.CountsAsClass(DamageClass.Ranged) && Main.rand.NextFloat() < 0.1f)
				modifiers.SourceDamage *= 1.5f;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (KroosSetActive && proj.DamageType.CountsAsClass(DamageClass.Ranged) && Main.rand.NextFloat() < 0.1f)
				modifiers.SourceDamage *= 1.5f;
		}
	}
}
