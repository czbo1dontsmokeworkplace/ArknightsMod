using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	/// <summary>
	/// W 战场景效果：把 <see cref="WBattleVisuals.SkyWanted"/> 接到原版 SceneEffect 生命周期上——<br/>
	/// <c>ManageSpecialBiomeVisuals</c> 负责天空的 Activate/Deactivate 与离开世界时的清理，比手动调 SkyManager 稳<br/>
	/// 不接管音乐（Music = -1），boss 曲仍由 NPC 自己的 Music 决定
	/// </summary>
	public class WBattleSceneEffect : ModSceneEffect
	{
		public override int Music => -1;
		public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
		public override bool IsSceneEffectActive(Player player) => WBattleVisuals.SkyWanted;
		public override void SpecialVisuals(Player player, bool isActive) => player.ManageSpecialBiomeVisuals(WBattleVisuals.SkyKey, isActive);
	}

	/// <summary>
	/// W 战的场景层总控（客户端表现 + 台词广播）：<br/>
	/// - <see cref="Gloom"/>/<see cref="RedPulse"/>：由场上 W 的阶段与血量推导，驱动天空压暗与世界光压暗<br/>
	/// - <see cref="Flash"/>：一次性屏幕白闪（闪光弹、JACKPOT）<br/>
	/// - <see cref="NukeSky"/>/<see cref="SkyFlash"/>：核爆白炽与爆炸照亮云底，交给 <see cref="WBattleSky"/> 的着色器按爆心屏幕位置演出<br/>
	/// - <see cref="ShowNameplate"/>：登场名牌（HUD 顶部居中 2.5 秒）<br/>
	/// - <see cref="Say"/>：W 的台词（权威端调用，聊天栏广播；联机发 key 不发文本）
	/// </summary>
	public class WBattleVisuals : ModSystem
	{
		public const string SkyKey = "ArknightsMod:WBattleSky";
		public const float DesperateHpRatio = 0.25f; // 二阶段 ≤25%：天空全暗 + 红光 + 双管

		/// <summary>天空/世界光压暗强度 0~1</summary>
		public static float Gloom { get; private set; }
		/// <summary>低血量红光脉动强度 0~1</summary>
		public static float RedPulse { get; private set; }

		private static float flash;
		private static float flashDecay = 0.055f;
		private static Color flashColor = Color.White;
		private static int nameplateTimer;
		private const int NameplateDuration = 150;

		/// <summary>核爆白炽 0~1：天空整层泛白、世界光过曝，之后慢慢回落成红</summary>
		public static float NukeGlow { get; private set; }
		/// <summary>核爆在屏幕上的横向位置 0~1（天空着色器的白炽/光穹以此为中心）</summary>
		public static float NukeScreenX { get; private set; } = 0.5f;

		private static bool skyFlashPending;
		private static float skyFlashX = 0.5f;
		private static float skyFlashStrength;

		/// <summary>屏幕滤镜的基色（硝烟灰）；低血量与核爆时每帧改写为红/白</summary>
		public static readonly Vector3 FilterBaseColor = new(0.16f, 0.12f, 0.13f);

		public static readonly Color QuipColor = new(225, 95, 95);

		/// <summary>一次性屏幕闪光；strength 0~1 为峰值亮度，decay 为每帧衰减量（默认 0.055 ≈ 18 帧散尽）</summary>
		public static void Flash(float strength, Color? color = null, float decay = 0.055f) {
			if (Main.dedServ)
				return;
			float s = MathHelper.Clamp(strength, 0f, 1f);
			if (s >= flash) {
				flash = s;
				flashDecay = decay;
				flashColor = color ?? Color.White;
			}
		}

		/// <summary>核爆：白炽拉满，天空与世界光一起过曝；worldCenter 为爆心，决定天空白炽/光穹的横向位置</summary>
		public static void NukeSky(Vector2 worldCenter) {
			if (Main.dedServ)
				return;
			NukeGlow = 1f;
			NukeScreenX = ToScreenX(worldCenter);
		}

		/// <summary>
		/// 爆炸照亮云底（仅客户端表现）：天空着色器在 worldPos 对应的屏幕横向位置上给云层一记暖光，随后由 WBattleSky 逐帧衰减<br/>
		/// 同一帧多次调用只保留最强的一次
		/// </summary>
		public static void SkyFlash(Vector2 worldPos, float strength) {
			if (Main.dedServ)
				return;
			strength = MathHelper.Clamp(strength, 0f, 1f);
			if (skyFlashPending && strength < skyFlashStrength)
				return;
			skyFlashPending = true;
			skyFlashStrength = strength;
			skyFlashX = ToScreenX(worldPos);
		}

		/// <summary>由 WBattleSky.Update 每帧消费一次待处理的照云请求</summary>
		public static bool ConsumeSkyFlash(out float screenX, out float strength) {
			screenX = skyFlashX;
			strength = skyFlashStrength;
			if (!skyFlashPending)
				return false;
			skyFlashPending = false;
			return true;
		}

		/// <summary>世界 x → 屏幕横向比例，夹在 0.1~0.9 之间，屏幕外的爆炸仍能在边缘照出一点光</summary>
		private static float ToScreenX(Vector2 worldPos) {
			return MathHelper.Clamp((worldPos.X - Main.screenPosition.X) / Math.Max(1, Main.screenWidth), 0.1f, 0.9f);
		}

		public static void ShowNameplate() {
			if (!Main.dedServ)
				nameplateTimer = NameplateDuration;
		}

		/// <summary>
		/// W 开口（仅权威端调用）key 为 Mods.ArknightsMod.WBossText.Quip 下的子键；<br/>
		/// 单机直接写聊天栏，服务端广播本地化 key 由各客户端自行取文本
		/// </summary>
		public static void Say(string key) {
			string fullKey = $"Mods.ArknightsMod.WBossText.Quip.{key}";
			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Language.GetTextValue(fullKey), QuipColor);
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey(fullKey), QuipColor);
		}

		/// <summary>天空是否该在场：由 WBattleSceneEffect 每帧读取，走 ManageSpecialBiomeVisuals 的标准生命周期</summary>
		public static bool SkyWanted => Gloom > 0.01f || NukeGlow > 0.01f;

		public override void PostUpdateEverything() {
			if (Main.dedServ)
				return;

			// 一阶段就有一层薄硝烟（战场从开场就在冒烟），二阶段压重，≤25% 全暗 + 红光
			float targetGloom = 0f, targetRed = 0f;
			int idx = NPC.FindFirstNPC(ModContent.NPCType<WBoss>());
			if (idx >= 0 && Main.npc[idx].ModNPC is WBoss w) {
				NPC npc = Main.npc[idx];
				bool curtain = w.Machine?.CurrentState is WDeathState or WDespawnState; // 谢幕时天空归位
				if (!curtain) {
					targetGloom = w.Phase == 2 ? 0.8f : 0.3f;
					if (w.Phase == 2 && npc.life <= npc.lifeMax * DesperateHpRatio) {
						targetGloom = 1f;
						targetRed = 1f;
					}
				}
			}
			Gloom = Approach(Gloom, targetGloom, 0.012f);
			RedPulse = Approach(RedPulse, targetRed, 0.02f);
			UpdateSceneFilter();

			if (flash > 0f)
				flash = Math.Max(0f, flash - flashDecay);
			if (NukeGlow > 0f)
				NukeGlow = Math.Max(0f, NukeGlow - (NukeGlow > 0.5f ? 0.012f : 0.006f));
			if (nameplateTimer > 0)
				nameplateTimer--;
		}

		/// <summary>
		/// 屏幕滤镜跟着战况走：硝烟越浓越暗，≤25% 转血红并随脉动加深，核爆瞬间整屏过曝到白<br/>
		/// 激活/关闭由 <see cref="WBattleSceneEffect"/> 的 ManageSpecialBiomeVisuals 负责，这里只改参数
		/// </summary>
		private static void UpdateSceneFilter() {
			Filter filter = Filters.Scene[SkyKey];
			if (filter == null)
				return;
			Vector3 color = FilterBaseColor;
			float opacity = 0.42f * Gloom;
			if (RedPulse > 0.01f) {
				float p = (MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f) + 1f) * 0.5f * RedPulse;
				color = Vector3.Lerp(color, new Vector3(0.42f, 0.06f, 0.06f), 0.6f + 0.4f * p);
				opacity += 0.12f * p;
			}
			if (NukeGlow > 0.01f) {
				float white = MathHelper.Clamp((NukeGlow - 0.5f) * 2f, 0f, 1f);
				color = Vector3.Lerp(color, Vector3.One, white);
				opacity = Math.Max(opacity, 0.55f * NukeGlow);
			}
			filter.GetShader().UseColor(color).UseOpacity(MathHelper.Clamp(opacity, 0f, 1f));
		}

		private static float Approach(float value, float target, float step) {
			if (Math.Abs(target - value) <= step)
				return target;
			return value + Math.Sign(target - value) * step;
		}

		public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor) {
			if (Gloom > 0.01f) {
				backgroundColor = Color.Lerp(backgroundColor, new Color(70, 55, 60), 0.55f * Gloom);
				tileColor = Color.Lerp(tileColor, new Color(150, 122, 126), 0.35f * Gloom);
				if (RedPulse > 0.01f) {
					float p = (MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f) + 1f) * 0.5f * RedPulse;
					backgroundColor = Color.Lerp(backgroundColor, new Color(120, 30, 30), 0.35f * p);
					tileColor = Color.Lerp(tileColor, new Color(200, 90, 90), 0.15f * p);
				}
			}
			if (NukeGlow > 0.01f) {
				// 白炽：前半段整个世界过曝成白，后半段落成血红再回来
				float white = MathHelper.Clamp((NukeGlow - 0.5f) * 2f, 0f, 1f);
				float red = MathHelper.Clamp(NukeGlow * 2f, 0f, 1f) * (1f - white);
				tileColor = Color.Lerp(tileColor, Color.White, white * 0.95f);
				backgroundColor = Color.Lerp(backgroundColor, Color.White, white);
				tileColor = Color.Lerp(tileColor, new Color(255, 120, 90), red * 0.45f);
				backgroundColor = Color.Lerp(backgroundColor, new Color(150, 40, 30), red * 0.6f);
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			// 白闪盖在世界之上、界面之下
			int flashIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");
			if (flashIndex != -1) {
				layers.Insert(flashIndex, new LegacyGameInterfaceLayer("ArknightsMod: W Screen Flash", () => {
					DrawFlash(Main.spriteBatch);
					return true;
				}, InterfaceScaleType.UI));
			}
			// 名牌盖在大部分界面之上
			int plateIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
			if (plateIndex != -1) {
				layers.Insert(plateIndex, new LegacyGameInterfaceLayer("ArknightsMod: W Nameplate", () => {
					DrawNameplate(Main.spriteBatch);
					return true;
				}, InterfaceScaleType.UI));
			}
		}

		private static void DrawFlash(SpriteBatch sb) {
			if (flash <= 0.01f)
				return;
			// 亮度按平方衰减：起爆瞬间刺眼，随后很快让开画面
			float a = flash * flash;
			sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), flashColor * a);
		}

		private static void DrawNameplate(SpriteBatch sb) {
			if (nameplateTimer <= 0)
				return;
			int elapsed = NameplateDuration - nameplateTimer;
			float alpha = MathHelper.Clamp(Math.Min(elapsed / 18f, nameplateTimer / 30f), 0f, 1f);
			// 入场时从下方略微升起
			float rise = (1f - MathHelper.Clamp(elapsed / 18f, 0f, 1f)) * 14f;

			string name = Lang.GetNPCNameValue(ModContent.NPCType<WBoss>());
			string title = Language.GetTextValue("Mods.ArknightsMod.WBossText.Title");
			DynamicSpriteFont big = FontAssets.DeathText.Value;
			DynamicSpriteFont small = FontAssets.MouseText.Value;
			Vector2 center = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.22f + rise);

			Vector2 nameSize = big.MeasureString(name) * 1.1f;
			Vector2 titleSize = small.MeasureString(title) * 0.95f;
			Color nameColor = new Color(235, 225, 225) * alpha;
			Color shadow = Color.Black * (0.85f * alpha);
			ChatManager.DrawColorCodedStringWithShadow(sb, big, name, center - nameSize * 0.5f, nameColor, shadow, 0f, Vector2.Zero, new Vector2(1.1f), -1f, 2.5f);

			// 名字下方一道向两侧展开的红线
			float lineHalf = MathHelper.Clamp(elapsed / 24f, 0f, 1f) * (nameSize.X * 0.5f + 40f);
			float lineY = center.Y + nameSize.Y * 0.5f + 6f;
			sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)(center.X - lineHalf), (int)lineY, (int)(lineHalf * 2f), 2), QuipColor * alpha);

			ChatManager.DrawColorCodedStringWithShadow(sb, small, title, new Vector2(center.X - titleSize.X * 0.5f, lineY + 8f),
				new Color(220, 150, 150) * alpha, shadow, 0f, Vector2.Zero, new Vector2(0.95f), -1f, 2f);
		}
	}
}
