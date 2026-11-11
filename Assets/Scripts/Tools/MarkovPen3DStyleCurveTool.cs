using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Handles drawing the 3D style curve for the Markov pen.
    /// </summary>
    ///
    /// <remarks>
    /// Stores the pointer positions while the brush trigger is held.
    /// The tool is not registered yet.
    /// </remarks>
    public class MarkovPen3DStyleCurveTool : FreePaintTool
    {
        /// <summary>
        /// Holds the pointer positions of the style curve.
        /// </summary>
        private readonly List<Vector3> m_StyleCurvePoints = new();

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
                m_StyleCurvePoints.Add(position);
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