using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Scene
{
	// 稀音的摄像机：右键部署摄影车的再部署冷却（5 秒）。
	public class SceneCameraPlayer : ModPlayer
	{
		public const int RedeployCooldownMax = 300; // 5 秒 @60fps

		public int RedeployCooldown;
		public bool CanRedeploy => RedeployCooldown <= 0;

		public void StartRedeployCooldown() => RedeployCooldown = RedeployCooldownMax;

		public override void PostUpdate() {
			if (RedeployCooldown > 0)
				RedeployCooldown--;
		}
	}
}
