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

public sealed class T_Ink_Particle : global::ArknightsMod.Common.Particle.Particle
	{
		private readonly float strength;
		private readonly Projectile flightSource;
		private readonly int flightIdentity;
		public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
		public override BlendState DrawBlendState => BlendState.AlphaBlend;

		public T_Ink_Particle(Vector2 position, Vector2 velocity, int lifetime, float scale,
			float strength = 1f, Projectile flightSource = null)
		{
			Position = position;
			Velocity = velocity;
			Lifetime = lifetime;
			Scale = scale;
			this.strength = MathHelper.Clamp(strength, 0f, 1f);
			this.flightSource = flightSource;
			flightIdentity = flightSource?.identity ?? -1;
			Color = new Color(3, 4, 9);
			Rotation = (flightSource?.velocity ?? velocity).ToRotation() + MathHelper.PiOver2;
		}

		public override void Update()
		{
			Velocity *= 0.945f;
			if (flightSource != null)
			{
				if (flightSource.active && flightSource.identity == flightIdentity && flightSource.velocity.LengthSquared() > 0.01f)
					Rotation = flightSource.velocity.ToRotation() + MathHelper.PiOver2;
			}
			else if (Velocity.LengthSquared() > 0.01f)
				Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
			float fadeIn = Utils.GetLerpValue(0f, 0.12f, LifetimeRatio, true);
			Opacity = fadeIn * MathF.Pow(1f - LifetimeRatio, 1.35f) * 0.86f * strength;
			Scale *= 0.982f;
		}

		public override void Draw()
		{
			Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * Opacity,
				Rotation, Texture.Size() * 0.5f, new Vector2(0.34f, 1.90f) * Scale,
				SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null,
				Color.Black * (Opacity * 0.68f), Rotation, Texture.Size() * 0.5f,
				new Vector2(0.15f, 1.28f) * Scale,
				SpriteEffects.None, 0f);
		}
	}
