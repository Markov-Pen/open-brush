// Copyright 2022 The Tilt Brush Authors
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

    public class MarkovPenOptionsPopUpWindow : OptionsPopUpWindow
    {
        private const float k_MinDirectionSqrMagnitude = 0.001f;

        [Header("Radio Buttons")]
        [SerializeField] private RadioButton m_2DButton;
        [SerializeField] private RadioButton m_3DButton;

        [Header("2D Tool and Panel")]
        [SerializeField] private BaseTool.ToolType m_2DTool;
        [SerializeField] private BasePanel.PanelType m_PanelType;
        [SerializeField] private bool m_AlwaysSpawnPanel = false;

        [Header("2D Panel Spawn")]
        [SerializeField] private float m_PanelRightOffsetFromLeftController = 0.35f;
        [SerializeField] private float m_PanelForwardOffsetFromLeftController = 0.15f;
        [SerializeField] private float m_PanelUpOffsetFromLeftController = 0.05f;
        [SerializeField] private bool m_UseHeadRotationForPanel = true;
        [SerializeField] private Vector3 m_PanelRotationOffsetEulers = Vector3.zero;
        override public void Init(GameObject rParent, string sText)
        {
            base.Init(rParent, sText);
            var pm = PointerManager.m_Instance;

        }




        public void HandleOKButton()
        {
            RequestClose(true);
        }

        public void TwoDimensional(RadioButton button)
        {
            SketchSurfacePanel sketchSurfacePanel =
                SketchSurfacePanel.m_Instance;

            if (sketchSurfacePanel == null ||
                PanelManager.m_Instance == null ||
                SketchControlsScript.m_Instance == null)
            {
                Debug.LogError(
                    "MarkovPenOptionsPopUpWindow: Required instance is null.");
                return;
            }

            // Activate the 2D tool.
            sketchSurfacePanel.EnableSpecificTool(m_2DTool);

            // Check whether the panel is already open.
            bool isPanelOpen =
                PanelManager.m_Instance.IsPanelOpen(m_PanelType);

            if (!isPanelOpen)
            {
                SketchControlsScript.m_Instance.OpenPanelOfType(
                    m_PanelType,
                    GetLongPressPanelSpawnTransform());
            }


        }






        /// @brief Build a panel spawn transform to the right of this left-controller button
        /// The panel rotation follows the user's full head direction, including looking down/up.
        /// @return The transform used by SketchControlsScript.OpenPanelOfType.
        private TrTransform GetLongPressPanelSpawnTransform()
        {
            Transform headTransform = ViewpointScript.Head;

            if (headTransform == null)
            {
                return TrTransform.FromTransform(transform);
            }

            Vector3 userRight = GetNormalizedDirection(headTransform.right, transform.right);
            Vector3 userForward = GetNormalizedDirection(headTransform.forward, transform.forward);

            Vector3 panelPosition =
                transform.position +
                userRight * m_PanelRightOffsetFromLeftController +
                userForward * m_PanelForwardOffsetFromLeftController +
                Vector3.up * m_PanelUpOffsetFromLeftController;

            Quaternion panelRotation = m_UseHeadRotationForPanel
                ? Quaternion.LookRotation(userForward, headTransform.up)
                : transform.rotation;

            panelRotation *= Quaternion.Euler(m_PanelRotationOffsetEulers);

            return TrTransform.TRS(panelPosition, panelRotation, 1.0f);
        }
        /// @brief Normalize a direction, falling back to another direction if it is too short
        /// @param direction The preferred direction.
        /// @param fallbackDirection The direction used when the preferred direction is degenerate.
        /// @return The normalized direction.
        private static Vector3 GetNormalizedDirection(Vector3 direction, Vector3 fallbackDirection)
        {
            return direction.sqrMagnitude < k_MinDirectionSqrMagnitude
                ? fallbackDirection.normalized
                : direction.normalized;
        }


    }
} // namespace TiltBrush
