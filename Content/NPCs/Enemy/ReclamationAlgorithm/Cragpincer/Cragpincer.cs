using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArknightsMod.Common;

namespace ArknightsMod.Content.NPCs.Enemy.ReclamationAlgorithm.Cragpincer
{
	/// <summary>
	/// 峭壁钳
	/// </summary>
	public class Cragpincer : ModNPC
	{
		private const string _tex = "ArknightsMod/Content/NPCs/Enemy/ReclamationAlgorithm/Cragpincer/Cragpincer";
		public override string Texture => _tex + "_Common";
		private int _time = 0;
		private int _fra = 0;
		private enum State {
			Common,
			Enemy,
			Hit
		}
		private State _state;
		public override void SetDefaults() {
			NPC.width = 0;
			NPC.height = 0;
			NPC.damage = 40;
			NPC.lifeMax = 400;
			NPC.defense = 80;
			NPC.friendly = false;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.value = Item.buyPrice(0, 0, 15, 0);
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}
		public override void AI()
		{
			switch (_state)
			{
				case State.Common:
				{
					break;
				}
				case State.Enemy:
				{
					if(TryGetPlayer(out var target))
					{
						
						if(Vector2.DistanceSquared(NPC.Center,target.Center) < 5184)
						{
							NPC.velocity = Vector2.Zero;
						}
						else
						{
							NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 20f;
						}
					}
					break;
				}
			}
		}
		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
		{
			if(_state == State.Common)
			{
				hurtInfo.Damage = 0;
			}
			else
			{
				base.OnHitPlayer(target,hurtInfo);
			}
		}
		public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
		{
			_state = (_state == State.Hit ? State.Hit : State.Enemy);
		}
		public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			_state = (_state == State.Hit ? State.Hit : State.Enemy);
		}
		private bool TryGetPlayer(out Player target)
		{
			Player _best = null;
			float _bestDis = float.MaxValue;
			foreach(Player player in Main.player)
			{
				if(player.active && !player.dead)
				{
					float now = Vector2.DistanceSquared(player.Center, NPC.Center);
					if (now < _bestDis)
					{
						_bestDis = now;
						_best = player;
					}
				}
			}
			target = _best;
			return _best != null;
		}
		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			switch (_state)
			{
				case State.Enemy:
				{
						if(_fra > 3)
						{
							_fra = 0;
						}
						if(_time > 2)
						{
							_time = 0;
							_fra++;
						}
						Texture2D texture = ModContent.Request<Texture2D>(_tex + "_Walk" + _fra).Value;
						spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, null, Color.White, 0f, texture.GetTextureSize() / 2f, 1f, SpriteEffects.None, 0f);
						break;
				}
				case State.Hit:
				{
						if (_fra > 4) {
							_fra = 0;
						}
						if (_time > 2) {
							_time = 0;
							_fra++;
						}
						Texture2D texture = ModContent.Request<Texture2D>(_tex + "_Hit" + _fra).Value;
						spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, null, Color.White, 0f, texture.GetTextureSize() / 2f, 1f, SpriteEffects.None, 0f);
						break;
				}
            }
			return false;
		}
	}
}
