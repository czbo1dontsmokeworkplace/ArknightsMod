using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Midnight
{
	internal class MidnightSetPlayer : ArknightsArmorPlayer
	{
		public bool MidnightHelmetActive;
		public bool MidnightSetActive;

		public override void ResetEffects() {
			MidnightHelmetActive = false;
			MidnightSetActive = false;
		}

		public override void PostUpdateEquips() {
			MidnightHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MidnightHead>());
			MidnightSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MidnightHead>(),
				ModContent.ItemType<MidnightBody>(),
				ModContent.ItemType<MidnightLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MidnightSetActive, "Mods.ArknightsMod.ArmorSets.Midnight.SetBonus");

			if (MidnightHelmetActive)
				Player.statDefense -= 5;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (MidnightHelmetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				damage *= 1.15f;
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (MidnightSetActive && item.DamageType.CountsAsClass(DamageClass.Melee) && Main.rand.NextFloat() < 0.1f)
				modifiers.SourceDamage *= 1.5f;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (MidnightSetActive && proj.DamageType.CountsAsClass(DamageClass.Melee) && Main.rand.NextFloat() < 0.1f)
				modifiers.SourceDamage *= 1.5f;
		}
	}
}
