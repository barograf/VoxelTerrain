using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;

namespace VoxelTerrain
{
    /// <summary>
    /// Defines settings used in making post process effects.
    /// </summary>
    public struct PostProcessSettings
    {
        /// <summary>
        /// A pack of bloom effect settings.
        /// X = Luminance
        /// Y = MiddleGray
        /// Z = WhiteCutoff
        /// W = GlowPower
        /// </summary>
        public Vector4 BloomSettings;
    }
}
