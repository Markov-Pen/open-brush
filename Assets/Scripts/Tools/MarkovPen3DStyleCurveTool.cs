using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Handles drawing the 3D style curve for the Markov pen.
    /// </summary>
    ///
    /// <remarks>
    /// Stores the pointer positions while the brush trigger is held
    /// and clears them together with the previous strokes when a new stroke begins.
    /// The tool is not registered yet.
    /// </remarks>
    public class MarkovPen3DStyleCurveTool : FreePaintTool
    {
        /// <summary>
        /// Holds the pointer positions of the style curve.
        /// </summary>
        private readonly List<Vector3> m_StyleCurvePoints = new();

        /// <summary>
        /// Enables or disables the tool.
        /// </summary>
        ///
        /// <remarks>
        /// Starts capturing the strokes that exist at that moment when the tool is enabled.
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
                MarkovPenSketchMemoryScript.BeginMarkovStrokeCapture();
            }
        }

        /// <summary>
        /// Updates the tool once per tick.
        /// </summary>
        ///
        /// <remarks>
        /// Called every tool update tick. Delegates to the base implementation.
        /// When the brush trigger is pressed, deletes the strokes of the previous attempt,
        /// starts a new stroke capture and clears the stored points.
        /// While the brush trigger is held, stores the pointer position.
        /// </remarks>
        public override void UpdateTool()
        {
            base.UpdateTool();

            if (m_brushTriggerDown)
            {
                MarkovPenSketchMemoryScript.DeleteNewMarkovStrokes();
                MarkovPenSketchMemoryScript.BeginMarkovStrokeCapture();
                m_StyleCurvePoints.Clear();
            }

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