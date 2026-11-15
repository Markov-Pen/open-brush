// Copyright 2026 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Manages the 2D Markov Pen popup and drawing panel.
    /// </summary>
    public class MarkovPen2DPopUpWindow : OptionsPopUpWindow
    {
        private const float k_MinDirectionSqrMagnitude = 0.001f;

        private bool m_Was2DPanelOpen;

        [Header("2D Tool and Panel")]
        [SerializeField] private BaseTool.ToolType m_2DTool;
        [SerializeField] private BasePanel.PanelType m_2DPanelType;

        [Header("2D Panel Spawn")]
        [SerializeField] private float m_PanelRightOffsetFromLeftController = 0.35f;
        [SerializeField] private float m_PanelForwardOffsetFromLeftController = 0.15f;
        [SerializeField] private float m_PanelUpOffsetFromLeftController = 0.05f;
        [SerializeField] private bool m_UseHeadRotationForPanel = true;
        [SerializeField] private Vector3 m_PanelRotationOffsetEulers = Vector3.zero;

        /// <summary>
        /// Initializes the popup and activates the 2D Markov Pen mode.
        /// </summary>
        /// <param name="rParent">
        /// The parent GameObject of the popup.
        /// </param>
        /// <param name="sText">
        /// The text passed to the popup.
        /// </param>
        public override void Init(GameObject rParent, string sText)
        {
            base.Init(rParent, sText);

            m_Was2DPanelOpen =
                PanelManager.m_Instance != null &&
                PanelManager.m_Instance.IsPanelOpen(m_2DPanelType);

            ActivateTwoDimensionalTool();
        }

        /// <summary>
        /// Monitors the drawing panel and closes the popup
        /// when the panel transitions from open to closed.
        /// </summary>
        protected override void BaseUpdate()
        {
            base.BaseUpdate();

            if (PanelManager.m_Instance == null)
            {
                return;
            }

            bool isPanelOpen =
                PanelManager.m_Instance.IsPanelOpen(m_2DPanelType);

            if (m_Was2DPanelOpen && !isPanelOpen)
            {
                RequestClose(true);
            }

            m_Was2DPanelOpen = isPanelOpen;
        }

        /// <summary>
        /// Activates the 2D Markov Pen tool and opens its
        /// drawing panel if it is not already open.
        /// </summary>
        public void ActivateTwoDimensionalTool()
        {
            SketchSurfacePanel sketchSurfacePanel =
                SketchSurfacePanel.m_Instance;

            if (sketchSurfacePanel == null ||
                PanelManager.m_Instance == null ||
                SketchControlsScript.m_Instance == null)
            {
                Debug.LogError(
                    "MarkovPen2DPopUpWindow: Required instance is null.");
                return;
            }

            sketchSurfacePanel.EnableSpecificTool(m_2DTool);

            if (!PanelManager.m_Instance.IsPanelOpen(m_2DPanelType))
            {
                SketchControlsScript.m_Instance.OpenPanelOfType(
                    m_2DPanelType,
                    GetLongPressPanelSpawnTransform());
            }

            m_Was2DPanelOpen =
                PanelManager.m_Instance.IsPanelOpen(m_2DPanelType);
        }

        /// <summary>
        /// Returns to the Markov Pen selection popup.
        /// </summary>
        public void BackToOptions()
        {
            MarkovPenPopUpSwitcher.Switch(
                this,
                SketchControlsScript.GlobalCommands.MarkovPenOptions);
        }

        /// <summary>
        /// Closes the 2D Markov Pen popup.
        /// </summary>
        public void HandleOKButton()
        {
            RequestClose(true);
        }

        /// <summary>
        /// Builds a spawn transform to the right of the
        /// left-controller button.
        /// </summary>
        /// <remarks>
        /// The panel rotation follows the user's head direction,
        /// including looking up and down.
        /// </remarks>
        /// <returns>
        /// The transform used to spawn the 2D panel.
        /// </returns>
        private TrTransform GetLongPressPanelSpawnTransform()
        {
            Transform headTransform = ViewpointScript.Head;

            if (headTransform == null)
            {
                return TrTransform.FromTransform(transform);
            }

            Vector3 userRight = GetNormalizedDirection(
                headTransform.right, transform.right);

            Vector3 userForward = GetNormalizedDirection(
                headTransform.forward, transform.forward);

            Vector3 panelPosition =
                transform.position +
                userRight * m_PanelRightOffsetFromLeftController +
                userForward * m_PanelForwardOffsetFromLeftController +
                Vector3.up * m_PanelUpOffsetFromLeftController;

            Quaternion panelRotation = m_UseHeadRotationForPanel
                ? Quaternion.LookRotation(userForward, headTransform.up)
                : transform.rotation;

            panelRotation *= Quaternion.Euler(m_PanelRotationOffsetEulers);

            return TrTransform.TRS(
                panelPosition, panelRotation, 1.0f);
        }

        /// <summary>
        /// Normalizes a direction vector using a fallback
        /// direction if its magnitude is too small.
        /// </summary>
        /// <param name="direction">
        /// The preferred direction.
        /// </param>
        /// <param name="fallbackDirection">
        /// The fallback direction.
        /// </param>
        /// <returns>
        /// The normalized direction vector.
        /// </returns>
        private static Vector3 GetNormalizedDirection(
            Vector3 direction, Vector3 fallbackDirection)
        {
            return direction.sqrMagnitude < k_MinDirectionSqrMagnitude
                ? fallbackDirection.normalized
                : direction.normalized;
        }



        /// <summary>
        /// Saves the current Markov Pen drawing and closes
        /// the drawing panel.
        /// </summary>
        /// <remarks>
        /// Backs up the current control point lists before
        /// finalizing the drawing.
        /// </remarks>
        public void SaveDrawing()
        {
            if (!MarkovPenDrawingFreepaint.IsDrawingFinished)
            {
                Debug.LogWarning(
                    "MarkovPen: Complete both the base curve and " +
                    "the style curve before saving.");
                return;
            }

            CompleteDrawing(true);
        }

        /// <summary>
        /// Discards the current Markov Pen drawing and closes
        /// the drawing panel.
        /// </summary>
        /// <remarks>
        /// Restores the previously backed-up control point lists
        /// before closing the panel.
        /// </remarks>
        public void CloseDrawing()
        {
            CompleteDrawing(false);
        }

        /// <summary>
        /// Completes the current drawing operation and closes
        /// the Markov Pen drawing panel.
        /// </summary>
        /// <param name="save">
        /// True to save the current drawing, false to restore
        /// the previously backed-up control point lists.
        /// </param>
        private void CompleteDrawing(bool save)
        {
            MarkovPenDrawingPanel drawingPanel =
                MarkovPenDrawingPanel.Instance;

            if (drawingPanel == null ||
                !MarkovPenDrawingPanel.IsOpen ||
                PanelManager.m_Instance == null)
            {
                Debug.LogWarning(
                    "MarkovPen2DPopUpWindow: Drawing panel is not open.");
                return;
            }

            // Save or restore the current drawing data.
            if (save)
            {
                MarkovPenDrawingFreepaint.BackupPaintPointLists();
            }
            else
            {
                MarkovPenDrawingFreepaint.RestorePaintPointListsFromBackup();
            }

            // Stop drawing before hiding the panel.
            drawingPanel.PrepareForClose();

            MarkovPenDrawingFreepaint.ResetPointer();
            MarkovPenSketchMemoryScript.DeleteNewMarkovStrokes();

            // OnDisablePanel finalizes the Markov Pen if
            // the control point lists are not empty.
            PanelManager.m_Instance.HidePanel(drawingPanel.Type);
        }

    }
}
