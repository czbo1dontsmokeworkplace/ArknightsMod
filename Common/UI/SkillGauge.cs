using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Common.UI
{
	internal class SkillGauge : UIState
	{
		private UIText text;

		private UIElement area;
		private UIImage barFrame;
		private Color gradientA;
		private Color gradientB;
		private Color skillColor;
		private const int ChargePulseActiveTicks = 36;
		private const int ChargePulsePauseTicks = 12;
		private const int ChargePulseCycleTicks = ChargePulseActiveTicks + ChargePulsePauseTicks;
		private const float ChargePulseExpandMax = 1.75f;
		private static readonly Color ChargePulseColor = new(255, 220, 0);
		private readonly Texture2D[] stockIcon = [
			ModContent.Request<Texture2D>("ArknightsMod/Common/UI/SkillStock1", AssetRequestMode.ImmediateLoad).Value,
			ModContent.Request<Texture2D>("ArknightsMod/Common/UI/SkillStock2", AssetRequestMode.ImmediateLoad).Value,
			ModContent.Request<Texture2D>("ArknightsMod/Common/UI/SkillStock3", AssetRequestMode.ImmediateLoad).Value,
		];
		private readonly Texture2D skillCanUse =
			ModContent.Request<Texture2D>("ArknightsMod/Common/UI/Skill", AssetRequestMode.ImmediateLoad).Value;

		public override void OnInitialize() {
			area = new UIElement();
			area.Top.Set(70, 0f);
			area.Width.Set(139, 0f);
			area.Height.Set(20, 0f);
			area.VAlign = 0.5f;
			area.HAlign = 0.5f;

			barFrame = new UIImage(ModContent.Request<Texture2D>("ArknightsMod/Common/UI/SkillGaugeFrame"));
			barFrame.Left.Set(10, 0f);
			barFrame.Top.Set(0, 0f);
			barFrame.Width.Set(118, 0f);
			barFrame.Height.Set(12, 0f);

			text = new UIText("0/0", 0.8f);
			text.Width.Set(118, 0f);
			text.Height.Set(12, 0f);
			text.Top.Set(100, 0f);
			text.Left.Set(0, 0f);

			gradientA = new Color(181, 191, 100);
			gradientB = new Color(127, 114, 96);
			skillColor = new Color(255, 197, 0);

			area.Append(barFrame);
			Append(area);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			if (Main.LocalPlayer.HeldItem.ModItem is not UpgradeWeaponBase)
				return;
			base.Draw(spriteBatch);
		}

		protected override void DrawSelf(SpriteBatch sb) {
			base.DrawSelf(sb);

			var mp = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			SkillData skill = mp.CurrentSkill;

			if (skill == null)
				return;

			SkillLevelData data = skill.CurrentLevelData;

			float activeTime = data.ActiveTime * 60;
			int maxStock = data.MaxStack;
			int stock = mp.StockCount;

			if (activeTime <= 0)
				activeTime = 1f;
			if (maxStock <= 0)
				maxStock = 1;

			float quotient1 = mp.SkillChargeMax > 0 ? (float)mp.SkillCharge / mp.SkillChargeMax : 0f;
			quotient1 = Utils.Clamp(quotient1, 0f, 1f);
			float quotient2 = mp.SkillTimer / activeTime;
			quotient2 = Utils.Clamp(quotient2, 0f, 1f);

			if (barFrame == null)
				return;

			Rectangle hitbox = barFrame.GetInnerDimensions().ToRectangle();
			hitbox.X += 2;
			hitbox.Width -= 2;
			hitbox.Y += 2;
			hitbox.Height -= 2;

			var aboveHead = new Rectangle(Main.screenWidth / 2 - 12, Main.screenHeight / 2 - 65, 22, 22);

			int left = hitbox.Left;
			int right = hitbox.Right;
			int steps1 = (int)((right - left) * quotient1);
			int steps2 = (int)((right - left) * quotient2);

			sb.Draw(pixel, new Rectangle(left, hitbox.Y, 116, hitbox.Height), gradientB);
			for (int i = 0; i < steps1; i += 1) {
				sb.Draw(pixel, new Rectangle(left + i, hitbox.Y, 1, hitbox.Height), gradientA);
			}
			if (mp.StockCount == maxStock) {
				sb.Draw(pixel, new Rectangle(left, hitbox.Y, 116, hitbox.Height), gradientA);
			}

			if (mp.SkillActive) {
				sb.Draw(pixel, new Rectangle(left, hitbox.Y, 116, hitbox.Height), skillColor);
				for (int i = 0; i < steps2; i += 1) {
					sb.Draw(pixel, new Rectangle(right - i, hitbox.Y, 1, hitbox.Height), gradientB);
				}
			}

			if (maxStock > 1 && stock > 0) {
				if (stockIcon != null && stockIcon.Length >= stock && stockIcon[stock - 1] != null) {
					if (stock == maxStock && !skill.SuppressReadyPulse)
						DrawChargeReadyPulse(sb, aboveHead);
					sb.Draw(stockIcon[stock - 1], aboveHead, Color.White);
				}
			}
			else if (maxStock == 1 && !skill.AutoTrigger) {
				if (stock == 1 && skillCanUse != null && !mp.SkillActive) {
					DrawChargeReadyPulse(sb, aboveHead);
					sb.Draw(skillCanUse, aboveHead, Color.White);
				}
			}
		}

		private static void DrawChargeReadyPulse(SpriteBatch sb, Rectangle iconRect) {
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			int tickInCycle = (int)(Main.GameUpdateCount % ChargePulseCycleTicks);
			if (tickInCycle >= ChargePulseActiveTicks)
				return;

			float progress = tickInCycle / (float)ChargePulseActiveTicks;
			float alpha = 1f - progress;
			if (alpha <= 0f)
				return;

			float expand = MathHelper.Lerp(0.55f, ChargePulseExpandMax, progress);
			float side = iconRect.Width * expand;
			Vector2 center = iconRect.Center.ToVector2();
			Color color = ChargePulseColor * alpha;

			sb.Draw(
				pixel,
				center,
				new Rectangle(0, 0, 1, 1),
				color,
				MathHelper.PiOver4,
				new Vector2(0.5f, 0.5f),
				new Vector2(side, side),
				SpriteEffects.None,
				0f);
		}
	}

	internal class SkillGaugeSystem : ModSystem
	{
		private UserInterface SkillGaugeUserInterface;

		internal SkillGauge SkillGauge;

		public override void Load() {
			if (!Main.dedServ) {
				SkillGauge = new();
				SkillGaugeUserInterface = new();
				SkillGaugeUserInterface.SetState(SkillGauge);
			}
		}

		public override void UpdateUI(GameTime gameTime) {
			SkillGaugeUserInterface?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
			if (resourceBarIndex != -1) {
				layers.Insert(resourceBarIndex, new LegacyGameInterfaceLayer(
					"ArknightsMod: Skill Gauge",
					delegate {
						SkillGaugeUserInterface.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}
	}
}
