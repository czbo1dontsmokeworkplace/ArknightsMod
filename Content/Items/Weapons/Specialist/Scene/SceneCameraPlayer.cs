using ArknightsMod.Content.Projectiles.Specialist.Scene;
using ArknightsMod.Players;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Scene
{
	// 稀音的摄像机：右键部署冷却（5 秒）+ 两个技能的激活状态/时长管理。
	// 技能用「Down(S)+右键」释放当前在技能栏选中的技能；技力充能/消耗复用 WeaponPlayer。
	public class SceneCameraPlayer : ModPlayer
	{
		public const int RedeployCooldownMax = 300; // 5 秒 @60fps
		public const int Skill2DurationTicks = 20 * 60;
		public const int Skill2StunTicks = 5 * 60;

		public int RedeployCooldown;
		public bool CanRedeploy => RedeployCooldown <= 0;

		// 技能状态（latch 在玩家身上，效果生效仍以「手持摄像机」为门槛）
		public bool Skill1Active;
		public int Skill2Timer;

		private static readonly SoundStyle SkillActiveSound = new("ArknightsMod/Sounds/SkillActive1") {
			Volume = 0.4f,
			MaxInstances = 4,
		};

		public void StartRedeployCooldown() => RedeployCooldown = RedeployCooldownMax;

		public override void PostUpdate() {
			if (RedeployCooldown > 0)
				RedeployCooldown--;

			// 死亡时清空技能状态
			if (Player.dead) {
				Skill1Active = false;
				Skill2Timer = 0;
				return;
			}

			bool holding = Player.whoAmI == Main.myPlayer && Player.HeldItem.ModItem is SceneCamera;

			// 技能二限时：仅手持时推进；归 0 当帧令全部摄影车眩晕。
			if (Skill2Timer > 0 && holding) {
				Skill2Timer--;
				if (Skill2Timer == 0)
					CameraTruck.StunAllForPlayer(Player, Skill2StunTicks);
			}

			// 输入：手持 + Down(S) 按住 + 右键刚按下 → 释放选中技能
			if (holding && !BlocksInput() && Player.controlDown && PlayerInput.Triggers.JustPressed.MouseRight)
				TryActivateSelectedSkill();
		}

		private bool BlocksInput() =>
			Player.mouseInterface || Main.playerInventory || Player.talkNPC >= 0 || Player.chest != -1;

		private void TryActivateSelectedSkill() {
			var wp = Player.GetModPlayer<WeaponPlayer>();
			if (wp.StockCount <= 0)
				return;

			switch (wp.Skill) {
				case 0:
					if (Skill1Active)
						return;
					Skill1Active = true;
					wp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSound, Player.Center);
					break;
				case 1:
					if (Skill2Timer > 0)
						return;
					Skill2Timer = Skill2DurationTicks;
					wp.DelStockCount();
					// 立即额外召唤一辆「免位」摄影车（不占仆从位，总数仍受 5 上限约束）
					if (CameraTruck.CountActiveForPlayer(Player) < CameraTruck.MaxTrucks)
						SceneCamera.SummonTruck(Player, Player.GetSource_Misc("SceneCameraSkill2"), free: true);
					SoundEngine.PlaySound(SkillActiveSound, Player.Center);
					break;
			}
		}
	}
}
