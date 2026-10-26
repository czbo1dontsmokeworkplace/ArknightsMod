using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using ArknightsMod.Content.Projectiles;

namespace ArknightsMod.Content.Buffs
{
	public class StunDebuff : ModBuff
	{
        protected virtual bool ShowIndicator => true;
		public override void SetStaticDefaults() {
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
			Main.debuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.GetModPlayer<StunDebuffPlayer>().stunDebuff = true;

			//stun parameters
			player.velocity.X = 0;
			if (player.velocity.Y < 0) player.velocity.Y = 0;
		}

		public override void Update(NPC npc, ref int buffIndex) {
			var sdgNPC = npc.GetGlobalNPC<StunDebuffGlobalNPC>();

			sdgNPC.stunDebuff = true;

			if(!sdgNPC.defaultSettings.isAlreadySet) {
				sdgNPC.defaultSettings = (true, npc.noGravity, npc.noTileCollide, npc.damage);
			}

            if (ShowIndicator)
			Projectile.NewProjectile(spawnSource: npc.GetSource_FromThis(),
										position: npc.Top,
										velocity: Vector2.Zero,
											Type: ModContent.ProjectileType<_StunDebuffProjectile>(),
										  Damage: 0,
									   KnockBack: 0,
										   Owner: Main.myPlayer,
											 ai0: ++sdgNPC.tick,
											 ai1: 0.3f,
											 ai2: npc.getRect().Width) ;

			//stun parameters
			npc.velocity = Vector2.Zero;

			npc.damage = 0;
			npc.noGravity = true;
			npc.noTileCollide = false;
		}
	}


	public class StunDebuffPlayer : ModPlayer
	{
		public bool stunDebuff;

		public override void ResetEffects() {
			stunDebuff = false;
		}

		public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
		{
			if (stunDebuff) {
				r *= 0.8f;
				g *= 0.8f;
				b *= 0.8f;
			}
		}
	}

	public class StunDebuffGlobalNPC : GlobalNPC
	{
		public bool stunDebuff;
		public (bool isAlreadySet, bool noGravity, bool noTileCollide, int damage)
				defaultSettings = (false, false, false, 0);
		public int tick;


		public override bool InstancePerEntity => true;
		private bool pinned;
		private Vector2 anchor;

		public override bool PreAI(NPC npc) {
			if (!npc.HasBuff<StunDebuff>() && !npc.HasBuff<MostimaClockStun>()) {
				pinned = false;
				return true;
			}
			if (!pinned) { anchor = npc.position; pinned = true; }
			npc.position = anchor;
			npc.velocity = Vector2.Zero;
			return false;
		}

		public override void PostAI(NPC npc) {
			if (pinned) { npc.position = anchor; npc.velocity = Vector2.Zero; }
		}

		public override void ResetEffects(NPC npc) {

			if (stunDebuff)
			{
				stunDebuff = false;
				return;
			}

			tick = 0;

			if (defaultSettings.isAlreadySet)
			{
				npc.noGravity = defaultSettings.noGravity;
				npc.noTileCollide = defaultSettings.noTileCollide;
				npc.damage = defaultSettings.damage;
				defaultSettings.isAlreadySet = false;

				if (!npc.collideY && npc.velocity.Y > 0) {
					npc.velocity.Y *= 0.2f;
				}
			}
		}

		public override void DrawEffects(NPC npc, ref Color drawColor) {
			if (stunDebuff) {
				//drawColor.R *= (byte)0.8;
				//drawColor.G *= (byte)0.8;
				//drawColor.B *= (byte)0.8;
			}
		}
	}

	public sealed class StunProjectileSuppression : GlobalProjectile
	{
		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			if (projectile.hostile && source is EntitySource_Parent { Entity: NPC npc }
				&& OperatorStunNPC.HasStun(npc))
				// Do not run death effects that could spawn further hostile projectiles.
				projectile.active = false;
		}
	}
}
