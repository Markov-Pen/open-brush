using UnityEngine;

namespace TiltBrush
{
    /// @brief Handles a button with separate normal-press and long-press behaviour
    /// A normal press activates the configured Markov tool without opening a panel.
    /// A long press activates the configured long-press tool and toggles the assigned panel.
    public class LongPressMarkovPenButton : BaseButton
    {
        private const float k_MinDirectionSqrMagnitude = 0.001f;

        [SerializeField] private float m_LongPressDuration = 0.3f;

        [Header("Normal Press")]
        [SerializeField] private BaseTool.ToolType m_NormalPressTool;

        [Header("Long Press")]
        [SerializeField] private BaseTool.ToolType m_LongPressTool;

        [Header("Long Press Panel")]
        [SerializeField] private BasePanel.PanelType m_LongPressPanelType;
        [SerializeField] private bool m_AlwaysSpawnPanel = false;

        [Header("Long Press Panel Spawn")]
        [SerializeField] private float m_PanelRightOffsetFromLeftController = 0.35f;
        [SerializeField] private float m_PanelForwardOffsetFromLeftController = 0.15f;
        [SerializeField] private float m_PanelUpOffsetFromLeftController = 0.05f;

        [Header("Long Press Panel Rotation")]
        [SerializeField] private bool m_UseHeadRotationForPanel = true;
        [SerializeField] private Vector3 m_PanelRotationOffsetEulers = Vector3.zero;

        private float m_PressTimer;
        private bool m_HasLongPressTriggered;

        /// @brief Initialize button state and subscribes to tool-change notifications
        protected override void Awake()
        {
            base.Awake();

            App.Switchboard.ToolChanged += UpdateVisuals;

            m_HoldFocus = false;
        }

        /// @brief Remove event subscriptions before the button is destroyed
        protected override void OnDestroy()
        {
            App.Switchboard.ToolChanged -= UpdateVisuals;

            base.OnDestroy();
        }

        /// @brief Update the button selection when the long-press panel opens or closes
        /// There is no panel open/close event, so the selection state is polled.
        /// RefreshSelectionVisuals only touches the visuals when the state changed.
        private void Update()
        {
            RefreshSelectionVisuals();
        }

        /// @brief Start a button press and reset long-press tracking
        /// @param raycastHitInfo The raycast hit information for the interaction.
        public override void ButtonPressed(RaycastHit raycastHitInfo)
        {
            if (!IsAvailable())
            {
                return;
            }

            AdjustButtonPositionAndScale(
                m_ZAdjustClick,
                m_HoverScale,
                m_HoverBoxColliderGrow);

            m_CurrentButtonState = ButtonState.Held;
            ResetLongPressTracking();
        }

        /// @brief Track a held button press and triggers the long-press action when required
        /// @param raycastHitInfo The raycast hit information for the interaction.
        public override void ButtonHeld(RaycastHit raycastHitInfo)
        {
            if (m_CurrentButtonState != ButtonState.Held ||
                m_HasLongPressTriggered)
            {
                return;
            }

            m_PressTimer += Time.deltaTime;

            if (m_PressTimer < m_LongPressDuration)
            {
                return;
            }

            m_HasLongPressTriggered = true;

            if (TryGetSketchSurfacePanel(out SketchSurfacePanel sketchSurfacePanel))
            {
                sketchSurfacePanel.EnableSpecificTool(m_LongPressTool);
                ToggleLongPressPanel();
                PlayPressedAudio();
            }

            SetButtonUntouched();
        }

        /// @brief Activate or deactivates the normal-press tool when no long press was triggered
        public override void ButtonReleased()
        {
            if (m_HasLongPressTriggered)
            {
                m_HasLongPressTriggered = false;
            }
            else if (TryToggleNormalPressTool())
            {
                RefreshSelectionVisuals();
                PlayPressedAudio();
            }

            SetButtonUntouched();
        }

        /// @brief Update the button appearance when the controller starts hovering over it
        public override void GainFocus()
        {
            if (!IsAvailable())
            {
                return;
            }

            AdjustButtonPositionAndScale(
                m_ZAdjustHover,
                m_HoverScale,
                m_HoverBoxColliderGrow);

            if (m_CurrentButtonState != ButtonState.Pressed)
            {
                AudioManager.m_Instance.ItemHover(transform.position);
            }

            m_CurrentButtonState = ButtonState.Hover;
            SetDescriptionActive(true);

            ResetLongPressTracking();
        }

