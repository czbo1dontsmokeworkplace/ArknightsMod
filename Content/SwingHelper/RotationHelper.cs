using Microsoft.Xna.Framework;
using System;

namespace ArknightsMod.Content.SwingHelper
{
    public static class RotationHelper
    {
        // Screen coordinates: positive angles turn clockwise.
        public enum SwingDir { plus = 1, minus = -1 }

        /// <summary>
        /// All angles are radians. totalRotation is a nonnegative travel distance,
        /// not an end angle. The result stays unwrapped, including full turns.
        /// </summary>
        public static float GetSwingRotation(float startRotation, float totalRotation,
            float timer, float totalTime, SwingDir direction = SwingDir.plus)
        {
            if (totalRotation < 0f)
                throw new ArgumentOutOfRangeException(nameof(totalRotation));
            if (direction != SwingDir.plus && direction != SwingDir.minus)
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (totalTime <= 0f)
                return startRotation;

            float progress = EaseOutCubic(timer / totalTime);
            return startRotation + totalRotation * progress * (int)direction;
        }

        public static float EaseOutCubic(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - MathF.Pow(1f - t, 3f);
        }

        public static float EaseInOutSine(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return -(MathF.Cos(MathF.PI * t) - 1f) / 2f;
        }
    }
}
