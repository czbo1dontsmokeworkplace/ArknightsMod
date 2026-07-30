using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Reforge
{
	// NeoArmor Reforge 的注册中枢：保管"时装 ↔ 套装件"之间的各种映射表。
	//
	// ⚠ 注册入口不在这个类的 Load() 里，而是由每件时装在自己的
	// ReforgeVanityItem.Load() 中调用 RegisterSetPieceFor——原因见那边的注释
	// （tModLoader 按类型全名字母序自动加载，在这里统一扫描会静默漏掉字母序排在
	// "ReforgeSetLoader" 之后的所有干员）。这个类只负责存表，以及在
	// PostSetupContent 阶段（所有内容注册完、ItemType 都已分配）补齐那些需要用到
	// 时装 ItemType 的表。
	//
	// "同一套"的判定：同一命名空间下的 Head/Body/Legs 三件视为一套。每个干员一个
	// 文件夹/命名空间本来就是本项目既有的约定，所以不需要额外声明归属关系。
	internal sealed class ReforgeSetLoader : ModSystem
	{
		private static readonly List<(ReforgeVanityItem vanity, ReforgeSetPiece piece)> Registered = new();

		private static readonly Dictionary<int, ReforgeSetPiece> PieceByVanityType = new();
		private static readonly Dictionary<string, Family> FamilyByNamespace = new();

		// 按套装件自己的 ItemType 反查 Vanity/SlotType，供 ReforgeSetPiece 的懒加载
		// 属性使用。这两张表必须在套装件注册的那一刻就填好（不能等 PostSetupContent），
		// 因为 SetDefaults 可能在那之前就被调用（ContentSamples 初始化等）。
		private static readonly Dictionary<int, ReforgeVanityItem> VanityByPieceType = new();
		private static readonly Dictionary<int, EquipType> SlotTypeByPieceType = new();

		private sealed class Family
		{
			public int BodyVanityType = -1;
			public int LegsVanityType = -1;
		}

		// 由 ReforgeVanityItem.Load() 调用。注意此时调用方（时装）自己的 Type 还没分配
		// （ModType 是先 Load() 再 Register()），所以凡是需要时装 ItemType 的表都只能
		// 延后到 PostSetupContent 再填。
		internal static void RegisterSetPieceFor(ReforgeVanityItem vanity) {
			EquipType slotType = vanity switch {
				ReforgeVanityHead => EquipType.Head,
				ReforgeVanityBody => EquipType.Body,
				ReforgeVanityLegs => EquipType.Legs,
				_ => throw new InvalidOperationException(
					$"{vanity.GetType().FullName} 声明了 SetProfile，但不是 ReforgeVanityHead/Body/Legs 的子类——" +
					"必须是这三者之一，框架才知道套装件该注册到哪个装备类型下。"),
			};

			var piece = new ReforgeSetPiece();
			piece.Init(vanity, slotType);
			vanity.Mod.AddContent(piece);

			// AddContent 返回时 piece.Register() 已经执行过，piece.Type 此刻有效。
			VanityByPieceType[piece.Type] = vanity;
			SlotTypeByPieceType[piece.Type] = slotType;
			Registered.Add((vanity, piece));
		}

		public override void PostSetupContent() {
			foreach ((ReforgeVanityItem vanity, ReforgeSetPiece piece) in Registered) {
				PieceByVanityType[vanity.Type] = piece;

				string ns = vanity.GetType().Namespace;
				if (!FamilyByNamespace.TryGetValue(ns, out Family family))
					FamilyByNamespace[ns] = family = new Family();

				if (vanity is ReforgeVanityBody)
					family.BodyVanityType = vanity.Type;
				else if (vanity is ReforgeVanityLegs)
					family.LegsVanityType = vanity.Type;
			}
		}

		public override void Unload() {
			Registered.Clear();
			PieceByVanityType.Clear();
			FamilyByNamespace.Clear();
			VanityByPieceType.Clear();
			SlotTypeByPieceType.Clear();
			ReforgeAppearance.Unload();
		}

		internal static ReforgeVanityItem GetVanity(int pieceType) =>
			VanityByPieceType.TryGetValue(pieceType, out ReforgeVanityItem vanity) ? vanity : null;

		internal static EquipType GetSlotType(int pieceType) =>
			SlotTypeByPieceType.TryGetValue(pieceType, out EquipType type) ? type : default;

		/// <summary>某件头部套装是否已经三件（头/身/腿）都穿在盔甲栏上了。</summary>
		internal static bool IsFullSetEquipped(Player player, ReforgeVanityItem headVanity) {
			string ns = headVanity.GetType().Namespace;
			if (!FamilyByNamespace.TryGetValue(ns, out Family family))
				return false;
			if (family.BodyVanityType < 0 || family.LegsVanityType < 0)
				return false;
			if (!PieceByVanityType.TryGetValue(family.BodyVanityType, out ReforgeSetPiece bodyPiece))
				return false;
			if (!PieceByVanityType.TryGetValue(family.LegsVanityType, out ReforgeSetPiece legsPiece))
				return false;

			return player.armor[1].type == bodyPiece.Type && player.armor[2].type == legsPiece.Type;
		}

		/// <summary>
		/// 取某件时装对应的套装件 ItemType。干员自己的 ModPlayer 里判断"是否穿了套装"
		/// 时用这个，不需要记住自动生成的套装类名，直接传时装类型即可：
		/// <c>player.armor[0].type == ReforgeSetLoader.GetSetType&lt;MudrockHead&gt;()</c>
		/// </summary>
		public static int GetSetType<TVanity>() where TVanity : ReforgeVanityItem {
			int vanityType = ModContent.ItemType<TVanity>();
			return PieceByVanityType.TryGetValue(vanityType, out ReforgeSetPiece piece) ? piece.Type : -1;
		}
	}
}
