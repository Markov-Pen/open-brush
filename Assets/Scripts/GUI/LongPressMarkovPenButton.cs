using UnityEngine;

namespace TiltBrush
{
    /// @brief Handles a button with separate normal-press and long-press behaviour
    /// A normal press activates the configured Markov tool without opening a panel.
    /// A long press activates the configured long-press tool and toggles the assigned panel.
    public class LongPressMarkovPenButton : BaseButton
    {
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

        private const float k_MinDirectionSqrMagnitude = 0.001f;

        private float m_PressTimer;
        private bool m_HasLongPressTriggered;
        private bool m_WasLongPressPanelOpen;

        /// @brief Initialize button state and subscribes to tool-change notifications
        protected override void Awake()
        {
            base.Awake();

            App.Switchboard.ToolChanged += UpdateVisuals;

            m_HoldFocus = false;
            m_WasLongPressPanelOpen = IsLongPressPanelOpen();
        }

        /// @brief Remove event subscriptions before the button is destroyed
        protected override void OnDestroy()
        {
            App.Switchboard.ToolChanged -= UpdateVisuals;

            base.OnDestroy();
        }

        /// @brief Update the button selection when the long-press panel opens or closes
        private void Update()
        {
            bool isLongPressPanelOpen = IsLongPressPanelOpen();

            if (m_WasLongPressPanelOpen == isLongPressPanelOpen)
            {
                return;
            }

            m_WasLongPressPanelOpen = isLongPressPanelOpen;
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
            m_PressTimer = 0.0f;
            m_HasLongPressTriggered = false;
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

            if (TryActivateTool(m_LongPressTool))
            {
                TryToggleLongPressPanel();
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
                SetButtonUntouched();
                return;
            }

            if (TryToggleNormalPressTool())
            {
                RefreshSelectionVisuals();
                PlayPressedAudio();
            }

            SetButtonUntouched();
        }

        /// @brief Try to toggle the configured normal-press tool
        /// @return True if the tool state was changed successfully.
        private bool TryToggleNormalPressTool()
        {
            if (SketchSurfacePanel.m_Instance == null)
            {
                Debug.LogError(
                    "LongPressMarkovPenButton: SketchSurfacePanel instance is null.");
                return false;
            }

            if (IsNormalPressToolActive())
            {
                SketchSurfacePanel.m_Instance.DisableSpecificTool(m_NormalPressTool);
                return true;
            }

            SketchSurfacePanel.m_Instance.EnableSpecificTool(m_NormalPressTool);
            return true;
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

            m_PressTimer = 0.0f;
            m_HasLongPressTriggered = false;
        }

        /// @brief Update the base button visuals and refreshes the selected state
        public override void UpdateVisuals()
        {
            base.UpdateVisuals();
            RefreshSelectionVisuals();
        }

        /// @brief Update the activated visual state based on the active tool and panel state
        private void RefreshSelectionVisuals()
        {
            bool isNormalPressToolActive = IsNormalPressToolActive();
            bool isLongPressPanelOpen = IsLongPressPanelOpen();
            bool isButtonSelected =
                isNormalPressToolActive || isLongPressPanelOpen;

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
            if (SketchSurfacePanel.m_Instance == null)
            {
                return false;
            }

            return SketchSurfacePanel.m_Instance.GetCurrentToolType() ==
                m_NormalPressTool;
        }

        /// @brief Check whether the configured long-press panel is currently open
        /// @return True if the configured long-press panel is open.
        private bool IsLongPressPanelOpen()
        {
            return PanelManager.m_Instance != null &&
                PanelManager.m_Instance.IsPanelOpen(m_LongPressPanelType);
        }

        /// @brief Try to activate the specified tool
        /// @param toolType The tool type to activate.
        /// @return True if the tool was activated successfully.
        private bool TryActivateTool(BaseTool.ToolType toolType)
        {
            if (SketchSurfacePanel.m_Instance == null)
            {
                Debug.LogError(
                    "LongPressMarkovPenButton: SketchSurfacePanel instance is null.");
                return false;
            }

            SketchSurfacePanel.m_Instance.EnableSpecificTool(toolType);
            return true;
        }

        /// @brief Try to open or close the configured long-press panel
        /// @return True if the panel action was completed successfully.
        private bool TryToggleLongPressPanel()
        {
            if (PanelManager.m_Instance == null ||
                SketchControlsScript.m_Instance == null)
            {
                Debug.LogError(
                    "LongPressMarkovPenButton: PanelManager or " +
                    "SketchControlsScript instance is null.");
                return false;
            }

            if (m_AlwaysSpawnPanel)
            {
                PanelManager.m_Instance.DismissNonCorePanel(m_LongPressPanelType);
                OpenLongPressPanel();

                m_WasLongPressPanelOpen = true;
                RefreshSelectionVisuals();
                return true;
            }

            if (IsLongPressPanelOpen())
            {
                PanelManager.m_Instance.DismissNonCorePanel(m_LongPressPanelType);
                m_WasLongPressPanelOpen = false;
            }
            else
            {
                OpenLongPressPanel();
                m_WasLongPressPanelOpen = true;
            }

            RefreshSelectionVisuals();
            return true;
        }

        /// @brief Open the configured long-press panel at the custom spawn transform
        private void OpenLongPressPanel()
        {
            SketchControlsScript.m_Instance.OpenPanelOfType(
                m_LongPressPanelType,
                GetLongPressPanelSpawnTransform());
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

            Vector3 userRight = headTransform.right;

            if (userRight.sqrMagnitude < k_MinDirectionSqrMagnitude)
            {
                userRight = transform.right;
            }

            userRight.Normalize();

            Vector3 userForward = headTransform.forward;

            if (userForward.sqrMagnitude < k_MinDirectionSqrMagnitude)
            {
                userForward = transform.forward;
            }

            userForward.Normalize();

            Vector3 panelPosition =
                transform.position +
                userRight * m_PanelRightOffsetFromLeftController +
                userForward * m_PanelForwardOffsetFromLeftController +
                Vector3.up * m_PanelUpOffsetFromLeftController;

            Quaternion panelRotation = transform.rotation;

            if (m_UseHeadRotationForPanel)
            {
                panelRotation = Quaternion.LookRotation(
                    userForward,
                    headTransform.up);
            }

            panelRotation *= Quaternion.Euler(m_PanelRotationOffsetEulers);

            TrTransform panelSpawnTransform = TrTransform.FromTransform(transform);
            panelSpawnTransform.translation = panelPosition;
            panelSpawnTransform.rotation = panelRotation;
            panelSpawnTransform.scale = 1.0f;

            return panelSpawnTransform;
        }

        /// @brief Play the configured pressed audio feedback
        private void PlayPressedAudio()
        {
            if (!m_ButtonHasPressedAudio)
            {
                return;
            }

            AudioManager.m_Instance.ItemSelect(transform.position);
        }

        /// @brief Resets the button to its untouched visual state
        private void SetButtonUntouched()
        {
            m_CurrentButtonState = ButtonState.Untouched;
            ResetScale();
        }
    }
}
