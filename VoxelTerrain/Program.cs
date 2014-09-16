using System;
using SlimDX.Windows;
using System.Threading;
using System.Windows.Forms;

namespace VoxelTerrain
{
    /// <summary>
    /// Main program class.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ProgramForm program = new ProgramForm("VoxelTerrain");
            program.Initialize();

            PerformanceTimer timer = new PerformanceTimer();

            MessagePump.Run(program, () =>
            {
                double deltaTime = timer.GetDeltaTime();

                if (program.WindowState != FormWindowState.Minimized)
                {
                    program.CheckInput(deltaTime);
                    program.UpdateFrame(deltaTime);
                    program.RenderFrame(deltaTime);
                }
            });

            program.DisposeFramework();
        }
    }
}
