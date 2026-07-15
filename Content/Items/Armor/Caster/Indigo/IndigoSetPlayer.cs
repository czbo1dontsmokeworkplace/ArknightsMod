using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Indigo
{
	internal class IndigoSetPlayer : ArknightsArmorPlayer
	{
		public bool IndigoHelmetActive;
		public bool IndigoSetActive;

		public override void ResetEffects() {
			IndigoHelmetActive = false;
			IndigoSetActive = false;
		}

		public override void PostUpdateEquips() {
			IndigoHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<IndigoHead>());
			IndigoSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<IndigoHead>(),
				ModContent.ItemType<IndigoBody>(),
				ModContent.ItemType<IndigoLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, IndigoSetActive, "Mods.ArknightsMod.ArmorSets.Indigo.SetBonus");
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (IndigoHelmetActive && item.DamageType.CountsAsClass(DamageClass.Magic) && HasBindOrStun(target))
				modifiers.SourceDamage *= 1.3f;

			if (IndigoSetActive && item.DamageType.CountsAsClass(DamageClass.Magic) && Main.rand.NextFloat() < 0.18f)
				TryBind(target);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (IndigoHelmetActive && proj.DamageType.CountsAsClass(DamageClass.Magic) && HasBindOrStun(target))
				modifiers.SourceDamage *= 1.3f;

			if (IndigoSetActive && proj.DamageType.CountsAsClass(DamageClass.Magic) && Main.rand.NextFloat() < 0.18f)
				TryBind(target);
		}

		private static bool HasBindOrStun(NPC target) {
			return target.HasBuff(ModContent.BuffType<IndigoBindDebuff>()) || OperatorStunNPC.HasStun(target);
		}

		private static void TryBind(NPC target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage)
				return;

			target.AddBuff(ModContent.BuffType<IndigoBindDebuff>(), 4 * 60);
		}
	}
}
