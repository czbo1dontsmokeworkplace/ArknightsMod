using ArknightsMod.Common.UI;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Specialist.Scene;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Items.Weapons.Guard.Lupine;
using ArknightsMod.Content.Items.Weapons.Guard.Oblivionis;
using ArknightsMod.Content.Items.Weapons.Caster.Lava;
using ArknightsMod.Content.Items.Weapons.Caster.Haze;
using ArknightsMod.Content.Items.Weapons.Defender.Beagle;
using ArknightsMod.Content.Items.Weapons.Defender.Nian;
using ArknightsMod.Content.Items.Weapons.Defender.NoirCorne;
using ArknightsMod.Content.Items.Weapons.Guard.Chen;
using ArknightsMod.Content.Items.Weapons.Guard.SilverAsh;
using ArknightsMod.Content.Items.Weapons.Guard.Thorns;
using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using ArknightsMod.Content.Items.Weapons.Sniper.Jessica;
using ArknightsMod.Content.Items.Weapons.Sniper.Kroos;
using ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter;
using ArknightsMod.Content.Items.Weapons.Sniper.Pozemka;
using ArknightsMod.Content.Items.Weapons.Sniper.Schwarz;
using ArknightsMod.Content.Items.Weapons.Sniper.Shirayuki;
using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using ArknightsMod.Content.Items.Weapons.Vanguard.Bagpipe;
using ArknightsMod.Content.Items.Weapons.Caster.Haze;
using ArknightsMod.Content.Items.Weapons.Medic.Closure;
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
		private int baseSkillChargeMax;
		// Setter 始终接收基础充能阈值；Getter 动态应用装备减耗，穿脱时无需重置技能。
		public int SkillChargeMax {
			get => Div > 0 && baseSkillChargeMax > 0
				? baseSkillChargeMax - (baseSkillChargeMax / Div - GetEffectiveSkillSP(baseSkillChargeMax / Div)) * Div
				: baseSkillChargeMax;
			set => baseSkillChargeMax = value;
		}

		/// <summary>读取当前装备下的实际技力需求，不修改武器/CSV 中的基础技能数据。</summary>
		public int GetEffectiveSkillSP(int baseSP) {
			if (baseSP <= 0 || CurrentSkill?.UsesCustomCharge == true
				|| !Player.HeldItem.DamageType.CountsAsClass(DamageClass.Ranged))
				return baseSP;
			bool helmet = Player.armor[0].type == Content.Items.Armor.NeoArmorReforge.NeoArmorReforgeSetLoader
				.GetSetType<Content.Items.Armor.Sniper.Catapult.CatapultHead>();
			return helmet ? Math.Max(1, baseSP - 1) : baseSP;
		}
		private bool skillActive;
		public bool SkillActive {
			get => skillActive;
			set {
				bool starting = value && !skillActive;
				skillActive = value;
				if (starting) Systems.Gameplay.MujicaFeverSystem.SkillStarted(Player);
			}
		}
		private int skillTimer;
		public int SkillTimer {
			get => skillTimer;
			set {
				if (value > skillTimer && Systems.Gameplay.MujicaFeverSystem.PauseSkill(Player)) return;
				skillTimer = value;
			}
		}
		public int SP;
		public int StockCount;
		public int Div;
		public int Skill;
		public bool SummonMode;
		public bool SkillInitialize = true;

		/// <summary>
		/// 「换武器后需要刷新技能槽 UI」的待处理标记。
		/// ResetEffects 里不能直接调用 UI：它同时会被"加载角色存档"（Player.Deserialize）
		/// 和"主菜单枚举角色列表"（Main.LoadPlayers）触发，那时 Main.LocalPlayer 还是未初始化
		/// ModPlayer 数组的裸 Player，调用 GetModPlayer 会 IndexOutOfRangeException，
		/// 进而让角色文件加载失败、角色无法使用。这里只登记标记，真正的刷新放到 PostUpdate。
		/// </summary>
		public bool pendingSkillSlotRefresh;

		// SP恢复加成系统
		public float SPRegenMultiplier { get; set; } = 1f;
		private float spRegenFraction;
		private float emergencyChargeFraction;

		/// <summary>
		/// 全局技力恢复速度倍率：开启配置开关时为 2（二倍速），关闭时为 1（原速）。
		/// 只影响"技力条恢复的快慢"（自然回复/受击回复/攻击回复/饰品加速），不影响任何数值——
		/// 技能所需技力、技力上限等仍按 CSV/武器数据里 1 倍速的原始数值走，
		/// 部署费用吸收、套装/技能直接赠送技力等"技力返还"效果同样不受这个倍率影响
		/// （统一通过 <see cref="TryAddSP"/> 推进实际充能，不乘自然回复倍率）。
		/// </summary>
		public static int GlobalSPRegenSpeedMultiplier =>
			ModContent.GetInstance<Common.Configs.SkillChargeConfig>().DoubleSPRegenSpeed ? 2 : 1;

		/// <summary>
		/// 技能开启后，"消耗"技力（即倒数持续时间）的速度倍率：开启配置时为 2（二倍速），
		/// 关闭时为 1（原速）。用在 UpdateActiveSkill/StrikeSkill 里，把用来判断"技能是否
		/// 该结束"的时长阈值按这个倍率缩小——阈值越小，同样每帧 +1 的 SkillTimer 越快到达，
		/// 表现就是持续时间变成原来的 1/2。CSV/武器数据里的持续时间数值本身不受影响。
		/// </summary>
		public static float ActiveDurationMultiplier =>
			ModContent.GetInstance<Common.Configs.SkillChargeConfig>().DoubleSkillDurationConsumption ? 0.5f : 1f;

		private bool chargeReady;
		private bool chargeOpen;
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
		public bool HoldTyphonBow = false;
		public bool HoldHazeMagicBook = false;
		public bool HoldSurtrLaevatain = false;
		public bool HoldJessicaGun = false;

		private int oldHeld;
		private int oldSkill;
		private readonly Dictionary<int, int> selectedSkillByItemType = [];

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
			// 死亡等价于整批干员重新部署：背包里每一把技能武器都要恢复自己的初始技力资格，
			// 不能只让复活后最先拿起的那一把吃到玩家级重置标记。
			foreach (Item item in Player.inventory) {
				if (item.ModItem is UpgradeWeaponBase weapon)
					Array.Fill(weapon.chargeReady, true);
			}
			ClearSkillRuntime();
			ClearLoadedSkillIdentity();
			chargeReady = true;
			chargeOpen = true;
		}

		//======================================================================
		//新添入的，用于更新
		public override void PostUpdate() {
			UnderAttack = false;

			// 装备变化可能让已积攒的技力达到新需求，立即结算并更新显示。
			if (!Player.dead && !SkillActive && CurrentSkill != null && !CurrentSkill.UsesCustomCharge
				&& SkillChargeMax > 0 && Div > 0) {
				int maxStack = CurrentSkill.CurrentLevelData.MaxStack;
				if (StockCount < maxStack && SkillCharge >= SkillChargeMax) {
					int gained = Math.Min(SkillCharge / SkillChargeMax, maxStack - StockCount);
					StockCount += gained;
					SkillCharge = StockCount >= maxStack ? 0 : SkillCharge % SkillChargeMax;
				}
				SP = StockCount >= maxStack ? GetEffectiveSkillSP(CurrentSkill.CurrentLevelData.MaxSP)
					: SkillCharge / Div;
			}

			// 换武器后的技能槽 UI 刷新（登记于 ResetEffects，见 pendingSkillSlotRefresh 的说明）。
			// PostUpdate 只在实际游戏中运行，此时玩家与其 ModPlayer 数组都已初始化；
			// 同时再校一遍"确实是本地玩家本人"，避免任何非游戏内路径进到 UI 里。
			if (pendingSkillSlotRefresh) {
				pendingSkillSlotRefresh = false;
				if (!Main.dedServ && !Main.gameMenu && Player.whoAmI == Main.myPlayer
					&& Main.LocalPlayer == Player
					&& Player.HeldItem.ModItem is UpgradeWeaponBase ark)
					SelectSkills.ChangeSkillSlot(ark, Player);
			}

			if (!Player.dead && (HowManySkills > 0 || SkillCount > 0)) {
				if (CurrentSkill?.ChargeType == SkillChargeType.Auto && CurrentSkill.UsesCustomCharge != true) {
					AccessoriesAutoCharge();
				}
				else if (CurrentSkill == null && ChargeTypeIsPerSecond[Skill]) {
					AccessoriesAutoCharge();
				}
			}

			// 提丰 S3：空闲帧也保持随实时准星调整的迫击炮式举弓姿势。
			if (!Player.dead && Skill == 2 && SkillActive && Player.itemAnimation <= 0
			    && Player.HeldItem.ModItem is TyphonBow) {
				TyphonBow.ApplyS3Pose(Player);
			}

			// 可露希尔·扫描枪开火后坐力：必须在 PostUpdate（原版 ItemCheck_ApplyUseStyle 之后）
			// 才能叠加 itemRotation，否则在 HoldItem 里设置会被随后的原版持枪瞄准逻辑覆盖掉，
			// 表现为"抬起动作完全不生效"。
			if (Player.HeldItem.ModItem is ClosureScanGun closureGun) {
				closureGun.ApplyRecoilKick(Player);
			}
		}
		//=======================================================================
		public void InitSkill(bool giveCharge) {
			AccessoriesChargeFraction = 0d;
			SkillData skill = CurrentSkill;

			if (skill == null) {
				// if(HowManySkills>0)
				// 	Main.NewText($"[{GetType()}] 错误: 当前技能数据mp.CurrentSkill为null", Color.Red);
				return;
			}

			SkillLevelData data = skill.CurrentLevelData;
			Div = skill.ChargeType == SkillChargeType.Auto ? 60 : 1;
			int maxSP = GetEffectiveSkillSP(data.MaxSP);
			int initSP = Math.Min(data.InitSP, maxSP);
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

			SkillChargeMax = data.MaxSP * Div;
			SkillTimer = 0;
			SkillActive = false;
			SummonMode = false;
		}

		/// <summary>
		/// 原子地切换当前武器技能：旧技能在同一帧结束，新技能资源也在同一帧完成初始化，
		/// 避免 UI 先改索引、下一帧才清状态造成的“旧技能效果套到新技能”串态。
		/// </summary>
		public bool TrySelectSkill(UpgradeWeaponBase weapon, int skill, bool force = false) {
			if (weapon == null || skill < 0 || skill >= SkillData.Length || SkillData[skill] == null)
				return false;
			if (!force && (SkillCount <= skill || Skill == skill))
				return false;

			Skill = skill;
			selectedSkillByItemType[weapon.Item.type] = skill;
			oldSkill = skill;
			InitSkill(weapon.chargeReady[skill]);
			weapon.chargeReady[skill] = false;
			return true;
		}

		private void ClearSkillRuntime() {
			AccessoriesChargeFraction = 0d;
			SkillCharge = 0;
			SkillChargeMax = 0;
			SkillActive = false;
			SkillTimer = 0;
			SP = 0;
			StockCount = 0;
			Div = 1;
			SummonMode = false;
		}

		private void ClearLoadedSkillIdentity() {
			oldHeld = 0;
			oldSkill = -1;
			SkillCount = 0;
			for (int i = 0; i < SkillData.Length; i++)
				SkillData[i] = null;
		}

		// 将当前技能技力填满至可释放状态
		public void DevFillSkillCharge()
		{
			if (CurrentSkill == null || SkillActive)
				return;

			SkillLevelData data = CurrentSkill.CurrentLevelData;
			StockCount = data.MaxStack;
			SkillCharge = 0;
			SP = GetEffectiveSkillSP(data.MaxSP);
			chargeOpen = true;

			if (Player.HeldItem.ModItem is UpgradeWeaponBase ark)
				ark.chargeReady = [true, true, true];
		}

		/// <summary>
		/// 收藏应急充能手册时使用：无视自然/攻击/受击回复类型，在最多 60 tick 内补出下一层技能库存。
		/// 这里只推进当前手持武器的当前技能，不改变技能持续时间，也不会凭空重复填满多层库存。
		/// </summary>
		internal void ApplyEmergencyOneSecondCharge() {
			if (Player.GetModPlayer<Content.Items.Accessories.Developer.RecoverySuppressionPlayer>().Equipped) {
				emergencyChargeFraction = 0f;
				return;
			}
			bool isArknightsSkillWeapon = Player.HeldItem.ModItem?.Mod == Mod
				&& (CurrentSkill != null || HowManySkills > 0 || SkillCount > 0);
			if (!isArknightsSkillWeapon || SkillActive || SkillChargeMax <= 0) {
				emergencyChargeFraction = 0f;
				return;
			}

			int maxStock = CurrentSkill?.CurrentLevelData.MaxStack
				?? (Skill >= 0 && Skill < StockMax.Count ? StockMax[Skill] : 0);
			if (maxStock <= 0 || StockCount >= maxStock) {
				emergencyChargeFraction = 0f;
				return;
			}

			emergencyChargeFraction += SkillChargeMax / 60f;
			int gain = (int)emergencyChargeFraction;
			if (gain <= 0)
				return;

			emergencyChargeFraction -= gain;
			SkillCharge += gain;
			if (SkillCharge >= SkillChargeMax) {
				SkillCharge = 0;
				StockCount++;
				emergencyChargeFraction = 0f;
			}

			int maxSP = GetEffectiveSkillSP(CurrentSkill?.CurrentLevelData.MaxSP
				?? (Skill >= 0 && Skill < MaxSP.Count ? MaxSP[Skill] ?? 0 : 0));
			SP = StockCount >= maxStock ? maxSP : SkillCharge / Math.Max(1, Div);
		}

		/// <summary>
		/// 当前技能是否可以接受外部赠送的技力。专属充能技能由武器自己维护，
		/// 不写公共充能条；技能期间、充能冻结或库存已满时也不接受。
		/// </summary>
		private bool CanAddSP() {
			if (!Player.active || Player.dead || SkillActive
				|| Skill < 0 || Skill >= SkillData.Length
				|| CurrentSkill == null || CurrentSkill.UsesCustomCharge
				|| CurrentSkill.ChargeType == SkillChargeType.None
				|| Div <= 0 || SkillChargeMax <= 0
				|| SceneCameraSkills.BlocksSkillCharge(Player))
				return false;

			SkillLevelData data = CurrentSkill.CurrentLevelData;
			return data.MaxSP > 0 && data.MaxStack > 0 && StockCount >= 0 && StockCount < data.MaxStack;
		}

		/// <summary>
		/// 为当前技能增加可见技力，同时更新实际充能、技能库存与显示值。
		/// 自然回复技能 Div=60，攻击/受击回复技能 Div=1；跨过充能上限时保留
		/// 下一层的余量，库存全满后丢弃溢出。成功增加资源时返回 true。
		/// </summary>
		public bool TryAddSP(int points) {
			if (points <= 0 || !CanAddSP())
				return false;

			SkillLevelData data = CurrentSkill.CurrentLevelData;
			long availableCharge = ((long)data.MaxStack - StockCount) * SkillChargeMax - Math.Max(0, SkillCharge);
			long acceptedCharge = Math.Min((long)points * Div, availableCharge);
			long charge = Math.Max(0, SkillCharge) + (long)points * Div;
			int gainedStock = (int)Math.Min(charge / SkillChargeMax, data.MaxStack - StockCount);
			StockCount += gainedStock;
			SkillCharge = StockCount >= data.MaxStack ? 0 : (int)(charge % SkillChargeMax);
			SP = StockCount >= data.MaxStack ? GetEffectiveSkillSP(data.MaxSP) : SkillCharge / Div;
			if (acceptedCharge > 0 && !Main.dedServ && Main.netMode != Terraria.ID.NetmodeID.Server) {
				string amount = (acceptedCharge / (double)Div).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
				CombatText.NewText(Player.Hitbox.Modified(0, -24, 0, 0),
					new Microsoft.Xna.Framework.Color(80, 160, 255), "+" + amount);
			}
			return true;
		}

		/// <summary>没有可用充能空间时不吸收部署费用，让费用继续环绕等待。</summary>
		public bool CanAbsorbDeploymentCost() {
			return Player.HeldItem.ModItem is UpgradeWeaponBase && CanAddSP();
		}

		/// <summary>部署费用与套装赠送技力共用同一个资源结算入口。</summary>
		public bool AbsorbDeploymentCost(int points) {
			return CanAbsorbDeploymentCost() && TryAddSP(points);
		}

		public void SetSkill(int skill) {
			// An active standard-bearer skill owns its slot until its timer ends.
			if (global::ArknightsMod.Content.Projectiles.Vanguard.StandardBearers.StandardBearerPlayer.IsPhased(Player))
				return;
			if (SkillInitialize) {
				AccessoriesChargeFraction = 0d;
				Skill = skill;
				if (ChargeTypeIsPerSecond[Skill])
					Div = 60;
				else
					Div = 1;

				int maxSP = GetEffectiveSkillSP(MaxSP[Skill] ?? 0);
				int initSP = Math.Min(InitialSP[Skill] ?? 0, maxSP);
				SkillCharge = initSP * Div;
				SP = initSP;
				SkillChargeMax = MaxSP[Skill] != null ? (int)MaxSP[Skill] * Div : 0;
				SkillTimer = 0;
				StockCount = 0;
				SkillActive = false;

				if (initSP == maxSP) {
					SkillCharge = 0;
					StockCount++;
					SP = StockCount == StockMax[Skill] ? GetEffectiveSkillSP((int)MaxSP[Skill]) : 0;
				}

				SummonMode = false;
				SkillInitialize = false;
			}
		}

		public override void ResetEffects() {

			defenseBonus = 0;
			SPRegenMultiplier = 1f; // 重置SP恢复倍率（修改后的）

			// 更新武器状态
			HoldBagpipeSpear = Player.HeldItem.ModItem is BagpipeSpear;
			HoldExusiaiVector = Player.HeldItem.ModItem is ExusiaiVector;
			HoldKroosCrossbow = Player.HeldItem.ModItem is KroosCrossbow;
			HoldChenSword_Item = Player.HeldItem.ModItem is ChenSword_Item;
			HoldSilverAshWeapon = Player.HeldItem.ModItem is SilverAshWeapon;
			HoldBeagleWeapon = Player.HeldItem.ModItem is BeagleWeapon;
			HoldNoirShield = Player.HeldItem.ModItem is NoirShield;
			HoldThornsWeapon = Player.HeldItem.ModItem is ThornsWeapon;
			HoldShirayuki_Shuriken = Player.HeldItem.ModItem is Shirayuki_Shuriken;
			HoldLava_Dagger = Player.HeldItem.ModItem is Lava_Dagger;
			HoldKroosAlterCrossbow = Player.HeldItem.ModItem is KroosAlterCrossbow;
			HoldPozemkaCrossbow = Player.HeldItem.ModItem is PozemkaCrossbow;
			HoldNianWeapon = Player.HeldItem.ModItem is NianWeapon;
			HoldSchwarzBow = Player.HeldItem.ModItem is SchwarzBow;
			HoldTyphonBow  = Player.HeldItem.ModItem is TyphonBow;
			HoldHazeMagicBook  = Player.HeldItem.ModItem is HazeMagicBook;
			HoldSurtrLaevatain  = Player.HeldItem.ModItem is SurtrLaevatain;
			HoldJessicaGun  = Player.HeldItem.ModItem is JessicaGun;
			// 基于武器的技能系统
			hasNearbyEnemy = false;
			// 旧版武器支持
			HowManySkills = 0;
			SetAllSkillsData();

			// 死亡期间不重新初始化手中武器。UpdateDead 已清空运行态；复活后的第一帧
			// 再按当前物品的 InitSP 正常部署，技能不会跨死亡暂停后续上。
			if (Player.dead)
				return;

			Item item = Player.HeldItem;
			if (item.ModItem is UpgradeWeaponBase ark) {
				if (chargeOpen) {
					foreach (var npc in Main.ActiveNPCs) {
						if (npc.CanBeChasedBy(Player) && npc.Distance(Player.MountedCenter) < 35 * 16) {
							hasNearbyEnemy = true;
							initChargeTimer = 0;
							break;
						}
					}
					if (!hasNearbyEnemy && ++initChargeTimer >= GetRestoreTime()) {
						initChargeTimer = 0;
						// 仅解锁自然回复（关闭冻结），不要置 chargeReady——
						// chargeReady 会被 Propagate 成 ark.chargeReady，进而在下方
						// else if 分支把 InitSP 预灌进 SkillCharge，导致技能结束后
						// 技力不从 0 而直接从初始技力开始回复。
						chargeOpen = false;
					}
				}
				if (chargeReady) {
					chargeReady = false;
					chargeOpen = false;
					ark.chargeReady = [true, true, true];
					if (Player == Main.LocalPlayer)
						CombatText.NewText(Player.Hitbox.Modified(0, -48, 0, 0), Microsoft.Xna.Framework.Color.Gold,
							Language.GetTextValue("Mods.ArknightsMod.Skills.ChargeReady"), true);
				}
				int type = item.type;
				if (type != oldHeld) {
					oldSkill = -1;
					oldHeld = type;
					SkillCount = 0;

					for (int i = 0; i < 3; i++) {
						SkillData data = ark.GetSkillData(i);
						SkillData[i] = data;
						SkillCount += data == null ? 0 : 1;
					}

					Skill = selectedSkillByItemType.TryGetValue(type, out int selected)
						&& selected >= 0 && selected < SkillData.Length && SkillData[selected] != null
						? selected
						: 0;
					selectedSkillByItemType[type] = Skill;

					// ⚠ 这里不能直接调 UI。ResetEffects 除了游戏内每帧执行，还会在
					// "加载角色存档"（Player.Deserialize）与"主菜单枚举角色列表"（Main.LoadPlayers）
					// 时被调用；那一刻 Main.LocalPlayer 还是 ModPlayer 数组未初始化的裸 Player，
					// 直接进 UI 会 IndexOutOfRangeException，导致该角色文件加载失败、角色无法使用。
					// 因此只登记待刷新标记，真正的 UI 刷新交给 PostUpdate（只在实际游戏中运行）。
					if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
						pendingSkillSlotRefresh = true;
				}

				if (oldSkill != Skill) {
					oldSkill = Skill;
					// 切换武器（oldHeld 改变时 oldSkill 被置 -1）或切换技能槽（mp.Skill 改变）时，
					// 一律按"初始就绪"授予初始技力 InitSP，使玩家每次换武器/换技能都从初始技力起步。
					// 不再依赖 ark.chargeReady（该标志只用于 DevFillSkillCharge / 重生等少数场景），
					// 否则正常游玩中切换不会给初始技力，且与上一处冻结解锁污染初始技力的问题同源。
					InitSkill(true);
					ark.chargeReady[Skill] = false;
				}
				else if (ark.chargeReady[Skill] && StockCount == 0) {
					ark.chargeReady[Skill] = false;

					// 脱战/复活补回“初始技力下限”时，数值条、库存和内部 tick 必须一起更新。
					// 只写 SkillCharge 会出现条上仍显示旧 SP，InitSP == MaxSP 时也不会变成可释放库存。
					SkillData data = ark.GetSkillData(Skill);
					if (data != null) {
						SkillLevelData level = data.CurrentLevelData;
						if (level.InitSP >= GetEffectiveSkillSP(level.MaxSP)) {
							SkillCharge = 0;
							StockCount = Math.Min(level.MaxStack, 1);
							SP = GetEffectiveSkillSP(level.MaxSP);
						}
						else {
							SkillCharge = Math.Max(SkillCharge, level.InitSP * Div);
							SP = SkillCharge / Math.Max(1, Div);
						}
					}
				}
			}
			else if (oldHeld != 0) {
				// 定时技能不能靠换到普通物品来冻结倒计时；离开技能武器即清场。
				// 煌 S2 的“本次生命永久”由其专属 ModPlayer 显式恢复，不依赖这份公共残留状态。
				ClearSkillRuntime();
				ClearLoadedSkillIdentity();
			}
		}

		// 释放技能后，如果附近 560px 内没有可仇恨的敌人，自然回复会被冻结，
		// 直到出现敌人、或等够这个时长才解锁（见 ResetEffects 里 chargeOpen/hasNearbyEnemy 的判定）。
		// 该冻结窗口是自动充能型技能（如隐德来希 S1）放完技能后无敌人时回充被挡的"死区"。
		// 原 5 秒过长，对瞬发/自动释放技能感知明显；现缩短到 12 帧（0.2 秒），既保留极短的节奏间隔
		// 又不致明显卡住回充。这是全局值，作用于所有武器，不是某个武器专属的。
		private int GetRestoreTime() {
			return 12;
		}

		public void TryAutoCharge() {
			if (SceneCameraSkills.BlocksSkillCharge(Player)) // 稀音技能持续期间冻结充能
				return;
			// 标记为专属充能的技能由武器自己的部署状态驱动；否则公共条和专属暖机同时推进，
			// 切换快捷栏后会出现两套进度分叉。
			if (CurrentSkill?.UsesCustomCharge == true)
				return;
			if (chargeOpen && !hasNearbyEnemy)
				return;
			if (CurrentSkill?.ChargeType == SkillChargeType.Auto)
				AutoCharge();
		}
		/// <summary>
		/// 玩家受击
		/// </summary>
		public bool UnderAttack = false;

		public override void PreUpdate()
		{
			if (Player.lifeRegen < 0)
				UnderAttack = true;
		}

		/// <summary>
		/// 统一技能开启热键的核心：ProcessTriggers 只在本地客户端跑，是自定义 ModKeybind
		/// 唯一能读到"当前是否按住"的地方。各武器的 CanUseItem/Shoot 里已经改成检查
		/// ArknightsKeybinds.SkillActivatePressed(player)，但那些钩子本身只在玩家真的
		/// 左键/右键点击、原版判定"这次点击要不要交给物品使用"时才会被调用——单纯按热键
		/// 并不会触发它们。这里在按住热键时把 Player.controlUseItem 设成 true，相当于
		/// "伪造一次左键点击"，把它接入原版本来就有的点击处理管线，武器那边的判定才有
		/// 机会跑起来。之所以伪造左键而不是右键：这样武器代码里剩下的
		/// player.altFunctionUse==2（比如摄影车部署、浮游信标）不会被误触发——两者互不干扰。
		///
		/// 只在手持本模组的技能武器时才伪造点击，避免误把其它物品（工具、纯输出武器、
		/// 别的模组物品）也顺带触发一次使用。LupineScarlet/OblivionisSword 没有继承
		/// UpgradeWeaponBase（历史遗留），单独列出来。
		/// </summary>
		public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
			if (!ArknightsKeybinds.SkillActivatePressed(Player))
				return;

			if (Player.HeldItem.ModItem is UpgradeWeaponBase
				or LupineScarlet
				or OblivionisSword) {
				Player.controlUseItem = true;
			}
		}

		public void TryHurtCharge()
		{
			if (CurrentSkill?.ChargeType == SkillChargeType.Hurt)
				HurtCharge();
		}

		/// <summary>
		/// 受击回复技力
		/// </summary>
		public void HurtCharge()
		{
			if (CurrentSkill != null)
			{
				SkillLevelData data = CurrentSkill.CurrentLevelData;
				if (!SkillActive && StockCount < data.MaxStack)
				{
					// 受击回复类型 Div=1，SkillCharge 和 SP 一一对应；倍率大于 1 时用 >= 而非 ==
					// 判满，避免（比如 MaxSP 为奇数时）两点两点跳过精确等于 SkillChargeMax 那一格。
					int gain = GlobalSPRegenSpeedMultiplier;
					SP += gain;
					SkillCharge += gain;
					if (SkillCharge >= SkillChargeMax)
					{
						SkillCharge=0;
						SP = ++StockCount == data.MaxStack ? GetEffectiveSkillSP(data.MaxSP) : 0;
					}
				}
			}
		}
		public override void OnHurt(Player.HurtInfo info)
		{
			UnderAttack = true;
			TryHurtCharge();
		}
		public void AutoCharge() {
			if (Player.GetModPlayer<Content.Items.Accessories.Developer.RecoverySuppressionPlayer>().Equipped)
				return;
			if (SceneCameraSkills.BlocksSkillCharge(Player)) // 稀音技能持续期间冻结充能
				return;
			// 自然回复类型 Div=60（SkillCharge 每 60 tick = 1 点可见技力）。倍率 x2 时每 tick
			// 累加 2 点 SkillCharge，SP 直接按 SkillCharge/Div 换算——和原来"每 60 tick 视觉 +1"
			// 的模运算在倍率为 1 时完全等价，但同时天然支持任意倍率，不会有跳格风险。
			int gain = GlobalSPRegenSpeedMultiplier;
			if (CurrentSkill != null) {
				SkillLevelData data = CurrentSkill.CurrentLevelData;
				if (!SkillActive && StockCount < data.MaxStack) {
					SkillCharge += gain;

					if (SkillCharge >= SkillChargeMax) {
						SkillCharge = 0;
						SP = ++StockCount == data.MaxStack ? GetEffectiveSkillSP(data.MaxSP) : 0;
					}
					else {
						SP = SkillCharge / Div;
					}
				}
			}
			else {
				// 旧版自动充能逻辑
				if (!SkillActive && StockCount < StockMax[Skill]) {
					SkillCharge += gain;

					if (SkillCharge >= SkillChargeMax) {
						SkillCharge = 0;
						StockCount += 1;
						SP = StockCount == StockMax[Skill] ? GetEffectiveSkillSP((int)MaxSP[Skill]) : 0;
					}
					else {
						SP = SkillCharge / Div;
					}
				}
			}
		}
		//============================================================================
		private double AccessoriesChargeFraction;
		public void AccessoriesAutoCharge() {
			if (Player.GetModPlayer<Content.Items.Accessories.Developer.RecoverySuppressionPlayer>().Equipped) {
				AccessoriesChargeFraction = 0d;
				return;
			}
			if (Skill < 0 || Skill >= SkillData.Length || Player.dead || SkillActive
				|| Div <= 0 || SkillChargeMax <= 0
				|| (CurrentSkill != null && (CurrentSkill.ChargeType != SkillChargeType.Auto
					|| CurrentSkill.UsesCustomCharge))) {
				AccessoriesChargeFraction = 0d;
				return;
			}

			int maxStock = CurrentSkill?.CurrentLevelData.MaxStack ?? StockMax[Skill];
			int maxSp = GetEffectiveSkillSP(CurrentSkill?.CurrentLevelData.MaxSP ?? MaxSP[Skill] ?? 0);
			double extraCharge = Math.Max(0d, (double)SPRegenMultiplier - 1d) * GlobalSPRegenSpeedMultiplier;
			if (maxStock <= 0 || StockCount >= maxStock || extraCharge <= 0d) {
				AccessoriesChargeFraction = 0d;
				return;
			}

			if (SceneCameraSkills.BlocksSkillCharge(Player) || (chargeOpen && !hasNearbyEnemy))
				return;

			// 每帧只重置回复倍率，小数余量保留到后续帧；Div=60 时，
			// 每帧多0.4充能单位即每秒多0.4点技力，不会在每帧ResetEffects中丢失。
			AccessoriesChargeFraction += extraCharge;
			// SPRegenMultiplier 是 float，容忍1.4f转为double后的微小误差。
			int gain = (int)Math.Floor(AccessoriesChargeFraction + 0.00001d);
			if (gain <= 0)
				return;

			AccessoriesChargeFraction = Math.Max(0d, AccessoriesChargeFraction - gain);
			long charge = (long)SkillCharge + gain;
			int gainedStock = (int)Math.Min(charge / SkillChargeMax, maxStock - StockCount);
			StockCount += gainedStock;
			SkillCharge = StockCount >= maxStock ? 0 : (int)(charge % SkillChargeMax);
			SP = StockCount >= maxStock ? maxSp : SkillCharge / Div;
			if (StockCount >= maxStock)
				AccessoriesChargeFraction = 0d;
		}
		//===============================================================================
		public void OffensiveRecovery() {
			if (SceneCameraSkills.BlocksSkillCharge(Player)) // 稀音技能持续期间冻结充能
				return;

			// 攻击回复类型同样 Div=1，理由同 HurtCharge：倍率大于 1 时必须用 >= 而不是 ==
			// 判满，否则 MaxSP 为奇数时会永远跳过精确等于 SkillChargeMax 的那一格，技能卡死攒不满。
			int gain = GlobalSPRegenSpeedMultiplier;

			if (CurrentSkill != null) {
				SkillLevelData data = CurrentSkill.CurrentLevelData;
				if (!SkillActive && StockCount < data.MaxStack) {
					SkillCharge += gain;
					SP += gain;
				}

				if (SkillCharge >= SkillChargeMax) {
					SkillCharge = 0;
					SP = ++StockCount == data.MaxStack ? GetEffectiveSkillSP(data.MaxSP) : 0;
				}
			}
			else {
				// 旧版攻击恢复逻辑
				if (!SkillActive && StockCount < StockMax[Skill]) {
					SkillCharge += gain;
					if (SkillCharge != 0)
						SP += gain;
				}

				if (SkillCharge >= SkillChargeMax) {
					SkillCharge = 0;
					StockCount += 1;
					SP = StockCount == StockMax[Skill] ? GetEffectiveSkillSP((int)MaxSP[Skill]) : 0;
				}
			}
		}

		public void UpdateActiveSkill() {
			if (Systems.Gameplay.MujicaFeverSystem.PauseSkill(Player)) return;
			if (SkillActive) {
				// 永久技能没有持续时间倒计时；其关闭/重置条件由技能自身处理。
				if (CurrentSkill?.IsPermanent == true)
					return;

				// 阈值本身乘倍率缩小（而不是让 SkillTimer 跳着加），并且统一用 >= 判断——
				// 倍率是 0.5 时阈值大概率不再是整数，用 == 精确相等会出现永远判不到的情况，
				// 参照之前修 SP 二倍速时踩过的同一个坑（见 HurtCharge 的注释）。
				if (CurrentSkill != null) {
					if (CurrentSkill.AutoUpdateActive
						&& ++SkillTimer >= CurrentSkill.CurrentLevelData.ActiveTime * 60 * ActiveDurationMultiplier)
						SkillActive = false;
				}
				else {
					SkillTimer++;
					if (SkillTimer >= SkillActiveTime[Skill] * 60 * ActiveDurationMultiplier)
						SkillActive = false;
				}
			}
		}
		public void UpdateActiveSkill2() {
			CurrentSkill.AutoUpdateActive = false;
		}

		public void StrikeSkill() {
			if (Systems.Gameplay.MujicaFeverSystem.PauseSkill(Player)) return;
			if (SkillActive) {
				SkillTimer++;
				if (SkillTimer >= 10 * ActiveDurationMultiplier)
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
			chargeOpen = true;
			// 消耗了一层库存即代表该技能已被实际使用：清除"初始就绪"标记，
			// 否则下方 ResetEffects 的 `else if (ark.chargeReady[Skill] && StockCount == 0)`
			// 会在技能结束后（单层技能 StockCount 立刻归零）把 InitSP 预灌进 SkillCharge，
			// 导致技力不从 0 而是从初始技力开始回复（复活后尤为明显）。
			if (Player.HeldItem.ModItem is UpgradeWeaponBase ark)
				ark.chargeReady[Skill] = false;
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
				SkillLevel = [7, 7, 7];
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
				ChargeTypeIsPerSecond = [false, true, true];
				AutoTrigger = [true, false, true];
				ShowSummonIconBySkills = [false, false, false];

				InitialSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
				InitialSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 25];
				InitialSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 20];
				MaxSPs1List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 40];
				MaxSPs2List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 35];
				MaxSPs3List = [0, 0, 0, 0, 0, 0, 0, 0, 0, 30];
				SkillActiveTimeS1List = [0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f];
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
				ShowSummonIconBySkills = [false, false, false];

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

			else if (HoldTyphonBow) {
				IconName = "TyphonBow";
				HowManySkills = 3;
				SkillLevel = new() { 10, 10, 10 };
				ChargeTypeIsPerSecond = new() { true, true, true };
				AutoTrigger = new() { false, false, false };
				ShowSummonIconBySkills = new() { false, false, false };
				InitialSPs1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 15 };
				InitialSPs2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 42 };
				InitialSPs3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 25 };
				MaxSPs1List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 35 };
				MaxSPs2List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 50 };
				MaxSPs3List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 40 };
				SkillActiveTimeS1List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f };
				SkillActiveTimeS2List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 20f };
				SkillActiveTimeS3List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 30f };
				StockMaxS1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
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

			else if (HoldHazeMagicBook) {
				IconName = "HazeMagicBook";
				HowManySkills = 2;
				SkillLevel = new() { 10, 10, 10 };
				ChargeTypeIsPerSecond = new() { true, true, true };
				AutoTrigger = new() { false, false, false };
				ShowSummonIconBySkills = new() { false, false, false };
				InitialSPs1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 15 };//夜烟这里的所有技能数据我都瞎填的
				InitialSPs2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 42 };
				InitialSPs3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 25 };
				MaxSPs1List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 35 };
				MaxSPs2List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 50 };
				MaxSPs3List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 40 };
				SkillActiveTimeS1List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f };
				SkillActiveTimeS2List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 20f };
				SkillActiveTimeS3List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 30f };
				StockMaxS1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				SetSkillData();
			}

			else if (HoldSurtrLaevatain) {
				IconName = "SurtrLaevatain";
				HowManySkills = 3;
				SkillLevel = new() { 10, 10, 10 };
				ChargeTypeIsPerSecond = new() { false, true, true };
				AutoTrigger = new() { true, false, false };
				ShowSummonIconBySkills = new() { false, false, false };
				InitialSPs1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 15 };//42这里的所有技能数据我都瞎填的
				InitialSPs2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 42 };
				InitialSPs3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 25 };
				MaxSPs1List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 35 };
				MaxSPs2List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 50 };
				MaxSPs3List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 40 };
				SkillActiveTimeS1List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f };
				SkillActiveTimeS2List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 20f };
				SkillActiveTimeS3List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 30f };
				StockMaxS1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				StockMaxS3List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
				SetSkillData();
			}

			else if (HoldJessicaGun) {
				IconName = "JessicaGun";
				HowManySkills = 2;
				SkillLevel = new() { 10, 10, 10 };
				ChargeTypeIsPerSecond = new() { false, true, false };
				AutoTrigger = new() { false, false, false };
				ShowSummonIconBySkills = new() { false, false, false };

				InitialSPs1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				InitialSPs2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				MaxSPs1List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				MaxSPs2List     = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				SkillActiveTimeS1List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
				SkillActiveTimeS2List = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
				StockMaxS1List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				StockMaxS2List = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				SetSkillData();
			}

			if (HowManySkills > 0) {
				SetSkill(Skill);
			}
		}
	}
}
