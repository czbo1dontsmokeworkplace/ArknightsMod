using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.SwingHelper
{
    /// <summary>RT 热浪合成器。仅加载 Effect；绘制时机和 RT 生命周期由调用者控制。</summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class HeatWaveRTEffect : ModSystem
    {
        private static Asset<Effect> effectAsset;

        public override void Load()
        {
            if (!Main.dedServ)
                effectAsset = ModContent.Request<Effect>(
                    "ArknightsMod/Content/SwingHelper/Effects/HeatWave", AssetRequestMode.ImmediateLoad);
        }

        public override void Unload() => effectAsset = null;

        /// <summary>
        /// 把 source RT 扭曲后画到当前绑定的输出目标。入口必须没有正在 Begin 的 SpriteBatch；
        /// 本方法自行 Begin/End，不创建或切换 RT，不注册滤镜。
        /// source 铺满当前 viewport。screenOrigin、distance、strength 按输出 viewport 像素计。
        /// 角度使用弧度，plus 顺时针、minus 逆时针；opacity 控制扭曲幅度，不改变透明度。
        /// 可直接用在 RTHelper.Draw / DrawScreen 的 compositeDraw 或 DrawPingPong 的 process 中。
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch, Texture2D source,
            float startAngle, float endAngle, RotationHelper.SwingDir direction,
            Vector2 screenOrigin, float distance, float strength = 4f, float opacity = 1f,
            float? time = null, BlendState blendState = null)
        {
            DrawCore(spriteBatch, source, startAngle, endAngle, direction, screenOrigin, distance,
                strength, opacity, time, blendState, false, screenOrigin, 1f, Vector2.Zero, 0f, 0f);
        }

        /// <summary>沿刺击轴扭曲背景，并可叠加一个命中点扩散波纹。坐标与尺寸使用输出 viewport 像素。</summary>
        public static void DrawStab(SpriteBatch spriteBatch, Texture2D source,
            Vector2 screenStart, Vector2 screenEnd, float halfWidth, float strength = 3f,
            float opacity = 1f, float? time = null, Vector2 impactCenter = default,
            float impactRadius = 0f, float impactOpacity = 0f, BlendState blendState = null)
        {
            if (!float.IsFinite(screenStart.X) || !float.IsFinite(screenStart.Y))
                throw new ArgumentOutOfRangeException(nameof(screenStart));
            if (!float.IsFinite(screenEnd.X) || !float.IsFinite(screenEnd.Y))
                throw new ArgumentOutOfRangeException(nameof(screenEnd));
            if (!float.IsFinite(halfWidth))
                throw new ArgumentOutOfRangeException(nameof(halfWidth));
            if (!float.IsFinite(impactCenter.X) || !float.IsFinite(impactCenter.Y))
                throw new ArgumentOutOfRangeException(nameof(impactCenter));
            if (!float.IsFinite(impactRadius) || impactRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(impactRadius));
            if (!float.IsFinite(impactOpacity))
                throw new ArgumentOutOfRangeException(nameof(impactOpacity));

            Vector2 axis = screenEnd - screenStart;
            float angle = MathF.Atan2(axis.Y, axis.X);
            DrawCore(spriteBatch, source, angle, angle, RotationHelper.SwingDir.plus, screenStart,
                axis.Length(), strength, opacity, time, blendState, true, screenEnd,
                Math.Max(1f, halfWidth), impactCenter, impactRadius, impactOpacity);
        }

        private static void DrawCore(SpriteBatch spriteBatch, Texture2D source,
            float startAngle, float endAngle, RotationHelper.SwingDir direction,
            Vector2 screenOrigin, float distance, float strength, float opacity,
            float? time, BlendState blendState, bool capsule, Vector2 screenEnd, float halfWidth,
            Vector2 impactCenter, float impactRadius, float impactOpacity)
        {
            if (Main.dedServ || effectAsset == null)
                return;
            ArgumentNullException.ThrowIfNull(spriteBatch);
            ArgumentNullException.ThrowIfNull(source);
            if (source.IsDisposed)
                throw new ObjectDisposedException(nameof(source));

            float sweep = GetSweepAngle(startAngle, endAngle, direction);
            if (!float.IsFinite(screenOrigin.X) || !float.IsFinite(screenOrigin.Y))
                throw new ArgumentOutOfRangeException(nameof(screenOrigin));
            if (!float.IsFinite(distance))
                throw new ArgumentOutOfRangeException(nameof(distance));
            if (!float.IsFinite(strength))
                throw new ArgumentOutOfRangeException(nameof(strength));
            if (!float.IsFinite(opacity))
                throw new ArgumentOutOfRangeException(nameof(opacity));
            float seconds = time ?? Main.GlobalTimeWrappedHourly;
            if (!float.IsFinite(seconds))
                throw new ArgumentOutOfRangeException(nameof(time));

            GraphicsDevice device = spriteBatch.GraphicsDevice;
            foreach (RenderTargetBinding binding in device.GetRenderTargets())
                if (ReferenceEquals(binding.RenderTarget, source))
                    throw new InvalidOperationException("热浪输入 RT 不能同时作为输出；请使用 RTHelper 或 Ping-Pong。");

            Viewport viewport = device.Viewport;
            Effect effect = effectAsset.Value;
            effect.Parameters["uViewportSize"].SetValue(new Vector2(viewport.Width, viewport.Height));
            effect.Parameters["uSourceSize"].SetValue(new Vector2(source.Width, source.Height));
            effect.Parameters["uOriginPixels"].SetValue(screenOrigin);
            effect.Parameters["uStartAngle"].SetValue(MathHelper.WrapAngle(startAngle));
            effect.Parameters["uSweepAngle"].SetValue(sweep);
            effect.Parameters["uRotationSign"].SetValue((int)direction);
            effect.Parameters["uRadiusPixels"].SetValue(Math.Max(1f, distance));
            effect.Parameters["uStrengthPixels"].SetValue(distance > 0f ? Math.Max(0f, strength) : 0f);
            effect.Parameters["uOpacity"].SetValue(MathHelper.Clamp(opacity, 0f, 1f));
            effect.Parameters["uTime"].SetValue(seconds);
            effect.Parameters["uShape"].SetValue(capsule ? 1f : 0f);
            effect.Parameters["uEndPixels"].SetValue(screenEnd);
            effect.Parameters["uHalfWidthPixels"].SetValue(halfWidth);
            effect.Parameters["uImpactPixels"].SetValue(impactCenter);
            effect.Parameters["uImpactRadius"].SetValue(impactRadius);
            effect.Parameters["uImpactOpacity"].SetValue(MathHelper.Clamp(impactOpacity, 0f, 1f));

            spriteBatch.Begin(SpriteSortMode.Immediate, blendState ?? BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            try
            {
                effect.CurrentTechnique.Passes["HeatWave"].Apply();
                spriteBatch.Draw(source, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            }
            finally
            {
                spriteBatch.End();
            }
        }

        /// <summary>同角度为空范围；相差整圈为整圆；跨零角度沿指定方向取弧。</summary>
        public static float GetSweepAngle(float startAngle, float endAngle, RotationHelper.SwingDir direction)
        {
            if (!float.IsFinite(startAngle))
                throw new ArgumentOutOfRangeException(nameof(startAngle));
            if (!float.IsFinite(endAngle))
                throw new ArgumentOutOfRangeException(nameof(endAngle));
            if (direction != RotationHelper.SwingDir.plus && direction != RotationHelper.SwingDir.minus)
                throw new ArgumentOutOfRangeException(nameof(direction));

            double delta = ((double)endAngle - startAngle) * (int)direction;
            if (Math.Abs(delta) >= MathHelper.TwoPi - 0.00001)
                return MathHelper.TwoPi;
            double sweep = delta % MathHelper.TwoPi;
            if (sweep < 0)
                sweep += MathHelper.TwoPi;
            return (float)sweep;
        }
    }
}