        /// @brief Update the base button visuals and refreshes the selected state
        public override void UpdateVisuals()
        {
            base.UpdateVisuals();
            RefreshSelectionVisuals();
        }

        /// @brief Reset the long-press timer and trigger state
        private void ResetLongPressTracking()
        {
            m_PressTimer = 0.0f;
            m_HasLongPressTriggered = false;
        }

        /// @brief Try to toggle the configured normal-press tool
        /// @return True if the tool state was changed successfully.
        private bool TryToggleNormalPressTool()
        {
            if (!TryGetSketchSurfacePanel(out SketchSurfacePanel sketchSurfacePanel))
            {
                return false;
            }

            if (IsNormalPressToolActive())
            {
                sketchSurfacePanel.DisableSpecificTool(m_NormalPressTool);
            }
            else
            {
                sketchSurfacePanel.EnableSpecificTool(m_NormalPressTool);
            }

            return true;
        }

        /// @brief Update the activated visual state based on the active tool and panel state
        private void RefreshSelectionVisuals()
        {
            bool isButtonSelected = IsNormalPressToolActive() || IsLongPressPanelOpen();

            if (m_ToggleActive == isButtonSelected)
            {
                return;
            }

            m_ToggleActive = isButtonSelected;
            SetButtonActivated(isButtonSelected);
        }

        /// @brief Check whether the normal-press tool is currently active
        /// @return True if the configured normal-press tool is active.
        private bool IsNormalPressToolActive()
        {
            return SketchSurfacePanel.m_Instance != null &&
                SketchSurfacePanel.m_Instance.GetCurrentToolType() == m_NormalPressTool;
        }

        /// @brief Check whether the configured long-press panel is currently open
        /// @return True if the configured long-press panel is open.
        private bool IsLongPressPanelOpen()
        {
            return PanelManager.m_Instance != null &&
                PanelManager.m_Instance.IsPanelOpen(m_LongPressPanelType);
        }

        /// @brief Try to get the active sketch surface panel and log an error if it is missing
        /// @param sketchSurfacePanel The active sketch surface panel.
        /// @return True if a sketch surface panel instance exists.
        private static bool TryGetSketchSurfacePanel(out SketchSurfacePanel sketchSurfacePanel)
        {
            sketchSurfacePanel = SketchSurfacePanel.m_Instance;

            if (sketchSurfacePanel != null)
            {
                return true;
            }

            Debug.LogError("LongPressMarkovPenButton: SketchSurfacePanel instance is null.");
            return false;
        }

        /// @brief Open or close the configured long-press panel
        /// When m_AlwaysSpawnPanel is set, the panel is always respawned at the button.
        private void ToggleLongPressPanel()
        {
            if (PanelManager.m_Instance == null ||
                SketchControlsScript.m_Instance == null)
            {
                Debug.LogError(
                    "LongPressMarkovPenButton: PanelManager or " +
                    "SketchControlsScript instance is null.");
                return;
            }

            bool isPanelOpen = IsLongPressPanelOpen();

            if (m_AlwaysSpawnPanel || isPanelOpen)
            {
                PanelManager.m_Instance.DismissNonCorePanel(m_LongPressPanelType);
            }

            if (m_AlwaysSpawnPanel || !isPanelOpen)
            {
                SketchControlsScript.m_Instance.OpenPanelOfType(
                    m_LongPressPanelType,
                    GetLongPressPanelSpawnTransform());
            }

            RefreshSelectionVisuals();
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

        /// @brief Play the configured pressed audio feedback
        private void PlayPressedAudio()
        {
            if (m_ButtonHasPressedAudio)
            {
                AudioManager.m_Instance.ItemSelect(transform.position);
            }
        }

        /// @brief Resets the button to its untouched visual state
        private void SetButtonUntouched()
        {
            m_CurrentButtonState = ButtonState.Untouched;
            ResetScale();
        }
    }
}
