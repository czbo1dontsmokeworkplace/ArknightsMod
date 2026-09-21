using ArknightsMod.Content.NPCs.Friendly;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ArknightsMod.Systems
{
	/// <summary>
	/// 给坎诺特对话框里变色的两个按钮加一圈脉动的光晕：
	/// 左边「听坎诺特的」发绿光，右边「“请”坎诺特“降价”」发红光。
	///
	/// 文字本身的颜色不用在这里管——按钮文字里已经带了聊天颜色标签（见 Cannot.SetChatButtons），
	/// 原版画对话按钮用的是 ChatManager，会自己解析。这里只负责在原版画按钮<b>之前</b>
	/// 先在同样的位置垫一层光晕，所以光晕会被文字盖在下面。
	///
	/// 原版 DrawNPCChatButtons 里按钮的位置是现算的局部变量，没法直接读，
	/// 所以这里照抄了它的排版公式（y = 130 + 行数×30；x 起点 = 180 + (屏幕宽-800)/2；
	/// 三个按钮从左到右依次是 自定义按钮1 / 「关闭」 / 自定义按钮2，间隔 30 像素）。
	/// 如果以后 tModLoader/原版改了对话框排版，这里的光晕位置会跟着偏，需要对着
	/// Terraria.Main.DrawNPCChatButtons 重新核对。
	/// </summary>
	public class CannotChatButtonGlowSystem : ModSystem
	{
		private const float BaseScale = 0.9f;
		private const float HoverScale = 1.2f;   // 原版鼠标悬停时按钮放大到 1.2 倍
		private const float MaxButtonWidth = 260f;

		public override void Load() {
			On_Main.DrawNPCChatButtons += DrawButtonsWithGlow;
		}

		public override void Unload() {
			On_Main.DrawNPCChatButtons -= DrawButtonsWithGlow;
		}

		private static void DrawButtonsWithGlow(On_Main.orig_DrawNPCChatButtons orig, int superColor, Color chatColor, int numLines, string focusText, string focusText3) {
			if (TryGetUltimatumCannot(out _))
				DrawGlows(numLines, focusText, focusText3);

			orig(superColor, chatColor, numLines, focusText, focusText3);
		}

		private static bool TryGetUltimatumCannot(out Cannot cannot) {
			cannot = null;
			int talk = Main.LocalPlayer.talkNPC;
			if (talk < 0 || talk >= Main.maxNPCs)
				return false;

			NPC npc = Main.npc[talk];
			if (!npc.active || npc.ModNPC is not Cannot found)
				return false;

			cannot = found;
			return cannot.Phase == CannotPhase.Friendly && cannot.TouchCount >= Cannot.TouchThreshold;
		}

		private static void DrawGlows(int numLines, string leftText, string rightText) {
			if (string.IsNullOrEmpty(leftText) || string.IsNullOrEmpty(rightText))
				return;

			var font = FontAssets.MouseText.Value;
			float y = 130 + numLines * 30;
			float x = 180 + (Main.screenWidth - 800) / 2;
			Vector2 baseScale = new(BaseScale);

			// 左按钮
			Vector2 leftSize = ChatManager.GetStringSize(font, leftText, baseScale, -1f);
			float leftShrink = leftSize.X > MaxButtonWidth ? MaxButtonWidth / leftSize.X : 1f;
			DrawGlow(font, leftText, new Vector2(x, y), leftSize, leftShrink, Main.npcChatFocus2, Cannot.ListenColor);

			// 中间是原版的「关闭」按钮，只用来量宽度、推算右按钮的起点
			float middleX = x + leftSize.X * leftShrink + 30f;
			string closeText = Lang.inter[52].Value;
			Vector2 closeSize = ChatManager.GetStringSize(font, closeText, baseScale, -1f);
			float closeShrink = closeSize.X > MaxButtonWidth ? MaxButtonWidth / closeSize.X : 1f;

			// 右按钮
			float rightX = middleX + closeSize.X * closeShrink + 30f;
			Vector2 rightSize = ChatManager.GetStringSize(font, rightText, baseScale, -1f);
			float rightShrink = rightSize.X > MaxButtonWidth ? MaxButtonWidth / rightSize.X : 1f;
			DrawGlow(font, rightText, new Vector2(rightX, y), rightSize, rightShrink, Main.npcChatFocus3, Cannot.HaggleColor);
		}

		private static void DrawGlow(ReLogic.Graphics.DynamicSpriteFont font, string text, Vector2 topLeft, Vector2 textSize, float shrinkX, bool hovered, Color glowColor) {
			float scale = BaseScale * (hovered ? HoverScale : 1f);
			Vector2 drawScale = new(scale * shrinkX, scale);
			Vector2 center = topLeft + textSize * new Vector2(shrinkX, 1f) * 0.5f;
			Vector2 origin = textSize * 0.5f;

			// 慢速呼吸：0.55 ~ 1.0
			float pulse = 0.775f + 0.225f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f);

			// 用同一段文字（忽略里面的颜色标签、统一染成光晕色）在四周多描几圈、每圈透明度很低，
			// 叠起来就是一圈柔和的光晕。原版对话框用的是普通 AlphaBlend，这里不切换混合模式，
			// 靠低透明度多层叠加来模拟发光，避免中途改动 SpriteBatch 状态。
			DrawRing(font, text, center, drawScale, origin, glowColor * (0.10f * pulse), 6f, 16);
			DrawRing(font, text, center, drawScale, origin, glowColor * (0.16f * pulse), 3.5f, 12);
			DrawRing(font, text, center, drawScale, origin, glowColor * (0.22f * pulse), 1.8f, 8);
		}

		private static void DrawRing(ReLogic.Graphics.DynamicSpriteFont font, string text, Vector2 center, Vector2 scale, Vector2 origin, Color color, float radius, int count) {
			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count;
				Vector2 offset = angle.ToRotationVector2() * radius;
				ChatManager.DrawColorCodedString(Main.spriteBatch, font, text, center + offset, color, 0f, origin, scale, -1f, ignoreColors: true);
			}
		}
	}
}
