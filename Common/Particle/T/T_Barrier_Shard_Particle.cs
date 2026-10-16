using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Items.Weapons.Medic.Shining;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Medic.Shining;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Barrier_Shard_Particle : global::ArknightsMod.Common.Particle.Particle
	{
		private readonly Color initialColor;
		private readonly Projectile flightSource;
		private readonly int flightIdentity;

		// DefaultParticle 是本项目的一张独立单帧粒子贴图，不按精灵表切帧。
		public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
		public override BlendState DrawBlendState => BlendState.Additive;

		public T_Barrier_Shard_Particle(Vector2 position, Vector2 velocity, Color color,
			int lifetime, float scale, Projectile flightSource = null)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			initialColor = color;
			this.flightSource = flightSource;
			flightIdentity = flightSource?.identity ?? -1;
			Lifetime = lifetime;
			Scale = scale;
			Rotation = flightSource != null ? flightSource.velocity.ToRotation() + MathHelper.PiOver2 : velocity.ToRotation();
		}

		public override void Update()
		{
			Velocity *= 0.955f;
			if (flightSource != null)
			{
				if (flightSource.active && flightSource.identity == flightIdentity && flightSource.velocity.LengthSquared() > 0.01f)
					Rotation = flightSource.velocity.ToRotation() + MathHelper.PiOver2;
			}
			else
				Rotation += 0.08f * Math.Sign(Velocity.X == 0f ? 1f : Velocity.X);
			Opacity = MathF.Pow(1f - LifetimeRatio, 1.25f);
			Color = Color.Lerp(initialColor, Color.Transparent, LifetimeRatio * LifetimeRatio);
			Scale *= 0.985f;
		}

		public override void Draw()
		{
			Vector2 drawScale = new(Scale * 0.22f, Scale * 1.65f);
			Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * Opacity,
				Rotation, Texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color.White * (Opacity * 0.32f),
				Rotation, Texture.Size() * 0.5f, drawScale * new Vector2(0.28f, 0.76f),
				SpriteEffects.None, 0f);
		}
	}
