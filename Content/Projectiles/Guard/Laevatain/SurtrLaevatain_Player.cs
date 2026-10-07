using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	public class SurtrLaevatain_Player : ModPlayer
	{
		private static Asset<Effect> transformationFireEffect;
		public int transformationFireDuration = 150;
		public int transformationFireTimer = -1;
		private Color transformationFireColor = new Color(255, 72, 12);
		private Vector2 transformationFireSize = new Vector2(258f, 336f);

		public bool TransformationFireActive => transformationFireTimer >= 0 &&
			transformationFireTimer < transformationFireDuration;

		public float TransformationFireProgress => transformationFireTimer < 0
			? 1f
			: MathHelper.Clamp((float)transformationFireTimer / transformationFireDuration, 0f, 1f);

		public override void Load()
		{
			if (!Main.dedServ)
				On_LegacyPlayerRenderer.DrawPlayerFull += DrawTransformationAfterPlayer;
		}

		public override void Unload()
		{
			if (!Main.dedServ)
				On_LegacyPlayerRenderer.DrawPlayerFull -= DrawTransformationAfterPlayer;
			transformationFireEffect = null;
		}

		public override void UpdateDead() => StopTransformationFire();

		public override void PostUpdate() {
			if (TransformationFireActive)
				transformationFireTimer++;

			if (Player.HeldItem.type == ModContent.ItemType<SurtrLaevatain>()) {
				if (Player.ownedProjectileCounts[ModContent.ProjectileType<SurtrLaevatain_Proj>()] == 0&&
				    Player.ownedProjectileCounts[ModContent.ProjectileType<LaevatainProjectile_3>()] == 0) {
					Projectile.NewProjectile(Player.GetSource_FromThis(),Player.MountedCenter-Main.screenPosition,Vector2.One,ModContent.ProjectileType<SurtrLaevatain_Proj>()
						,Player.HeldItem.damage,Player.HeldItem.knockBack);
				}
			}
		}

		/// <summary>调用一次开始播放，随后自动跟随玩家绘制。颜色默认橙红，大小单位为像素。</summary>
		public void StartTransformationFire(Color? color = null, float durationSeconds = 2.5f,
			Vector2? size = null)
		{
			if (Main.dedServ)
				return;
			if (!float.IsFinite(durationSeconds) || durationSeconds <= 0f || durationSeconds > 3600f)
				throw new ArgumentOutOfRangeException(nameof(durationSeconds));
			Vector2 drawSize = size ?? new Vector2(258f, 336f);
			if (!float.IsFinite(drawSize.X) || !float.IsFinite(drawSize.Y) || drawSize.X <= 0f || drawSize.Y <= 0f)
				throw new ArgumentOutOfRangeException(nameof(size));

			transformationFireColor = color ?? new Color(255, 72, 12);
			transformationFireSize = drawSize;
			transformationFireDuration = Math.Max(1, (int)MathF.Ceiling(durationSeconds * 60f));
			transformationFireTimer = 0;
		}

		/// <summary>立即关闭动画；正常播放会在总时长结束后自动停止。</summary>
		public void StopTransformationFire() => transformationFireTimer = -1;

		private static void DrawTransformationAfterPlayer(On_LegacyPlayerRenderer.orig_DrawPlayerFull orig,
			LegacyPlayerRenderer renderer, Camera camera, Player drawPlayer)
		{
			orig(renderer, camera, drawPlayer);
			if (Main.gameMenu || !drawPlayer.active || drawPlayer.dead || drawPlayer.ghost)
				return;
			drawPlayer.GetModPlayer<SurtrLaevatain_Player>().DrawTransformationFire(camera);
		}

		private void DrawTransformationFire(Camera camera)
		{
			if (!TransformationFireActive)
				return;

			transformationFireEffect ??= ModContent.Request<Effect>(
				"ArknightsMod/Content/Projectiles/Guard/Laevatain/FireTransformation",
				AssetRequestMode.ImmediateLoad);
			Effect effect = transformationFireEffect.Value;
			Texture2D texture = TextureAssets.MagicPixel.Value;
			effect.Parameters["uTime"]?.SetValue(transformationFireTimer / 60f);
			effect.Parameters["uDuration"]?.SetValue(transformationFireDuration / 60f);
			effect.Parameters["uFireColor"]?.SetValue(transformationFireColor.ToVector3());
			effect.Parameters["uSize"]?.SetValue(Vector2.One);

			// DrawPlayerFull 已结束玩家的批次；这里独立 Begin/End，不更改后续绘制状态。
			SpriteBatch spriteBatch = camera.SpriteBatch;
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
				DepthStencilState.None, camera.Rasterizer, effect,
				camera.GameViewMatrix.TransformationMatrix);
			try
			{
				Vector2 feet = Player.Bottom + new Vector2(0f, Player.gfxOffY);
				spriteBatch.Draw(texture, feet - camera.UnscaledPosition, null,
					Color.White * (transformationFireColor.A / 255f), 0f,
					new Vector2(texture.Width * 0.5f, texture.Height),
					transformationFireSize / texture.Size(), SpriteEffects.None, 0f);
			}
			finally
			{
				spriteBatch.End();
			}
		}
	}
}
