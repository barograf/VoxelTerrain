using SlimDX;
using SlimDX.Direct3D11;
using System;

namespace VoxelTerrain
{
    /// <summary>
    /// Defines a class with various helper functions.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Computes a mouse direction in world coordinates.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        /// <param name="camera">Allows observing the scene with the mouse and keyboard.</param>
        /// <param name="mousePosition">Position of a mouse in screen coordinates.</param>
        /// <returns>Mouse direction vector.</returns>
        public static Vector3 MouseDirection(Device graphicsDevice, Camera camera, Vector2 mousePosition)
        {
            Viewport viewport = graphicsDevice.ImmediateContext.Rasterizer.GetViewports()[0];

            Vector3 near = new Vector3(mousePosition.X, mousePosition.Y, 0);
            Vector3 far = new Vector3(mousePosition.X, mousePosition.Y, 1);
            near = Vector3.Unproject(near, viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinZ, viewport.MaxZ, camera.ViewProjection);
            far = Vector3.Unproject(far, viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinZ, viewport.MaxZ, camera.ViewProjection);
            far -= near;
            far.Normalize();

            return far;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Restricts a value to be within a zero to one range.
        /// </summary>
        /// <param name="value">The value to saturate.</param>
        /// <returns>The saturated value.</returns>
        public static float Saturate(float value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }

        /// <summary>
        /// Performs a linear interpolation.
        /// </summary>
        /// <param name="x">First parameter.</param>
        /// <param name="y">Second parameter.</param>
        /// <param name="s">A value that linearly interpolates between parameters.</param>
        /// <returns>Interpolated value.</returns>
        public static float Lerp(float x, float y, float s)
        {
            return y * s + x * (1 - s);
        }
    }
}
