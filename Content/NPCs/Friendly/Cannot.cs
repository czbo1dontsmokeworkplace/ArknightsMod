using ArknightsMod.Content.Items;
using ArknightsMod.Content.Players;
using ArknightsMod.Content.NPCs.Enemy.Chapter6;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.NPCs.Enemy.TillChapter7;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Friendly
{
	/// <summary>坎诺特的两个阶段：友善（商人）/ 敌对（按波次召唤增援）。</summary>
	public enum CannotPhase : byte
	{
		Friendly = 0,
		Hostile = 1,
	}

	/// <summary>玩家在对话框里能对坎诺特做的三件事（客户端 → 服务器）。</summary>
	public enum CannotAction : byte
	{
		/// <summary>「试探性碰碰商品」：触碰次数 +1。</summary>
		Touch = 0,
		/// <summary>绿色「听坎诺特的」：触碰进度清零，恢复初始按钮。</summary>
		ChooseListen = 1,
		/// <summary>红色「“请”坎诺特“降价”」：进入敌对阶段，开始按波次召唤怪物。</summary>
		ChooseHaggle = 2,
	}

	// 坎诺特分两个阶段：
	//
	// ● 友善阶段（默认）：一个普通的商人。受到攻击不会召唤怪物；被打死也不掉任何东西；
	//   在世界里停留满一整个昼夜（Main.dayLength + Main.nightLength）后自己收摊离开（同样不掉落）。
	//   对话框里的「试探性碰碰商品」连按三次后，两个按钮变色发光：
	//     绿色「听坎诺特的」→ 触碰进度清零，恢复初始按钮，并照常打开商店；
	//     红色「“请”坎诺特“降价”」→ 进入敌对阶段。
	//
	// ● 敌对阶段：坎诺特站定不动，按 CannotWaves 里的阵容分五波，每一波通过"传送门"
	//   （CannotPortal，出现在目标玩家屏幕范围内）放出怪物。一波里的怪物全部消灭后进入
	//   波间空档，空档结束刷下一波。第五波打完，坎诺特逃走并掉落一件当前在售的藏品。
	//   怪物没打完期间坎诺特无敌；只有两波之间的空档才能被打，
	//   在空档里把他打死，只掉 5~8 个源石锭，战斗直接结束。
	//
	// 联机：阶段/触碰进度/波数/是否可被攻击由服务器权威，通过 SendExtraAI 同步给客户端；
	// 客户端在对话框里点按钮，是发一个 CannotInteract 包给服务器，由服务器改状态。
	[AutoloadHead]
	public class Cannot : ModNPC
	{
		private const float MaxCannotInteractionDistancePixels = 20f * 16f;

		/// <summary>触碰满这么多次后，按钮变成「听坎诺特的 / 请坎诺特降价」二选一。</summary>
		public const int TouchThreshold = 3;

		public const int WaveCount = CannotWaves.WaveCount;

		// 友善阶段停留时间：正好一个完整的游戏昼夜。
		private const int StayTicksMax = (int)(Main.dayLength + Main.nightLength);

		private const int BattleStartDelayTicks = 3 * 60;   // 选了红色按钮后，到第一波出现之前的准备时间
		private const int WaveIntermissionTicks = 5 * 60;   // 一波打完到下一波出现之间的空档（此时坎诺特可被攻击）
		private const int PortalStaggerTicks = 40;          // 同一波里，两个传送门之间的间隔
		private const int AbandonTicks = 30 * 60;           // 附近一直没有可用的目标玩家超过这么久，就放弃战斗离开
		private const float BattleTargetMaxDistancePixels = 120f * 16f;

		public const int RespawnCooldownTicks = 7200;

		// 传送门出现的位置：目标玩家左右这个格数范围内、上下这个格数范围内——都在一般屏幕里
		// （缩放到 2 倍时半屏宽约 30 格，所以横向最远只取 24 格）。
		private const int PortalMinDistanceTiles = 12;
		private const int PortalMaxDistanceTiles = 24;
		private const int PortalVerticalRangeTiles = 12;

		public static readonly Color ListenColor = new(90, 255, 120);
		public static readonly Color HaggleColor = new(255, 80, 80);

		// 修改：保存完整的 Item 对象而不是只保存 type
		public readonly static List<Item> shopItems = [];

		// A static instance of the declarative shop, defining all the items which can be brought. Used to create a new inventory when the NPC spawns
		public static CannotShop Shop;

		public const string ShopName = "Shop";

		// ── 需要同步给客户端的状态（见 SendExtraAI）──
		public CannotPhase Phase;
		public int TouchCount;
		public int WaveIndex;          // 已经开始的波数，0 = 还没开始
		public bool BattleVulnerable;  // 是否处于两波之间的空档，只有此时敌对阶段的坎诺特才能被攻击
		public bool Runaway;

		// ── 只在服务器/单人侧使用的战斗状态 ──
		private int stayTimer;
		private int battleTarget = -1;
		private int battleTimer;
		private bool waveInProgress;
		private int portalTimer;
		private int abandonTimer;
		private Queue<int> spawnQueue;
		private CannotWaveStage battleStage;

		public static bool Isnpcexist {
			get {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC SeekForNPCs = Main.npc[i];
					if (SeekForNPCs.active && Array.Exists(Eliteslist, x => x == SeekForNPCs.type)) {
						return true;
					}
				}
				return false;
			}
		}

		static int[] Eliteslist => [
			ModContent.NPCType<ShieldGuard>(),
			ModContent.NPCType<IceCleaver>(),
			ModContent.NPCType<Seniorcaster>(),
			ModContent.NPCType<InsaneZombieL>(),
			ModContent.NPCType<Oneiros>()
			];

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 23;
			NPCID.Sets.ExtraFramesCount[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.AttackFrameCount[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.DangerDetectRange[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.ActsLikeTownNPC[Type] = true;
			NPCID.Sets.AttackType[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.AttackTime[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.AttackAverageChance[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.HatOffsetY[Type] = NPCID.Sets.ExtraFramesCount[NPCID.OldMan];
			NPCID.Sets.NoTownNPCHappiness[Type] = true;
		}

		public override List<string> SetNPCNameList() {
			return [];
		}

		public override void SetDefaults() {
			NPC.townNPC = true;
			NPC.friendly = false;
			NPC.dontTakeDamage = false;
			NPC.chaseable = false;
			NPC.dontTakeDamageFromHostiles = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = NPCAIStyleID.Passive;
			NPC.damage = 0;
			NPC.defense = 99;
			NPC.lifeMax = 1000;
			NPC.npcSlots = 7f;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.rarity = 1;
			AnimationType = NPCID.OldMan;

			// 引用类型字段放在这里初始化，保证每个坎诺特实例各有一份（ModNPC 是按实例克隆出来的）。
			spawnQueue = new Queue<int>();
		}

		// ═══════════════════════ 受击 / 可被攻击 ═══════════════════════
		// 友善阶段受到攻击不会召唤任何怪物（以前会），所以这里不再有 OnHitByItem/OnHitByProjectile。

		// 友善阶段：沿用原来的规则——世界里有精英怪时打不了他；
		// 敌对阶段：只有两波之间的空档才能打（怪没打完之前他无敌）。
		private bool CanBeHitNow() =>
			Phase == CannotPhase.Hostile ? BattleVulnerable : !Isnpcexist;

		public override bool? CanBeHitByItem(Player player, Item item) {
			if (!CanBeHitNow())
				return false;
			if (!player.GetModPlayer<CannotAggroPlayer>().CanDamageCannotForCurrentLife())
				return false;
			return base.CanBeHitByItem(player, item);
		}

		public override bool? CanBeHitByProjectile(Projectile projectile) {
			if (!CanBeHitNow())
				return false;
			if (!projectile.TryGetOwner(out Player owner) || !owner.GetModPlayer<CannotAggroPlayer>().CanDamageCannotForCurrentLife())
				return false;
			return base.CanBeHitByProjectile(projectile);
		}

		public override bool CanBeHitByNPC(NPC attacker) {
			return false;
		}

		// ═══════════════════════ 对话 ═══════════════════════

		public override bool CanChat() => Phase == CannotPhase.Friendly;

		public override string GetChat() {
			// 已经触碰满三次、按钮已经变色时，再次打开对话框还是停留在那句话上，和按钮保持一致。
			if (TouchCount >= TouchThreshold)
				return Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch3");

			int rand = Main.rand.Next(1, 9);
			return Language.GetTextValue($"Mods.ArknightsMod.Dialogue.Cannot.Dialogue{rand}");
		}

		public override void SetChatButtons(ref string button, ref string button2) {
			if (TouchCount >= TouchThreshold) {
				// 按钮文字里直接带聊天颜色标签，原版画对话按钮用的是 ChatManager，会正确解析；
				// 发光效果见 CannotChatButtonGlowSystem。
				button = ColorTag(this.GetLocalizedValue("Buttons.Listen"), ListenColor);
				button2 = ColorTag(this.GetLocalizedValue("Buttons.Haggle"), HaggleColor);
				return;
			}

			button = this.GetLocalizedValue("Buttons.Shop");
			button2 = this.GetLocalizedValue("Buttons.Touch");
		}

		private static string ColorTag(string text, Color color) => $"[c/{color.R:X2}{color.G:X2}{color.B:X2}:{text}]";

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			bool ultimatum = TouchCount >= TouchThreshold;

			if (firstButton) {
				// 商店按钮；如果此时它是绿色的「听坎诺特的」，先把触碰进度清零，再照常开店。
				if (ultimatum)
					SendAction(CannotAction.ChooseListen);
				shop = ShopName;
				return;
			}

			if (!ultimatum) {
				// 「试探性碰碰商品」。第一次按下也是玩家获得"可以攻击坎诺特"资格的时刻（见 CannotAggroPlayer）。
				SendAction(CannotAction.Touch);
				Main.LocalPlayer.GetModPlayer<CannotAggroPlayer>().AcknowledgeCannotTouchGoodsDialogue();
				Main.npcChatText = Language.GetTextValue($"Mods.ArknightsMod.Dialogue.Cannot.Touch{Math.Clamp(TouchCount, 1, TouchThreshold)}");
				return;
			}

			// 红色「“请”坎诺特“降价”」：开战，关掉对话框。
			SendAction(CannotAction.ChooseHaggle);
			Main.CloseNPCChatOrSign();
		}

		// ═══════════════════════ 联机：客户端 → 服务器 ═══════════════════════

		// 单人/主机直接生效；多人客户端发包给服务器，并先在本地做一次"预测"，
		// 让对话框里的按钮/文字立刻有反应（服务器同步回来的值会覆盖它）。
		private void SendAction(CannotAction action) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Mod.GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.CannotInteract);
				packet.Write(NPC.whoAmI);
				packet.Write((byte)action);
				packet.Send();

				switch (action) {
					case CannotAction.Touch:
						if (TouchCount < TouchThreshold)
							TouchCount++;
						break;
					case CannotAction.ChooseListen:
						TouchCount = 0;
						break;
				}
				return;
			}

			ApplyAction(action, Main.myPlayer);
		}

		public static void ReadInteract(BinaryReader reader, int senderWhoAmI) {
			int npcIndex = reader.ReadInt32();
			byte actionByte = reader.ReadByte();

			if (Main.netMode != NetmodeID.Server || (uint)senderWhoAmI >= Main.maxPlayers)
				return;
			if ((uint)npcIndex >= Main.maxNPCs || actionByte > (byte)CannotAction.ChooseHaggle)
				return;

			Player player = Main.player[senderWhoAmI];
			NPC npc = Main.npc[npcIndex];
			if (!player.active || player.dead || !npc.active || npc.ModNPC is not Cannot cannot)
				return;
			if (Vector2.DistanceSquared(player.Center, npc.Center) > MaxCannotInteractionDistancePixels * MaxCannotInteractionDistancePixels)
				return;

			cannot.ApplyAction((CannotAction)actionByte, senderWhoAmI);
		}

		// 只在服务器/单人侧调用：真正修改状态。
		private void ApplyAction(CannotAction action, int playerIndex) {
			if (Phase != CannotPhase.Friendly)
				return;

			switch (action) {
				case CannotAction.Touch:
					if (TouchCount < TouchThreshold)
						TouchCount++;
					break;
				case CannotAction.ChooseListen:
					if (TouchCount >= TouchThreshold)
						TouchCount = 0;
					break;
				case CannotAction.ChooseHaggle:
					if (TouchCount >= TouchThreshold)
						StartBattle(playerIndex);
					break;
			}

			NPC.netUpdate = true;
		}

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write((byte)Phase);
			writer.Write((byte)TouchCount);
			writer.Write((byte)WaveIndex);
			writer.Write(BattleVulnerable);
			writer.Write(Runaway);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			Phase = (CannotPhase)reader.ReadByte();
			TouchCount = reader.ReadByte();
			WaveIndex = reader.ReadByte();
			BattleVulnerable = reader.ReadBoolean();
			Runaway = reader.ReadBoolean();
		}

		// ═══════════════════════ AI ═══════════════════════

		public override void AI() {
			if (Phase == CannotPhase.Hostile) {
				HoldGround();
				// 敌对阶段不能再聊天了：正在和他对话的本机玩家把对话框关掉。
				if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.talkNPC == NPC.whoAmI)
					Main.CloseNPCChatOrSign();
			}

			// 状态推进只在服务器/单人侧做，多人客户端只负责显示。
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (Phase == CannotPhase.Friendly)
				UpdateFriendly();
			else
				UpdateBattle();
		}

		// 敌对阶段站定不动、面向最近的玩家。原版城镇 NPC 的 AI 仍然在跑（重力、动画都靠它），
		// 这里只是每帧把水平速度压成 0，防止他到处乱跑或者躲回房子里。
		private void HoldGround() {
			NPC.velocity.X = 0f;

			int closest = Player.FindClosest(NPC.position, NPC.width, NPC.height);
			if ((uint)closest < Main.maxPlayers && Main.player[closest].active) {
				int dir = Main.player[closest].Center.X >= NPC.Center.X ? 1 : -1;
				NPC.direction = dir;
				NPC.spriteDirection = dir;
			}
		}

		private void UpdateFriendly() {
			// 在世界里停留满一整个昼夜就自己收摊离开。有人正在跟他对话的话等对话结束再走，
			// 免得商店开着开着人没了。
			if (++stayTimer >= StayTicksMax && !IsAnyPlayerTalking())
				Leave();
		}

		private bool IsAnyPlayerTalking() {
			foreach (Player player in Main.ActivePlayers) {
				if (player.talkNPC == NPC.whoAmI)
					return true;
			}
			return false;
		}

		/// <summary>自己走人：不算被击杀，不掉任何东西；同样会进入一段重新刷新的冷却。</summary>
		private void Leave() {
			Broadcast("Mods.ArknightsMod.NPCs.Cannot.Leave", Color.LightBlue);
			RespawnCooldown = RespawnCooldownTicks;
			Despawn();
		}

		public void Despawn() {
			NPC.active = false;
			if (Main.netMode == NetmodeID.Server) {
				NPC.netSkip = -1;
				NPC.life = 0;
				NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
			}
		}

		// ═══════════════════════ 战斗（敌对阶段）═══════════════════════

		private void StartBattle(int playerIndex) {
			Phase = CannotPhase.Hostile;
			TouchCount = 0;
			WaveIndex = 0;
			BattleVulnerable = false;

			battleTarget = playerIndex;
			battleTimer = BattleStartDelayTicks;
			waveInProgress = false;
			abandonTimer = 0;
			spawnQueue.Clear();
			battleStage = CannotWaves.GetCurrentStage();

			Broadcast("Mods.ArknightsMod.NPCs.Cannot.Battle.Begin", Color.OrangeRed);
			NPC.netUpdate = true;
		}

		private void UpdateBattle() {
			if (!TryGetBattleTarget(out Player target)) {
				if (++abandonTimer >= AbandonTicks)
					AbandonBattle();
				return;
			}
			abandonTimer = 0;

			// 准备/波间空档：倒计时到 0 开始下一波
			if (!waveInProgress) {
				if (--battleTimer <= 0)
					BeginWave();
				return;
			}

			// 这一波的怪物排着队一个一个从传送门里出来
			if (spawnQueue.Count > 0) {
				if (--portalTimer <= 0) {
					SpawnPortal(spawnQueue.Dequeue(), target);
					portalTimer = PortalStaggerTicks;
				}
				return;
			}

			// 全部放出来之后，等场上的怪（以及还没放完怪的传送门）都清干净
			if (CountWaveThreats() > 0)
				return;

			OnWaveCleared();
		}

		private void BeginWave() {
			WaveIndex++;
			BattleVulnerable = false;
			waveInProgress = true;
			portalTimer = 0;

			foreach (int type in battleStage.GetWave(WaveIndex))
				spawnQueue.Enqueue(type);

			Broadcast("Mods.ArknightsMod.NPCs.Cannot.Wave.Incoming", Color.OrangeRed, WaveIndex, WaveCount);
			NPC.netUpdate = true;
		}

		private void OnWaveCleared() {
			waveInProgress = false;
			Broadcast("Mods.ArknightsMod.NPCs.Cannot.Wave.Cleared", Color.LightGreen, WaveIndex, WaveCount);

			if (WaveIndex >= WaveCount) {
				// 五波全部打完：坎诺特认栽，带着一件藏品逃走。
				DoRunaway();
				return;
			}

			BattleVulnerable = true; // 空档里可以打他，但打死了就拿不到通关奖励
			battleTimer = WaveIntermissionTicks;
			NPC.netUpdate = true;
		}

		// 还需要等待的东西：场上还活着的本场召唤怪 + 还没放完怪的传送门。
		private int CountWaveThreats() {
			int count = CannotSummonedTag.CountAlive(NPC.whoAmI);
			int portalType = ModContent.ProjectileType<CannotPortal>();
			foreach (Projectile projectile in Main.ActiveProjectiles) {
				if (projectile.type == portalType && (int)projectile.ai[1] == NPC.whoAmI)
					count++;
			}
			return count;
		}

		// 优先用一开始点了红色按钮的那位玩家；他死了/走远了/断线了就换成最近的存活玩家。
		private bool TryGetBattleTarget(out Player target) {
			target = null;

			if ((uint)battleTarget < Main.maxPlayers && IsValidBattleTarget(Main.player[battleTarget])) {
				target = Main.player[battleTarget];
				return true;
			}

			float bestDistSq = BattleTargetMaxDistancePixels * BattleTargetMaxDistancePixels;
			foreach (Player player in Main.ActivePlayers) {
				if (!IsValidBattleTarget(player))
					continue;

				float distSq = Vector2.DistanceSquared(player.Center, NPC.Center);
				if (distSq < bestDistSq) {
					bestDistSq = distSq;
					target = player;
					battleTarget = player.whoAmI;
				}
			}

			return target != null;
		}

		private bool IsValidBattleTarget(Player player) =>
			player.active && !player.dead
			&& Vector2.DistanceSquared(player.Center, NPC.Center) <= BattleTargetMaxDistancePixels * BattleTargetMaxDistancePixels;

		// 附近很久没有活着的玩家了：坎诺特把还没打完的增援一起撤走，自己也离开，不掉东西。
		private void AbandonBattle() {
			ReleaseSummoned(despawn: true);
			Leave();
		}

		private void SpawnPortal(int npcType, Player target) {
			Vector2 bottom = FindPortalBottom(target);
			Vector2 center = bottom - new Vector2(0f, CannotPortal.PortalHeight / 2f);

			Projectile.NewProjectile(NPC.GetSource_FromThis(), center, Vector2.Zero,
				ModContent.ProjectileType<CannotPortal>(), 0, 0f, Main.myPlayer,
				ai0: npcType, ai1: NPC.whoAmI, ai2: target.whoAmI);
		}

		// 在目标玩家屏幕范围内找一个"脚下是实心方块、头上有足够净空、没有岩浆"的落脚点。
		// 之前是刷在屏幕外面，怪物是从看不见的地方冒出来的；现在必须在玩家看得到的地方。
		private static Vector2 FindPortalBottom(Player target) {
			const int footprintWidth = 4;
			const int footprintHeight = 7;

			int playerTileX = (int)(target.Center.X / 16f);
			int playerTileY = (int)(target.Center.Y / 16f);

			for (int attempt = 0; attempt < 60; attempt++) {
				int side = Main.rand.NextBool() ? -1 : 1;
				int tileX = playerTileX + side * Main.rand.Next(PortalMinDistanceTiles, PortalMaxDistanceTiles + 1);

				for (int tileY = playerTileY - PortalVerticalRangeTiles; tileY <= playerTileY + PortalVerticalRangeTiles; tileY++) {
					if (!WorldGen.InWorld(tileX, tileY, 20))
						break;
					if (!IsSolidGround(tileX, tileY))
						continue;
					if (!HasClearance(tileX, tileY - 1, footprintWidth, footprintHeight))
						continue;

					return new Vector2(tileX * 16f + 8f, tileY * 16f);
				}
			}

			// 实在找不到合适的落脚点（比如玩家在很窄的洞里）：退而求其次，贴着玩家旁边刷，
			// 也要保证不刷进方块里。
			foreach (int side in new[] { -1, 1 }) {
				Vector2 spot = new(target.Center.X + side * 6f * 16f, target.Bottom.Y);
				if (!Collision.SolidCollision(spot - new Vector2(16f, 48f), 32, 48))
					return spot;
			}
			return target.Bottom;
		}

		private static bool IsSolidGround(int x, int y) {
			Tile tile = Main.tile[x, y];
			return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
		}

		private static bool HasClearance(int centerX, int bottomTileY, int width, int height) {
			int left = centerX - width / 2;
			for (int x = left; x < left + width; x++) {
				for (int y = bottomTileY - height + 1; y <= bottomTileY; y++) {
					if (!WorldGen.InWorld(x, y, 10))
						return false;

					Tile tile = Main.tile[x, y];
					if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
						return false;
					if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava)
						return false;
				}
			}
			return true;
		}

		// 战斗结束（成功/放弃/坎诺特被打死）后处理这场战斗召唤出来的怪和传送门：
		// despawn=true 直接撤走；false 只是取消"归属"标记，避免坎诺特死后
		// 这些怪的标记下标被之后新刷出来的坎诺特误认成自己的。
		private void ReleaseSummoned(bool despawn) {
			foreach (NPC npc in Main.ActiveNPCs) {
				CannotSummonedTag tag = npc.GetGlobalNPC<CannotSummonedTag>();
				if (tag.OwnerCannot != NPC.whoAmI)
					continue;

				tag.OwnerCannot = -1;
				if (despawn) {
					npc.active = false;
					if (Main.netMode == NetmodeID.Server) {
						npc.netSkip = -1;
						npc.life = 0;
						NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
					}
				}
			}

			int portalType = ModContent.ProjectileType<CannotPortal>();
			foreach (Projectile projectile in Main.ActiveProjectiles) {
				if (projectile.type == portalType && (int)projectile.ai[1] == NPC.whoAmI)
					projectile.Kill();
			}
		}

		public void DoRunaway() {
			Runaway = true;
			NPC.netUpdate = true;
			var hit = new NPC.HitInfo() { InstantKill = true };
			NPC.StrikeNPC(hit);
			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendStrikeNPC(NPC, hit);
		}

		// 单人下直接 Main.NewText；服务器广播给所有玩家（客户端不会走到这里）。
		private static void Broadcast(string key, Color color, params object[] args) {
			if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key, args), color);
			else if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Language.GetTextValue(key, args), color);
		}

		// ═══════════════════════ 死亡 / 掉落 ═══════════════════════

		public override LocalizedText DeathMessage => Language.GetText("Mods.ArknightsMod.NPCs.Cannot.DeathMessage.Runaway");

		public override bool ModifyDeathMessage(ref NetworkText customText, ref Color color) {
			if (Runaway) {
				color = Color.LightBlue;
				return base.ModifyDeathMessage(ref customText, ref color);
			}
			return false;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			// 敌对阶段被玩家击杀（非逃走）：掉落更多源石锭（5~8）。
			// 友善阶段被打死不掉任何东西（CannotDead 条件里判断了阶段）。
			npcLoot.Add(ItemDropRule.ByCondition(new CannotDead(), ModContent.ItemType<OriginiumIngot>(), 1, 5, 8));
			// 逃走掉落的“当前售卖藏品”改为在 OnKill 中确定性掉落 —— 逃走是自伤 InstantKill，
			// 常规掉落规则(NPCLoot)在该路径下不一定会解析，导致藏品掉不出来。
		}

		public override void AddShops() {
			Shop = new CannotShop();
			Shop.Register();
		}

		public override void OnSpawn(IEntitySource source) {
			stayTimer = 0;
			NPCShopSystem.TryUpdateCannotShop(Mod, true);
		}

		public override void ModifyActiveShop(string shopName, Item[] items) {
			NPCShopSystem.TryUpdateCannotShop(Mod);
			Array.Fill(items, null);
			// 直接克隆完整的 Item 对象,保留 shopSpecialCurrency 等所有属性
			Item[] shopItems = [.. NPCShopSystem.CannotShopItems.Select(i => i.Clone())];
			for (int i = 0; i < items.Length && i < shopItems.Length; i++) {
				items[i] = shopItems[i]?.Clone();
			}
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			damage = 30;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 30;
			randExtraCooldown = 30;
		}

		public static int RespawnCooldown {
			get => CannotSpawnHelper.RespawnCooldown;
			set => CannotSpawnHelper.RespawnCooldown = value;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			foreach (var npc in Main.ActiveNPCs) {
				if (npc.type == Type)
					return base.SpawnChance(spawnInfo);
			}
			// 死亡后的重生冷却：RespawnCooldown 在 CheckDead() 中被设为 7200，
			// 此前一直只在 CannotSpawnHelper 里递减，但没有任何地方真正用它阻止重新生成，
			// 导致坎诺特死亡后下一刻就可能被重新刷出来。
			if (RespawnCooldown > 0)
				return 0f;
			if (!spawnInfo.Invasion && !spawnInfo.Sky && (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3))
				return 0.2f;
			return base.SpawnChance(spawnInfo);
		}

		public override bool CheckDead() {
			RespawnCooldown = RespawnCooldownTicks;
			ModContent.GetInstance<CannotLifeGateSystem>().OnCannotDied();
			return base.CheckDead();
		}

		public override void OnKill() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ReleaseSummoned(despawn: false);

			// 逃走时确定性掉落一件“当前正在售卖的藏品”（服务器/单机权威，Item.NewItem 自动同步）
			if (!Runaway || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			var shopItems = NPCShopSystem.CannotShopItems;
			if (shopItems.Count == 0) {
				// 兜底：若尚未生成商店内容，强制刷新一次
				NPCShopSystem.TryUpdateCannotShop(Mod, true);
				shopItems = NPCShopSystem.CannotShopItems;
			}
			if (shopItems.Count > 0) {
				int type = shopItems[Main.rand.Next(shopItems.Count)].type;
				Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), type, 1);
			}
		}
	}

	public class CannotShop() : AbstractNPCShop(ModContent.NPCType<Cannot>())
	{
		public new record Entry(Item Item, List<Condition> Conditions) : AbstractNPCShop.Entry
		{
			IEnumerable<Condition> AbstractNPCShop.Entry.Conditions => Conditions;

			public bool Disabled { get; private set; }

			public Entry Disable() {
				Disabled = true;
				return this;
			}

			public bool ConditionsMet() => Conditions.All(c => c.IsMet());
		}

		public record Pool(string Name, int Slots, List<Entry> Entries)
		{
			public Pool Add(Item item, params Condition[] conditions) {
				Entries.Add(new Entry(item, conditions.ToList()));
				return this;
			}

			public Pool Add<T>(params Condition[] conditions) where T : ModItem => Add(ModContent.ItemType<T>(), conditions);
			public Pool Add(int item, params Condition[] conditions) => Add(ContentSamples.ItemsByType[item], conditions);

			// Picks a number of items (up to Slots) from the entries list, provided conditions are met.
			public IEnumerable<Item> PickItems() {
				// This is not a fast way to pick items without replacement, but it's certainly easy. Be careful not to do this many many times per frame, or on huge lists of items.
				var list = Entries.Where(e => !e.Disabled && e.ConditionsMet()).ToList();
				for (int i = 0; i < Slots; i++) {
					if (list.Count == 0)
						break;

					int k = Main.rand.Next(list.Count);
					yield return list[k].Item;

					// remove the entry from the list so it can't be selected again this pick
					list.RemoveAt(k);
				}
			}
		}

		public List<Pool> Pools { get; } = [];

		public override IEnumerable<Entry> ActiveEntries => Pools.SelectMany(p => p.Entries).Where(e => !e.Disabled);

		public Pool AddPool(string name, int slots) {
			var pool = new Pool(name, slots, []);
			Pools.Add(pool);
			return pool;
		}

		public Pool AddPoolFromNameSpace(string name, int slots, string fromNamespace, Mod mod) {
			var pool = new Pool(name, slots, []);

			var items = mod.GetContent<ModItem>()
				.Where(item => item.GetType().Namespace == fromNamespace)
				.ToList();

			if (items.Count == 0) {
				Main.NewText($"[CannotShop] 警告：命名空间 '{fromNamespace}' 中未找到任何 ModItem！", Color.OrangeRed);
			}

			foreach (var modItem in items) {
				var shopItem = new Item(modItem.Type) {
					shopSpecialCurrency = ArknightsMod.OriginiumIngotCurrencyId
				};
				pool.Add(shopItem); // 调用 Add(Item, Condition[])
			}

			Pools.Add(pool);
			return pool;
		}

		// Some methods to add a pool with a single item
		public void Add(Item item, params Condition[] conditions) => AddPool(item.ModItem?.FullName ?? $"Terraria/{item.type}", slots: 1).Add(item, conditions);
		public void Add<T>(params Condition[] conditions) where T : ModItem => Add(ModContent.ItemType<T>(), conditions);
		public void Add(int item, params Condition[] conditions) => Add(ContentSamples.ItemsByType[item], conditions);

		// Here is where we actually 'roll' the contents of the shop
		public List<Item> GenerateNewInventoryList() {
			var items = new List<Item>();
			foreach (var pool in Pools) {
				items.AddRange(pool.PickItems());
			}
			return items;
		}

		public override void FillShop(ICollection<Item> items, NPC npc) {
			// use the items which were selected when the NPC spawned.
			foreach (var item in Cannot.shopItems) {
				// make sure to add a clone of the item, in case any ModifyActiveShop hooks adjust the item when the shop is opened
				items.Add(item.Clone());
			}
		}

		public override void FillShop(Item[] items, NPC npc, out bool overflow) {
			overflow = false;
			int i = 0;
			// use the items which were selected when the NPC spawned.
			foreach (var item in Cannot.shopItems) {

				if (i == items.Length - 1) {
					// leave the last slot empty for selling
					overflow = true;
					return;
				}

				// make sure to add a clone of the item, in case any ModifyActiveShop hooks adjust the item when the shop is opened
				items[i++] = item.Clone();
			}
		}
	}

	internal class Cannot_DorpCollection : IItemDropRule
	{
		readonly CannotRunaway condition = new();

		public List<IItemDropRuleChainAttempt> ChainedRules { get; private set; } = [];

		public bool CanDrop(DropAttemptInfo info) => condition.CanDrop(info);

		public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
			// 逃走时随机掉落一件“当前正在售卖的藏品”（即当前坎诺特商店内容）
			var shopItems = NPCShopSystem.CannotShopItems;
			float perItemRate = shopItems.Count > 0 ? ratesInfo.parentDroprateChance / shopItems.Count : 0f;
			for (int i = 0; i < shopItems.Count; i++)
				drops.Add(new DropRateInfo(shopItems[i].type, 1, 1, perItemRate, ratesInfo.conditions));
			Chains.ReportDroprates(ChainedRules, 1, drops, ratesInfo);
		}

		public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
			var shopItems = NPCShopSystem.CannotShopItems;
			if (shopItems.Count > 0) {
				int type = shopItems[info.rng.Next(shopItems.Count)].type;
				CommonCode.DropItem(info, type, 1);
			}
			ItemDropAttemptResult result = default;
			result.State = ItemDropAttemptResultState.Success;
			return result;
		}
	}

	internal class CannotRunaway : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) {
			if (info.npc.ModNPC is Cannot cannot)
				return cannot.Runaway;
			return false;
		}

		public bool CanShowItemDropInUI() => true;

		public string GetConditionDescription() => Language.GetTextValue("Mods.ArknightsMod.ItemDropRuleCondition.CannotRunaway");
	}

	internal class CannotDead : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) {
			// 友善阶段被打死不掉落任何东西——只有敌对阶段（战斗中）被击败才算"击败"。
			if (info.npc.ModNPC is Cannot cannot)
				return !cannot.Runaway && cannot.Phase == CannotPhase.Hostile;
			return false;
		}

		public bool CanShowItemDropInUI() => true;

		public string GetConditionDescription() => Language.GetTextValue("Mods.ArknightsMod.ItemDropRuleCondition.CannotDead");
	}

	internal class CannotSpawnHelper : ModSystem
	{
		public static int RespawnCooldown;

		public override void PreUpdateNPCs() {
			if (RespawnCooldown > 0)
				RespawnCooldown--;
		}
	}
}
