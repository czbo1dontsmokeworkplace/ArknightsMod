using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Projectiles.Caster.Necrass;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Necrass
{
	internal sealed class NecrassSetPlayer : ModPlayer
	{
		public bool NecrassHelmetActive;
		public bool NecrassSetActive;
		private bool wasSetActive;

		public override void ResetEffects() {
			NecrassHelmetActive = false;
			NecrassSetActive = false;
		}

		public override void PostUpdateEquips() {
			NecrassHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<NecrassHead>();
			NecrassSetActive = NecrassHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<NecrassBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<NecrassLegs>();
		}

		public override void PostUpdate() {
			if (Player.dead)
				return;
			// 仅穿齐的瞬间召唤，不每帧补召：仆役被击杀后须靠头盔击杀效果重新召唤。
			if (NecrassSetActive && !wasSetActive)
				LamentingServant.SpawnFor(Player);
			wasSetActive = NecrassSetActive;
		}

		public override void UpdateDead() => wasSetActive = false;
		public override void OnRespawn() => wasSetActive = false;

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			if (damageDone > 0)
				TryRewardKill(target);
		}
		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			if (damageDone > 0)
				TryRewardKill(target);
		}

		internal void TryRewardKill(NPC target) {
			if (Player.whoAmI != Main.myPlayer || Player.dead || !NecrassHelmetActive
				|| target.life > 0 || target.friendly || target.lifeMax <= 5)
				return;
			NecrassKillCreditNPC credit = target.GetGlobalNPC<NecrassKillCreditNPC>();
			if (credit.Rewarded)
				return;
			credit.Rewarded = true;

			LamentingServant servant = LamentingServant.FindFor(Player);
			if (servant == null)
				LamentingServant.SpawnFor(Player);
			else
				servant.ReinforceAndHeal();
		}
	}

	internal sealed class NecrassKillCreditNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;
		public bool Rewarded;

		public override void OnKill(NPC npc) {
			// 单机也覆盖由持续伤害完成的击杀；与命中回调共用去重标记。
			if (Main.netMode == NetmodeID.SinglePlayer && npc.lastInteraction >= 0
				&& npc.lastInteraction < Main.maxPlayers) {
				Main.player[npc.lastInteraction].GetModPlayer<NecrassSetPlayer>().TryRewardKill(npc);
			}
		}
	}
}
