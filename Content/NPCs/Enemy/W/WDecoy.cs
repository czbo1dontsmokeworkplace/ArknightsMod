using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	/// <summary>
	/// 假身：W 封烟换位时留在原地的"她"。不动、不攻击、1 点血；被打中即炸成烟并暴露，8 秒没人碰也自己散掉。<br/>
	/// 画的是 W 的站姿帧 + 手持发射器（指向最近的玩家），带一点难以察觉的闪烁——观察力强的玩家能认出来。<br/>
	/// 作为 NPC 存在的意义：召唤物/追踪弹会真的去打它。ai[0]=存活计时；ai[1]=朝向（-1/1，与 W 的 spriteDirection 同义）。
	/// </summary>
	public class WDecoy : ModNPC
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/W/WBoss";

		private const int Lifetime = 480;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 76; // 共用 W 的帧图集
			NPCID.Sets.NPCBestiaryDrawModifiers hide = new() { Hide = true };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hide);
			NPCID.Sets.ImmuneToAllBuffs[Type] = true;
		}

		public override void SetDefaults() {
			NPC.width = 30;
			NPC.height = 48;
			NPC.lifeMax = 1;
			NPC.defense = 0;
			NPC.damage = 0;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.npcSlots = 0f;
			NPC.value = 0f;
			NPC.dontCountMe = true;
			NPC.HitSound = SoundID.Item66 with { Volume = 0.6f, Pitch = 0.3f };
			NPC.DeathSound = null;
			NPC.netAlways = true;
		}

		public override void AI() {
			NPC.velocity.X = 0f;
			NPC.spriteDirection = NPC.ai[1] < 0f ? -1 : 1;
			NPC.ai[0]++;

			// 微闪：慢呼吸式的透明度起伏，每 ~1.5 秒抖一下
			float breathe = (MathF.Sin(NPC.ai[0] * 0.035f) + 1f) * 0.5f;
			NPC.alpha = (int)(breathe * 26f);
			if ((int)NPC.ai[0] % 90 < 2)
				NPC.alpha = 110;

			if (NPC.ai[0] >= Lifetime && Main.netMode != NetmodeID.MultiplayerClient) {
				// 没人上钩：自己散掉，不说话
				NPC.life = 0;
				NPC.HitEffect();
				NPC.active = false;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
			}
		}

		public override void FindFrame(int frameHeight) {
			NPC.frame.Y = WBoss.RowWalkStart * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit) {
			if (NPC.life > 0)
				return;
			// 炸成一团烟；被识破时 W 开口（只有被打死才算识破，自然消散不算）
			WBoss.SmokeBurst(NPC.Center, 22, 3f);
			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item66, NPC.Center);
				for (int i = 0; i < 8; i++) {
					Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(12f, 20f), DustID.GemRuby, Main.rand.NextVector2Circular(2f, 2f), 120, Color.Red, 0.8f);
					d.noGravity = true;
				}
			}
			if (NPC.ai[0] < Lifetime && Main.netMode != NetmodeID.MultiplayerClient)
				WBattleVisuals.Say("DecoyPopped");
		}

		public override bool CheckActive() => false;

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (NPC.IsABestiaryIconDummy)
				return;
			// 手持发射器：指向最近的玩家（各端本地算，纯表现）
			int nearest = -1;
			float best = float.MaxValue;
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player p = Main.player[i];
				if (!p.active || p.dead)
					continue;
				float d = Vector2.DistanceSquared(p.Center, NPC.Center);
				if (d < best) {
					best = d;
					nearest = i;
				}
			}
			Vector2 anchor = NPC.Center + new Vector2(0f, 2f);
			float rot = nearest >= 0 ? (Main.player[nearest].Center - anchor).ToRotation() : (NPC.spriteDirection < 0 ? 0f : MathHelper.Pi);
			Texture2D tex = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/W/WBossLauncher").Value;
			bool facingLeft = Math.Abs(MathHelper.WrapAngle(rot)) > MathHelper.PiOver2;
			SpriteEffects fx = SpriteEffects.None;
			Vector2 origin = new(12f, 20f);
			if (facingLeft) {
				fx = SpriteEffects.FlipVertically;
				origin.Y = tex.Height - origin.Y;
			}
			Color color = drawColor * (1f - NPC.alpha / 255f);
			spriteBatch.Draw(tex, anchor - screenPos, null, color, rot, origin, 1f, fx, 0f);
		}
	}
}
