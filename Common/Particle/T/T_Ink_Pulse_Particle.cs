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

public sealed class T_Ink_Pulse_Particle : global::ArknightsMod.Common.Particle.Particle
	{
		private readonly float peakOpacity;

		public override string TexturePath => "ArknightsMod/Content/Textures/slash_04";
		public override BlendState DrawBlendState => BlendState.AlphaBlend;

		public T_Ink_Pulse_Particle(Vector2 position, float rotation, int lifetime,
			float scale, float opacity)
		{
			Position = position;
			Rotation = rotation;
			Lifetime = lifetime;
			Scale = scale;
			peakOpacity = opacity;
			Color = new Color(2, 3, 7);
		}

		public override void Update()
		{
			float fade = 1f - MathHelper.SmoothStep(0f, 1f,
				MathHelper.Clamp(LifetimeRatio, 0f, 1f));
			Opacity = fade * peakOpacity;
			Scale *= 1.035f;
		}

		public override void Draw()
		{
			Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * Opacity,
				Rotation, Texture.Size() * 0.5f, new Vector2(0.115f, 0.075f) * Scale,
				SpriteEffects.None, 0f);
		}
	}
