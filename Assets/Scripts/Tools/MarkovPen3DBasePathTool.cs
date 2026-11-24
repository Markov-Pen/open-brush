using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Handles drawing the 3D base path for the Markov pen.
    /// </summary>
    ///
    /// <remarks>
    /// Enables the straight edge mode of the pointer manager when the tool is enabled.
    /// The tool is not registered yet.
    /// </remarks>
    public class MarkovPen3DBasePathTool : FreePaintTool
    {
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
        /// Called every tool update tick. Delegates to the base implementation.
        /// </remarks>
        public override void UpdateTool()
        {
            base.UpdateTool();
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