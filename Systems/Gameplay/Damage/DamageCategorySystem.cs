using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay.Damage
{
	/// <summary>
	/// 法术伤害/法抗系统。
	///
	/// 历史备注：这里原来是一套用 MonoMod 直接改写原版字节码（IL_NPC.GetIncomingStrikeModifiers /
	/// IL_NPC.HitModifiers.GetDamage 等）的实现，想让一次攻击同时按物理/法术/元素/真实四条轨道
	/// 分别结算。那套代码从来没有真正启用过（Load/Unload 整段被注释掉），审查后发现里面有个
	/// 把 DamageCategoryNPC 引用直接塞进 DamageClass 类型参数槽位、绕过类型检查的操作，
	/// 风险是整个战斗管线级别的崩溃，所以整段替换成了下面这套用 tModLoader 官方 Hook
	/// （ModifyHitByItem/ModifyHitByProjectile）实现的版本：只做"法术伤害吃目标法抗"这一件事，
	/// 不做多类型伤害同时结算，但足够覆盖当前的实际需求，而且完全在受支持的 API 范围内。
	/// </summary>
	public class DamageCategoryNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		/// <summary>法术抗性，0~1 的比例。命中来自 <see cref="ArtsWeaponRegistry"/> 的攻击时，
		/// 按 FinalDamage *= (1 - artsResistance) 直接扣减（和 WeaknessSystem 里的减益写法同一套公式）。</summary>
		public float artsResistance;

		public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) {
			if (artsResistance > 0f && ArtsWeaponRegistry.ArtsItemTypes.Contains(item.type))
				modifiers.FinalDamage *= (1f - artsResistance);
		}

		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) {
			if (artsResistance > 0f && ArtsWeaponRegistry.ArtsProjectileTypes.Contains(projectile.type))
				modifiers.FinalDamage *= (1f - artsResistance);
		}
	}

	/// <summary>
	/// 被判定为"法术伤害"的原版武器登记表。命中走物品挥砍/戳刺判定的放 ArtsItemTypes，
	/// 命中走独立弹幕（悠悠球、回旋镖等）的放 ArtsProjectileTypes——两边都要看攻击到底是
	/// 通过哪种判定命中的，只登记其中一边会导致武器实际打出去没有被识别成法伤。
	/// </summary>
	public static class ArtsWeaponRegistry
	{
		// 只有这 4 件在原版 Item.SetDefaults 里没设 noMelee=true——它们的挥砍本身就有伤害判定，
		// 除了下面 ArtsProjectileTypes 里各自对应的剑气/弹幕以外，物品本身的挥砍命中也要算。
		// （核对方式：反编译源码里逐个查了 case 段的 noMelee/shoot 字段，不是猜的。）
		public static readonly HashSet<int> ArtsItemTypes = new() {
			ItemID.InfluxWaver,    // 波涌之刃
			ItemID.LightsBane,     // 魔光剑
			ItemID.EnchantedSword, // 附魔剑
			ItemID.Frostbrand,     // 寒霜剑（霜印剑）
		};

		// 现代 Terraria 里大部分"剑挥出去"的伤害其实是这把剑自己生成的弹幕在打，不是物品自带的
		// 近战碰撞框（noMelee=true）。下面这些武器基本都属于这一类，弹幕名字经常和物品名不一样
		// （比如 附魔剑 打的是 EnchantedBeam，北极 打的是 NorthPoleWeapon），一样是查反编译源码核对的。
		public static readonly HashSet<int> ArtsProjectileTypes = new() {
			ProjectileID.IceSickle,       // 冰雪镰刀（回旋镖类）
			ProjectileID.Meowmere,        // 彩虹喵之刃（纯法术弹幕）
			ProjectileID.CorruptYoyo,     // 抑郁球
			ProjectileID.Cascade,         // 喷流球
			ProjectileID.HelFire,         // 狱火球
			ProjectileID.Kraken,          // 克拉肯球
			ProjectileID.MushroomSpear,   // 蘑菇长矛（noMelee，纯弹幕）
			ProjectileID.DarkLance,       // 暗黑长枪（noMelee，纯弹幕）
			ProjectileID.NorthPoleWeapon, // 北极（noMelee，纯弹幕）
			ProjectileID.TheHorsemansBlade,// 无头骑士剑（noMelee，纯弹幕）
			ProjectileID.TrueNightsEdge,  // 真永夜刃（noMelee，纯弹幕）
			ProjectileID.NightsEdge,      // 永夜刃（noMelee，纯弹幕）
			ProjectileID.CobaltNaginata,  // 钴薙刀（noMelee，纯弹幕）
			ProjectileID.MonkStaffT2,     // 恐怖关刀（noMelee，纯弹幕）
			ProjectileID.LightsBane,      // 魔光剑的剑气（挥砍本身也算，见上）
			ProjectileID.InfluxWaver,     // 波涌之刃的光刃（挥砍本身也算，见上）
			ProjectileID.EnchantedBeam,   // 附魔剑的剑气（挥砍本身也算，见上）
			ProjectileID.FrostBoltSword,  // 寒霜剑的冰刃（挥砍本身也算，见上）
		};
	}

	/// <summary>
	/// 按地区给原版怪物赋默认法抗：邪恶群系（腐化/猩红）20，困难模式额外 +20；
	/// 其余地区（森林等）默认 0。只在生成那一刻取一次附近玩家所在地区，不做逐帧跟随——
	/// 这是"这只怪物是在什么环境里生成的"的一次性快照，不是"这只怪物当前站在哪"。
	///
	/// 只处理原版怪物（npc.ModNPC == null）。本模组自己的干员化敌人已经在各自 SetDefaults
	/// 里手动设过法抗（参见 Caster.cs/Hound.cs/Evolution.cs 等 ~20 个文件），这里不会碰它们。
	/// </summary>
	public class ArtsResistanceZoneDefaults : GlobalNPC
	{
		private const float EvilBiomeResistance = 0.20f;
		private const float EvilBiomeResistanceHardmode = 0.40f;

		// 显式指定的原版怪物，优先于地区默认值。
		private static readonly Dictionary<int, float> ExplicitOverrides = new() {
			{ NPCID.KingSlime, 0.30f },
			{ NPCID.PossessedArmor, 0.40f },
		};

		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (npc.ModNPC != null)
				return;

			var cat = npc.GetGlobalNPC<DamageCategoryNPC>();

			if (ExplicitOverrides.TryGetValue(npc.type, out float explicitValue)) {
				cat.artsResistance = explicitValue;
				return;
			}

			cat.artsResistance = GetZoneDefault(npc.Center);
		}

		private static float GetZoneDefault(Vector2 position) {
			Player nearest = FindNearestPlayer(position);
			if (nearest == null)
				return 0f;

			if (nearest.ZoneCorrupt || nearest.ZoneCrimson)
				return Main.hardMode ? EvilBiomeResistanceHardmode : EvilBiomeResistance;

			return 0f;
		}

		private static Player FindNearestPlayer(Vector2 position) {
			Player nearest = null;
			float nearestDistSq = float.MaxValue;
			foreach (Player player in Main.ActivePlayers) {
				float distSq = Vector2.DistanceSquared(player.Center, position);
				if (distSq < nearestDistSq) {
					nearestDistSq = distSq;
					nearest = player;
				}
			}
			return nearest;
		}
	}
}
