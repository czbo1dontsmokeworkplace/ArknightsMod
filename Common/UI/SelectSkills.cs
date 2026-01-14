using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Common.UI
{
	internal class SelectSkills : UIState
	{
		private static SelectSkills ins;

		// For this bar we'll be using a frame texture and then a gradient inside bar, as it's one of the more simpler approaches while still looking decent.
		// Once this is all set up make sure to go and do the required stuff for most UI's in the ModSystem class.
		private readonly static SoundStyle click = new("ArknightsMod/Sounds/UIButton");
		private SkillSlotUI s1, s2, s3;
		private SummonSkillSlot summon;
		private UIText skillLevel;
		internal SelectSkills() => ins = this;

		public override void OnInitialize() {
			UIImage area = new(ModContent.Request<Texture2D>("ArknightsMod/Common/UI/SkillBase", AssetRequestMode.ImmediateLoad));
			area.Left.Set(20, 0f);
			area.Top.Set(136, 0f);
			area.Width.Set(300, 0f);
			area.Height.Set(100, 0f);
			Append(area);

			skillLevel = new UIText("0", 1f);
			skillLevel.Left.Set(262, 0f);
			skillLevel.Top.Set(8, 0f);
			skillLevel.Width.Set(20, 0f);
			skillLevel.Height.Set(20, 0f);
			area.Append(skillLevel);

			s1 = new();
			s1.Left.Set(2, 0f);
			s1.Top.Set(-8, 0f);
			s1.Width.Set(64, 0f);
			s1.Height.Set(64, 0f);
			s1.OnLeftClick += (_, _) => ChangeSkill(0);
			area.Append(s1);

			s2 = new();
			s2.Left.Set(72, 0f);
			s2.Top.Set(-8, 0f);
			s2.Width.Set(64, 0f);
			s2.Height.Set(64, 0f);
			s2.OnLeftClick += (_, _) => ChangeSkill(1);
			area.Append(s2);

			s3 = new();
			s3.Left.Set(142, 0f);
			s3.Top.Set(-8, 0f);
			s3.Width.Set(64, 0f);
			s3.Height.Set(64, 0f);
			s3.OnLeftClick += (_, _) => ChangeSkill(2);
			area.Append(s3);
		}
		private static void ChangeSkill(int index, bool force = false) {
			Player p = Main.LocalPlayer;
			if (p.HeldItem.ModItem is not UpgradeWeaponBase)
				return;
			var mp = p.GetModPlayer<WeaponPlayer>();
			if (!force && (mp.SkillCount <= index || mp.Skill == index))
				return;
			mp.Skill = index;
			SkillData data = mp.CurrentSkill;

			if (data == null) {
				/*var stackTrace = new StackTrace();
				var frame = stackTrace.GetFrame(1);
				var method = frame.GetMethod();
				var callingClass = method.DeclaringType.Name;
				Main.NewText($"[{callingClass}] 错误: 当前技能数据mp.CurrentSkill为null", Color.Red);*/
				return;
			}

			// 添加 null 检查防止 NullReferenceException
			if (ins?.skillLevel != null) {
				ins.skillLevel.SetText((data.ForceReplaceLevel ?? data.Level).ToString());
			}
			else {
				// 详细的错误提示
				string errorMsg = "UI元素初始化失败: ";
				if (ins == null)
					errorMsg += "技能UI实例(ins)为null，请确保UI已正确初始化";
				else
					errorMsg += "技能等级文本框(skillLevel)为null，请检查UI元素绑定";

				// 输出错误信息（根据你的框架选择合适的方式）
				Main.NewText(errorMsg, Color.Red);
				// 或者使用日志系统
				// YourMod.Instance.Logger.Error(errorMsg);

				// 调试用异常抛出（正式版可注释掉）
				// throw new InvalidOperationException(errorMsg);
			}

			//SoundEngine.PlaySound(click);
			SummonSkillSlot summon = ins?.summon; // 这里也添加了null检查
			if (!data.SummonSkill) {
				if (summon == null)
					return;
				summon.usable = false;
				return;
			}

			// 确保ins不为null再调用ActiveSummonUI
			ins?.ActiveSummonUI(data.SummonIcon.Value);
		}
		public static void ChangeSkillSlot(UpgradeWeaponBase ark) {
			ins.s1.SetSkill(ark.GetSkillData(0));
			ins.s2.SetSkill(ark.GetSkillData(1));
			ins.s3.SetSkill(ark.GetSkillData(2));
			ChangeSkill(0, true);
		}
		private void ActiveSummonUI(Texture2D icon) {
			if (summon == null) {
				summon = new(icon);
				summon.Left.Set(330, 0f);
				summon.Top.Set(150, 0f);
				Append(summon);
				Recalculate();
			}
			else
				summon.SetImage(icon);
			summon.usable = true;
		}
	}

	internal class SelectSkillsSystem : ModSystem
	{
		private UserInterface SelectSkillsUserInterface;

		internal SelectSkills SelectSkillsUI;
		private bool Visiable;

		public void ShowMyUI() {
			if (Visiable)
				return;
			Visiable = true;
			SelectSkillsUserInterface?.SetState(SelectSkillsUI);
		}

		public void HideMyUI() {
			if (!Visiable)
				return;
			Visiable = false;
			SelectSkillsUserInterface?.SetState(null);
		}

		public override void Load() {
			// All code below runs only if we're not loading on a server
			if (!Main.dedServ) {
				SelectSkillsUI = new();
				SelectSkillsUserInterface = new();
				SelectSkillsUserInterface.SetState(SelectSkillsUI);
				Visiable = true;
			}
		}

		public override void UpdateUI(GameTime gameTime) {
			SelectSkillsUserInterface?.Update(gameTime);
			if (Main.playerInventory) {
				HideMyUI();
			}
			else {
				if (Main.LocalPlayer.HeldItem.ModItem is UpgradeWeaponBase) {
					ShowMyUI();
				}
				else {
					HideMyUI();
				}
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars")) + 1;
			if (resourceBarIndex != -1) {
				layers.Insert(resourceBarIndex, new LegacyGameInterfaceLayer(
					"ArknightsMod: Skill Select",
					delegate {
						SelectSkillsUserInterface.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}
	}
}