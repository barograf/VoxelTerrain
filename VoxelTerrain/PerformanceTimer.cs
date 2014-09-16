using System.Runtime.InteropServices;

namespace VoxelTerrain
{
    /// <summary>
    /// Allows high-resolution time measurement.
    /// </summary>
    public class PerformanceTimer
    {
        /// <summary>
        /// Clock frequency.
        /// </summary>
        private long frequency;

        /// <summary>
        /// Time at object creation.
        /// </summary>
        private long startTime;

        /// <summary>
        /// Time value used in delta time method.
        /// </summary>
        private long lastTime;

        /// <summary>
        /// Creates a timer.
        /// </summary>
        public PerformanceTimer()
        {
            QueryPerformanceFrequency(ref frequency);
            QueryPerformanceCounter(ref startTime);
            lastTime = startTime;
        }

        /// <summary>
        /// Retrieves the frequency of the high-resolution performance counter, if one exists.
        /// </summary>
        /// <param name="performanceFrequency">Variable that receives the value.</param>
        /// <returns>If the function fails, the return value is zero.</returns>
        [DllImport("kernel32")]
        private static extern bool QueryPerformanceFrequency(ref long performanceFrequency);

        /// <summary>
        /// Retrieves the current value of the high-resolution performance counter.
        /// </summary>
        /// <param name="performanceCount">Variable that receives the value.</param>
        /// <returns>If the function fails, the return value is zero.</returns>
        [DllImport("kernel32")]
        private static extern bool QueryPerformanceCounter(ref long performanceCount);

        /// <summary>
        /// Computes absolute time.
        /// </summary>
        /// <returns>Computed value.</returns>
        public double GetAbsoluteTime()
        {
            long time = 0;
            QueryPerformanceCounter(ref time);
            return (double)time / frequency;
        }

        /// <summary>
        /// Computes relative time.
        /// </summary>
        /// <returns>Computed value.</returns>
        public double GetTime()
        {
            long time = 0;
            QueryPerformanceCounter(ref time);
            return (double)(time - startTime) / frequency;
        }

        /// <summary>
        /// Computes time between two calls of this method.
        /// </summary>
        /// <returns>Computed value.</returns>
        public double GetDeltaTime()
        {
            long time = 0;
            QueryPerformanceCounter(ref time);
            double result = (double)(time - lastTime) / frequency;
            lastTime = time;
            return result;
        }
    }
}
