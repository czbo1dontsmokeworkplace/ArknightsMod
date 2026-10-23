using System;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Rope
{
	// 暗索（Rope）的套装效果玩家。主题：钩索师的灵动身法——
	// 头盔：被击中时有 15% 概率闪避（类似原版神圣套装）。
	// 套装：5 秒内既未触发头盔闪避也未受到伤害时，蓄能完毕，下次受击必定闪避（一次性）。
	internal class RopeSetPlayer : ArknightsArmorPlayer, global::ArknightsMod.Players.IProbabilityDodgeSource, global::ArknightsMod.Players.IGuaranteedAvoidanceSource
	{
		public bool RopeHelmetActive;
		public bool RopeSetActive;

		private const float DodgeChanceValue = 0.15f; // 头盔的闪避概率
		private const int DodgeWindow = 300;     // 套装蓄能判定窗口（5 秒 = 300 tick）
		private const int DodgeImmuneTime = 60;  // 闪避成功后的无敌帧（1 秒，仿原版神圣套闪避）

		/// <summary>最近一次"触发闪避或实际受伤"的游戏 tick。</summary>
		private ulong lastDodgeOrHurtTick;

		public override void ResetEffects() {
			RopeHelmetActive = false;
			RopeSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 RopeHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			RopeHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<RopeHead>();
			RopeSetActive = RopeHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<RopeBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<RopeLegs>();

		}

        public string DodgeName => "暗索头盔";
        public double DodgeChance => RopeHelmetActive ? DodgeChanceValue : 0;
        private bool GuaranteedReady => RopeSetActive
            && Main.GameUpdateCount - lastDodgeOrHurtTick >= (ulong)DodgeWindow;
        public global::ArknightsMod.Players.GuaranteedAvoidanceState? GuaranteedAvoidance => !RopeSetActive ? null
            : new("暗索套装·蓄能", GuaranteedReady, GuaranteedReady ? "下次战斗伤害必定规避"
                : $"蓄能剩余 {Math.Max(0, DodgeWindow - (long)(Main.GameUpdateCount - lastDodgeOrHurtTick)) / 60d:0.0} 秒");
        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            if (!GuaranteedReady || (info.DamageSource.SourceNPCIndex < 0
                && info.DamageSource.SourceProjectileLocalIndex < 0)) return false;
            OnProbabilityDodge(info);
            return true;
        }
        public void OnProbabilityDodge(Player.HurtInfo info)
        {
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, DodgeImmuneTime);
            lastDodgeOrHurtTick = Main.GameUpdateCount;
            if (Main.dedServ) return;
            for (int i = 0; i < 12; i++) {
                Dust d = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame,
                    Main.rand.NextVector2Circular(6f, 6f), 0, default, 1.4f);
                d.noGravity = true;
            }
        }

		// 实际受伤（被闪避 Cancel 的伤害不会走到这里）→ 同样作为事件刷新计时
		public override void OnHurt(Player.HurtInfo info) {
			if (RopeHelmetActive)
				lastDodgeOrHurtTick = Main.GameUpdateCount;
		}
	}
}
