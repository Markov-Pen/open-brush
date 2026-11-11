using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Handles drawing the 3D base path for the Markov pen.
    /// </summary>
    ///
    /// <remarks>
    /// Enables the straight edge mode of the pointer manager when the tool is enabled.
    /// Stores the pointer positions while the brush trigger is held.
    /// The tool is not registered yet.
    /// </remarks>
    public class MarkovPen3DBasePathTool : FreePaintTool
    {
        /// <summary>
        /// Holds the pointer positions of the base path.
        /// </summary>
        private readonly List<Vector3> m_BasePathPoints = new();

        /// <summary>
        /// Enables or disables the tool.
        /// </summary>
        ///
        /// <remarks>
        /// Turns on the straight edge mode of the pointer manager when the tool is enabled.
        /// </remarks>
        ///
        /// <param name="isEnabled">
        /// True to enable the tool; false to disable it.
        /// </param>
        public override void EnableTool(bool isEnabled)
        {
            base.EnableTool(isEnabled);

            if (isEnabled)
            {
                PointerManager pointerManager = PointerManager.m_Instance;
                pointerManager.StraightEdgeModeEnabled = true;
            }
        }

        /// <summary>
        /// Updates the tool once per tick.
        /// </summary>
        ///
        /// <remarks>
        /// Called every tool update tick. Delegates to the base implementation
        /// and stores the pointer position while the brush trigger is held.
        /// </remarks>
        public override void UpdateTool()
        {
            base.UpdateTool();

            if (m_brushTrigger)
            {
                (Vector3 position, _) = GetPointerPosition();
                m_BasePathPoints.Add(position);
            }
        }

        /// <summary>
        /// Retrieves the world-space position and rotation of the brush pointer.
        /// </summary>
        ///
        /// <remarks>
        /// Delegates to the base implementation.
        /// </remarks>
        ///
        /// <returns>
        /// A tuple of position and rotation in global coordinates.
        /// </returns>
        protected override (Vector3, Quaternion) GetPointerPosition()
        {
            return base.GetPointerPosition();
        }
    }
}