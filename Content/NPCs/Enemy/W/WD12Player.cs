using ArknightsMod.Content.Projectiles.Bosses.W;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	/// <summary>
	/// D12 甩脱交互：被黏附时快速左键连点 15 次甩脱。<br/>
	/// 黏附识别靠扫弹幕（WD12.ai[1] == 自己的 whoAmI），不需要额外同步；<br/>
	/// 连点计数只在本地玩家进行，达成后单机直接甩脱、联机发 WD12Detach 包由服务端裁决。
	/// </summary>
	public class WD12Player : ModPlayer
	{
		public const int RequiredClicks = 15;

		public int ShakeClicks;
		public int AttachedProjIndex = -1;
		private bool wasAttached;

		public override void PreUpdate() {
			int found = -1;
			int type = ModContent.ProjectileType<WD12>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.type == type && (int)p.ai[1] == Player.whoAmI) {
					found = i;
					break;
				}
			}
			AttachedProjIndex = found;
			if (found < 0)
				ShakeClicks = 0;

			// 黏附瞬间的操作提示（只给被黏附的本地玩家看）
			if (Player.whoAmI == Main.myPlayer && !Main.dedServ) {
				if (found >= 0 && !wasAttached)
					CombatText.NewText(Player.Hitbox, Color.OrangeRed, Language.GetTextValue("Mods.ArknightsMod.WBossText.D12Prompt"), true);
			}
			wasAttached = found >= 0;
		}

		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (AttachedProjIndex < 0 || Player.dead)
				return;
			if (!PlayerInput.Triggers.JustPressed.MouseLeft)
				return;

			ShakeClicks++;
			if (ShakeClicks < RequiredClicks) {
				if (ShakeClicks % 5 == 0)
					CombatText.NewText(Player.Hitbox, Color.Orange, $"{ShakeClicks}/{RequiredClicks}");
				return;
			}

			ShakeClicks = 0;
			if (Main.netMode == NetmodeID.SinglePlayer) {
				WD12.Detach(Main.projectile[AttachedProjIndex]);
			}
			else {
				ModPacket packet = Mod.GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.WD12Detach);
				packet.Write(AttachedProjIndex);
				packet.Send();
			}
			// 甩脱反馈在本地播：服务端裁决的那一端没有喇叭
			SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.3f }, Player.Center);
			CombatText.NewText(Player.Hitbox, Color.LightGreen, Language.GetTextValue("Mods.ArknightsMod.WBossText.D12Shaken"), true);
		}
	}
}
