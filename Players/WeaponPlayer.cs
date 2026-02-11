using ArknightsMod.Common.UI;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Items.Weapons.Caster.Lava;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Nian;
using ArknightsMod.Content.Items.Weapons.Defender.NoirCorne;
using ArknightsMod.Content.Items.Weapons.Guard.Chen;
using ArknightsMod.Content.Items.Weapons.Guard.SilverAsh;
using ArknightsMod.Content.Items.Weapons.Guard.Thorns;
using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using ArknightsMod.Content.Items.Weapons.Sniper.Kroos;
using ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter;
using ArknightsMod.Content.Items.Weapons.Sniper.Pozemka;
using ArknightsMod.Content.Items.Weapons.Sniper.Schwarz;
using ArknightsMod.Content.Items.Weapons.Sniper.Shirayuki;
using ArknightsMod.Content.Items.Weapons.Vanguard.Bagpipe;
using ArknightsMod.Systems.Gameplay.Skill;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Players
{
	public class WeaponPlayer : ModPlayer
	{
		public int defenseBonus = 0;
		protected override bool CloneNewInstances => true;
		public SkillData CurrentSkill => SkillData[Skill];
		public int SkillCount { get; private set; }
		public readonly SkillData[] SkillData = new SkillData[3];

		// 技能资源管理
		public int SkillCharge;
		public int SkillChargeMax;
		public bool SkillActive;
		public int SkillTimer;
		public int SP;
		public int StockCount;
		public int Div;
		public int Skill;
		public bool SummonMode;
		public bool SkillInitialize = true;

		// SP恢复加成系统
		public float SPRegenMultiplier { get; set; } = 1f;
		private float spRegenFraction;
		private bool chargeReady;
		private bool hasNearbyEnemy;
		private int initChargeTimer;

		// 位置信息
		public float mousePositionX;
		public float mousePositionY;
		public float playerPositionX;
		public float playerPositionY;

		// 武器状态
		public bool HoldBagpipeSpear = false;
		public bool HoldChenSword = false;
		public bool HoldKroosCrossbow = false;
		public bool HoldChenSword_Item = false;
		public bool HoldSilverAshWeapon = false;
		public bool HoldBeagleWeapon = false;
		public bool HoldShirayuki_Shuriken = false;
		public bool HoldLava_Dagger = false;
		public bool HoldThornsWeapon = false;
		public bool HoldKroosAlterCrossbow = false;
		public bool HoldExusiaiVector = false;
		public bool HoldPozemkaCrossbow = false;
		public bool HoldNianWeapon = false;
		public bool HoldNoirShield = false;
		public bool HoldSchwarzBow = false;

		private int oldHeld;
		private int oldSkill;

		// 技能数据结构
		public int HowManySkills = 0;
		public List<int?> InitialSP = [null, null, null];
		public List<int?> InitialSPs1List = [null, null, null, null, null, null, null, null, null, null];
		public List<int?> InitialSPs2List = [null, null, null, null, null, null, null, null, null, null];
		public List<int?> InitialSPs3List = [null, null, null, null, null, null, null, null, null, null];
		public List<int?> MaxSP = [null, null, null];
		public List<int?> MaxSPs1List = [null, null, null, null, null, null, null, null, null, null];
		public List<int?> MaxSPs2List = [null, null, null, null, null, null, null, null, null, null];
		public List<int?> MaxSPs3List = [null, null, null, null, null, null, null, null, null, null];
		public List<float> SkillActiveTime = [0, 0, 0];
		public List<float> SkillActiveTimeS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		public List<float> SkillActiveTimeS2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		public List<float> SkillActiveTimeS3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		public List<int> SkillLevel = [0, 0, 0];
		public List<int> StockMax = [0, 0, 0];
		public List<int> StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		public List<int> StockMaxS2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		public List<int> StockMaxS3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
		public List<bool> AutoTrigger = [false, false, false];
		public List<bool> ChargeTypeIsPerSecond = [false, false, false];
		public string IconName = "";
		public List<bool> ShowSummonIconBySkills = [false, false, false];
		public override void UpdateDead() {
			chargeReady = true;
		}

		public void InitSkill(bool giveCharge) {
			SkillData skill = CurrentSkill;

			if (skill == null) {
				// if(HowManySkills>0)
				// 	Main.NewText($"[{GetType()}] 错误: 当前技能数据mp.CurrentSkill为null", Color.Red);
				return;
			}

			SkillLevelData data = skill.CurrentLevelData;
			Div = skill.ChargeType == SkillChargeType.Auto ? 60 : 1;
			int initSP = data.InitSP;
			int maxSP = data.MaxSP;
			if (giveCharge) {
				if (initSP == maxSP) {
					SkillCharge = 0;
					StockCount = 1;
					SP = StockCount == data.MaxStack ? maxSP : 0;
				}
				else {
					SkillCharge = initSP * Div;
					StockCount = 0;
					SP = initSP;
				}
			}
			else {
				SkillCharge = 0;
				SP = 0;
				StockCount = 0;
			}

			SkillChargeMax = maxSP * Div;
			SkillTimer = 0;
			SkillActive = false;
			SummonMode = false;
		}

		public void SetSkill(int skill) {
			if (SkillInitialize) {
				Skill = skill;
				if (ChargeTypeIsPerSecond[Skill])
					Div = 60;
				else
					Div = 1;

				SkillCharge = InitialSP[Skill] != null ? (int)InitialSP[Skill] * Div : 0;
				SP = InitialSP[Skill] != null ? (int)InitialSP[Skill] : 0;
				SkillChargeMax = MaxSP[Skill] != null ? (int)MaxSP[Skill] * Div : 0;
				SkillTimer = 0;
				StockCount = 0;
				SkillActive = false;

				if (InitialSP[Skill] == MaxSP[Skill]) {
					SkillCharge = 0;
					StockCount++;
					SP = StockCount == StockMax[Skill] ? (int)MaxSP[Skill] : 0;
				}

				SummonMode = false;
				SkillInitialize = false;
			}
		}

		public override void ResetEffects() {

			defenseBonus = 0;
			SPRegenMultiplier = 1f; // 重置SP恢复倍率（修改后的）
									// 更新武器状态
			HoldBagpipeSpear = Main.LocalPlayer.HeldItem.ModItem is BagpipeSpear;
			HoldExusiaiVector = Main.LocalPlayer.HeldItem.ModItem is ExusiaiVector;
			HoldKroosCrossbow = Main.LocalPlayer.HeldItem.ModItem is KroosCrossbow;
			HoldChenSword_Item = Main.LocalPlayer.HeldItem.ModItem is ChenSword_Item;
			HoldSilverAshWeapon = Main.LocalPlayer.HeldItem.ModItem is SilverAshWeapon;
			HoldBeagleWeapon = Main.LocalPlayer.HeldItem.ModItem is BeagleWeapon;
			HoldNoirShield = Main.LocalPlayer.HeldItem.ModItem is NoirShield;
			HoldThornsWeapon = Main.LocalPlayer.HeldItem.ModItem is ThornsWeapon;
			HoldShirayuki_Shuriken = Main.LocalPlayer.HeldItem.ModItem is Shirayuki_Shuriken;
			HoldLava_Dagger = Main.LocalPlayer.HeldItem.ModItem is Lava_Dagger;
			HoldKroosAlterCrossbow = Main.LocalPlayer.HeldItem.ModItem is KroosAlterCrossbow;
			HoldPozemkaCrossbow = Main.LocalPlayer.HeldItem.ModItem is PozemkaCrossbow;
			HoldNianWeapon = Main.LocalPlayer.HeldItem.ModItem is NianWeapon;
			HoldSchwarzBow = Main.LocalPlayer.HeldItem.ModItem is SchwarzBow;
			// 基于武器的技能系统
			hasNearbyEnemy = false;
			// 旧版武器支持
			SetAllSkillsData();
			Item item = Main.LocalPlayer.HeldItem;
			if (item.ModItem is UpgradeWeaponBase ark) {
				foreach (var npc in Main.ActiveNPCs) {
					if (npc.CanBeChasedBy(Player) && npc.Distance(Player.MountedCenter) < 20 * 16) {
						hasNearbyEnemy = true;
						initChargeTimer = 0;
						break;
					}
				}
				if (!hasNearbyEnemy && ++initChargeTimer >= GetRestoreTime()) {
					initChargeTimer = 0;
					chargeReady = true;
				}
				if (chargeReady) {
					chargeReady = false;
					ark.chargeReady = [true, true, true];
					if (Main.myPlayer == Player.whoAmI)
						CombatText.NewText(Player.Hitbox.Modified(0, -48, 0, 0), Microsoft.Xna.Framework.Color.Gold,
							Language.GetTextValue("Mods.ArknightsMod.Skills.ChargeReady"), true);
				}
				int type = item.type;
				if (type != oldHeld) {
					oldSkill = -1;
					Skill = 0;
					oldHeld = type;
					SkillCount = 0;

					for (int i = 0; i < 3; i++) {
						SkillData data = ark.GetSkillData(i);
						SkillData[i] = data;
						SkillCount += data == null ? 0 : 1;
					}

					SelectSkills.ChangeSkillSlot(ark);
				}

				if (oldSkill != Skill) {
					oldSkill = Skill;
					InitSkill(ark.chargeReady[Skill]);
					ark.chargeReady[Skill] = false;
				}
				else if (ark.chargeReady[Skill] && StockCount == 0) {
					ark.chargeReady[Skill] = false;
					SkillCharge = Math.Max(SkillCharge, ark.GetSkillData(Skill).LevelData[SkillLevel[Skill] - 1].InitSP * Div);
				}
			}
		}

		private int GetRestoreTime() {
			return 60 * 15;
		}

		public void TryAutoCharge() {
			if (!hasNearbyEnemy)
				return;
			if (CurrentSkill?.ChargeType == SkillChargeType.Auto)
				AutoCharge();
		}

		public void AutoCharge() {
			if (CurrentSkill != null) {
				SkillLevelData data = CurrentSkill.CurrentLevelData;
				if (!SkillActive && StockCount < data.MaxStack) {
					if (++SkillCharge % 60 == 0)
						SP++;

					if (SkillCharge == SkillChargeMax) {
						SkillCharge = 0;
						SP = ++StockCount == data.MaxStack ? data.MaxSP : 0;
					}
				}
			}
			else {
				// 旧版自动充能逻辑
				if (!SkillActive && StockCount < StockMax[Skill]) {
					SkillCharge += 1;
					if (SkillCharge != 0 && SkillCharge % 60 == 0)
						SP += 1;

					if (SkillCharge == SkillChargeMax) {
						SkillCharge = 0;
						StockCount += 1;
						SP = StockCount == StockMax[Skill] ? (int)MaxSP[Skill] : 0;
					}
				}
			}
		}

		public void OffensiveRecovery() {
			if (CurrentSkill != null) {
				SkillLevelData data = CurrentSkill.CurrentLevelData;
				if (!SkillActive && StockCount < data.MaxStack) {
					SkillCharge++;
					SP++;
				}

				if (SkillCharge == SkillChargeMax) {
					SkillCharge = 0;
					SP = ++StockCount == data.MaxStack ? data.MaxSP : 0;
				}
			}
			else {
				// 旧版攻击恢复逻辑
				if (!SkillActive && StockCount < StockMax[Skill]) {
					SkillCharge += 1;
					if (SkillCharge != 0)
						SP += 1;
				}

				if (SkillCharge == SkillChargeMax) {
					SkillCharge = 0;
					StockCount += 1;
					SP = StockCount == StockMax[Skill] ? (int)MaxSP[Skill] : 0;
				}
			}
		}

		public void UpdateActiveSkill() {
			if (SkillActive) {
				if (CurrentSkill != null) {
					if (CurrentSkill.AutoUpdateActive && ++SkillTimer >= CurrentSkill.CurrentLevelData.ActiveTime * 60)
						SkillActive = false;
				}
				else {
					SkillTimer++;
					if (SkillTimer == SkillActiveTime[Skill] * 60)
						SkillActive = false;
				}
			}
		}
		public void UpdateActiveSkill2() {
			CurrentSkill.AutoUpdateActive = false;
		}

		public void StrikeSkill() {
			if (SkillActive) {
				SkillTimer++;
				if (SkillTimer == 10)
					SkillActive = false;
			}
		}

		public void DelStockCount() {
			if (CurrentSkill != null) {
				if (StockCount-- == CurrentSkill.CurrentLevelData.MaxStack)
					SP = 0;
			}
			else {
				if (StockCount == StockMax[Skill])
					SP = 0;
				StockCount -= 1;
			}
		}

		public void SetSkillData() {
			if (HowManySkills < 1) {
				InitialSPs1List = [null, null, null, null, null, null, null, null, null, null];
				MaxSPs1List = [null, null, null, null, null, null, null, null, null, null];
			}
			if (HowManySkills < 2) {
				InitialSPs2List = [null, null, null, null, null, null, null, null, null, null];
				MaxSPs2List = [null, null, null, null, null, null, null, null, null, null];
			}
			if (HowManySkills < 3) {
				InitialSPs3List = [null, null, null, null, null, null, null, null, null, null];
				MaxSPs3List = [null, null, null, null, null, null, null, null, null, null];
			}

			InitialSP = [
				InitialSPs1List[SkillLevel[0] - 1],
				InitialSPs2List[SkillLevel[1] - 1],
				InitialSPs3List[SkillLevel[2] - 1]
			];

			MaxSP = [
				MaxSPs1List[SkillLevel[0] - 1],
				MaxSPs2List[SkillLevel[1] - 1],
				MaxSPs3List[SkillLevel[2] - 1]
			];

			SkillActiveTime = [
				SkillActiveTimeS1List[SkillLevel[0] - 1],
				SkillActiveTimeS2List[SkillLevel[1] - 1],
				SkillActiveTimeS3List[SkillLevel[2] - 1]
			];

			StockMax = [
				StockMaxS1List[SkillLevel[0] - 1],
				StockMaxS2List[SkillLevel[1] - 1],
				StockMaxS3List[SkillLevel[2] - 1]
			];
		}

		public void SetAllSkillsData() {
			if (HoldBagpipeSpear) {
				IconName = "BagpipeSpear";
				HowManySkills = 3;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [true, true, true];
				AutoTrigger = [false, true, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 15];
				InitialSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				InitialSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 25];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 33];
				MaxSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 4];
				MaxSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 40];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f];
				SkillActiveTimeS2List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.5f];
				SkillActiveTimeS3List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 20f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				StockMaxS2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 3];
				StockMaxS3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				SetSkillData();
			}
			else if (HoldChenSword) {
				IconName = "ChenSword";
				HowManySkills = 2;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, false, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				InitialSPs2List = [10, 10, 10, 10, 10, 10, 10, 13, 16, 20];
				MaxSPs1List = [7, 7, 7, 6, 6, 6, 5, 5, 5, 4];
				MaxSPs2List = [40, 40, 40, 38, 38, 38, 36, 34, 32, 30];
				SkillActiveTimeS1List = [0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f];
				SkillActiveTimeS2List = [1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f];
				StockMaxS1List = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
				StockMaxS2List = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
				SetSkillData();
			}
			else if (HoldKroosCrossbow) {
				IconName = "KroosCrossbow";
				HowManySkills = 1;
				SkillLevel = [7, 7, 7];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 4, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 1, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldChenSword_Item) {
				IconName = "ChenSword_Item";
				HowManySkills = 1;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 4, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0.2f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 1, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldSilverAshWeapon) {
				IconName = "SilverAshWeapon";
				HowManySkills = 1;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldBeagleWeapon) {
				IconName = "BeagleWeapon";
				HowManySkills = 1;
				SkillLevel = [7, 7, 7];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldNoirShield) {
				IconName = "NoirShield";
				HowManySkills = 0;
				SkillLevel = [0, 0, 0];
				ChargeTypeIsPerSecond = [false, false, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldThornsWeapon) {
				IconName = "ThornsWeapon";
				HowManySkills = 1;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldShirayuki_Shuriken) {
				IconName = "Shirayuki_Shuriken";
				HowManySkills = 1;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldLava_Dagger) {
				IconName = "Lava_Dagger";
				HowManySkills = 1;
				SkillLevel = [7, 7, 7];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldKroosAlterCrossbow) {
				IconName = "KroosAlterCrossbow";
				HowManySkills = 1;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				SetSkillData();
			}
			else if (HoldExusiaiVector) {
				IconName = "ExusiaiVector";
				HowManySkills = 3;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, false];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				InitialSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 25];
				InitialSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 20];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 4];
				MaxSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 35];
				MaxSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 30];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.2f];
				SkillActiveTimeS2List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 15f];
				SkillActiveTimeS3List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 15f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				StockMaxS2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				StockMaxS3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				SetSkillData();
			}
			else if (HoldPozemkaCrossbow) {
				IconName = "PozemkaCrossbow";
				HowManySkills = 3;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [false, true, true];
				AutoTrigger = [true, false, false];
				ShowSummonIconBySkills = [true, true, true];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				InitialSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 9];
				InitialSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 23];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 20];
				MaxSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 9];
				MaxSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 35];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 30f];
				SkillActiveTimeS2List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.4f];
				SkillActiveTimeS3List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 30f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				StockMaxS2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 2];
				StockMaxS3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				SetSkillData();
			}
			else if (HoldNianWeapon) {
				IconName = "NianWeapon";
				HowManySkills = 3;
				SkillLevel = [10, 10, 10];
				ChargeTypeIsPerSecond = [true, true, true];
				AutoTrigger = [false, false, false];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 10];
				InitialSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 35];
				InitialSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 70];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 30];
				MaxSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 50];
				MaxSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 85];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 30f];
				SkillActiveTimeS2List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f];
				SkillActiveTimeS3List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 45f];
				StockMaxS1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				StockMaxS2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				StockMaxS3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
				SetSkillData();
			}

			else if (HoldSchwarzBow) {
				IconName = "SchwarzBow";
				HowManySkills = 3;
				SkillLevel = new() { 10, 10, 10 };
				ChargeTypeIsPerSecond = new() { false, true, true };
				AutoTrigger = new() { true, false, false };
				ShowSummonIconBySkills = new() { false, false, false };

				InitialSPs1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				InitialSPs2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 20 };
				InitialSPs3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 12 };
				MaxSPs1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 3 };
				MaxSPs2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 30 };
				MaxSPs3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 25 };
				SkillActiveTimeS1List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.4f };
				SkillActiveTimeS2List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 40f };
				SkillActiveTimeS3List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 25f };
				StockMaxS1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				SetSkillData();
			}

			if (HowManySkills > 0) {
				SetSkill(Skill);
			}
		}
	}
}