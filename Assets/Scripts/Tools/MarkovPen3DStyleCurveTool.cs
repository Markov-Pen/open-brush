using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Handles drawing the 3D style curve for the Markov pen.
    /// </summary>
    ///
    /// <remarks>
    /// Keeps the free-paint behavior unchanged and is not registered as a tool yet.
    /// </remarks>
    public class MarkovPen3DStyleCurveTool : FreePaintTool
    {
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