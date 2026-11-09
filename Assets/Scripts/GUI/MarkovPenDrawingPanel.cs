using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// @brief Provides the drawing panel used by the Markov pen drawing tool
    /// Handles panel lifetime state, drawing collider raycasts,
    /// button raycasts, and panel button actions.
    public class MarkovPenDrawingPanel : BasePanel
    {
        private const float k_RaycastMaxDistance = 100.0f;

        private static MarkovPenDrawingPanel s_Instance;
        private static bool s_IsOpen;

        [Header("Markov Panel Colliders")]
        [SerializeField]
        private Collider m_DrawingCollider;

        [SerializeField]
        private Collider m_SaveButtonCollider;

        [SerializeField]
        private Collider m_CloseButtonCollider;

        [Header("Button Hover")]
        [SerializeField]
        private Transform m_SaveButtonHoverTarget;

        [SerializeField]
        private Transform m_CloseButtonHoverTarget;

        [SerializeField]
        private float m_HoverScale = 1.1f;

        [Header("Panel Scale")]
        [SerializeField]
        private float m_SizeMultiplier = 0.85f;

        private Vector3 m_InitialLocalScale;
        private Collider m_HoveredButton;
        private Vector3 m_SaveButtonBaseScale;
        private Vector3 m_CloseButtonBaseScale;
        private bool m_IsSaved;

        /// @brief Get the active Markov drawing panel instance
        public static MarkovPenDrawingPanel Instance
        {
            get { return s_Instance; }
        }

        /// @brief Get whether the Markov drawing panel is currently open
        public static bool IsOpen
        {
            get { return s_IsOpen; }
        }

        /// @brief Get the collider used for drawing input
        public Collider DrawingCollider
        {
            get { return m_DrawingCollider; }
        }

        /// @brief Get the collider used for the save button
        public Collider SaveButtonCollider
        {
            get { return m_SaveButtonCollider; }
        }

        /// @brief Get the collider used for the close button
        public Collider CloseButtonCollider
        {
            get { return m_CloseButtonCollider; }
        }

        /// @brief Initialize the panel instance and stores the initial visual state
        protected override void Awake()
        {
            base.Awake();

            s_Instance = this;
            m_InitialLocalScale = transform.localScale;

            if (m_SaveButtonHoverTarget == null && m_SaveButtonCollider != null)
            {
                m_SaveButtonHoverTarget = m_SaveButtonCollider.transform;
            }

            if (m_CloseButtonHoverTarget == null && m_CloseButtonCollider != null)
            {
                m_CloseButtonHoverTarget = m_CloseButtonCollider.transform;
            }

            if (m_SaveButtonHoverTarget != null)
            {
                m_SaveButtonBaseScale = m_SaveButtonHoverTarget.localScale;
            }

            if (m_CloseButtonHoverTarget != null)
            {
                m_CloseButtonBaseScale = m_CloseButtonHoverTarget.localScale;
            }
        }

        /// @brief Activate the panel and prepares the Markov drawing tool
        protected override void OnEnablePanel()
        {
            base.OnEnablePanel();

            s_IsOpen = true;

            MarkovPenSketchMemoryScript.BeginMarkovStrokeCapture();

            ApplyPanelScale();

            MarkovPenDrawingFreepaint.OnPanelOpened();
            m_IsSaved = false;
        }

        /// @brief Deactivate the panel and resets the Markov drawing tool state
        protected override void OnDisablePanel()
        {
            base.OnDisablePanel();

            SetHoveredButton(null);
            s_IsOpen = false;

            MarkovPenSketchMemoryScript.EndMarkovStrokeCapture();
            MarkovPenDrawingFreepaint.OnPanelClosed();

            List<Vector3> baseCurvePoints = MarkovPenDrawingFreepaint.BaseCurvePoints;
            List<Vector3> styleCurvePoints = MarkovPenDrawingFreepaint.StyleCurvePoints;
            if (m_IsSaved)
            {
                MarkovPenTool.CreateMarkovPen(baseCurvePoints, styleCurvePoints);
                SketchSurfacePanel.m_Instance.EnableSpecificTool(BaseTool.ToolType.MarkovPenTool);
            }
            else
            {
                if (baseCurvePoints.Count > 0 && styleCurvePoints.Count > 0)
                {
                    MarkovPenTool.CreateMarkovPen(baseCurvePoints, styleCurvePoints);
                    SketchSurfacePanel.m_Instance.EnableSpecificTool(BaseTool.ToolType.MarkovPenTool);
                }
            }
        }

        /// @brief Set the currently hovered panel button and updates its visual state
        /// @param buttonCollider The collider of the button that is currently hovered.
        public void SetHoveredButton(Collider buttonCollider)
        {
            if (m_HoveredButton == buttonCollider)
            {
                return;
            }

            SetButtonHoverVisual(m_HoveredButton, false);

            m_HoveredButton = buttonCollider;

            SetButtonHoverVisual(m_HoveredButton, true);
        }

        /// @brief Try to get the closest point on the drawing collider using a ray
        /// @param ray The ray used to test the drawing collider.
        /// @param worldPoint The resulting world-space point on the drawing collider.
        /// @return True if the ray hit the drawing collider.
        public bool TryGetClosestPanelPoint(Ray ray, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;

            float closestDistance = float.MaxValue;

            return TryUpdateClosestColliderHit(
                m_DrawingCollider,
                ray,
                ref closestDistance,
                ref worldPoint);
        }

        /// @brief Try to get a drawing point from the drawing collider using a ray
        /// @param ray The ray used to test the drawing collider.
        /// @param point2D The resulting local two-dimensional point on the drawing panel.
        /// @param worldPoint The resulting world-space point on the drawing panel.
        /// @return True if the ray hit the drawing collider.
        public bool TryGetDrawingPoint(
            Ray ray,
            out Vector2 point2D,
            out Vector3 worldPoint)
        {
            point2D = Vector2.zero;
            worldPoint = Vector3.zero;

            if (m_DrawingCollider == null ||
                !m_DrawingCollider.Raycast(ray, out RaycastHit raycastHit, k_RaycastMaxDistance))
            {
                return false;
            }

            worldPoint = raycastHit.point;

            Vector3 panelSpacePoint =
                Quaternion.Inverse(transform.rotation) * (worldPoint - transform.position);
            point2D = new Vector2(panelSpacePoint.x, panelSpacePoint.y);

            return true;
        }

        /// @brief Try to get the closest button collider hit by a ray
        /// @param ray The ray used to test the button colliders.
        /// @param worldPoint The resulting world-space point on the button collider.
        /// @return The closest hit button collider, or null if no button was hit.
        public Collider TryGetButtonPoint(Ray ray, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;

            Collider closestButtonCollider = null;
            float closestDistance = float.MaxValue;

            if (TryUpdateClosestColliderHit(
                m_SaveButtonCollider,
                ray,
                ref closestDistance,
                ref worldPoint))
            {
                closestButtonCollider = m_SaveButtonCollider;
            }

            if (TryUpdateClosestColliderHit(
                m_CloseButtonCollider,
                ray,
                ref closestDistance,
                ref worldPoint))
            {
                closestButtonCollider = m_CloseButtonCollider;
            }

            return closestButtonCollider;
        }

        /// @brief Try to update the closest raycast hit for a collider
        /// @param collider The collider to test.
        /// @param ray The ray used for the collider test.
        /// @param closestDistance The current closest hit distance.
        /// @param worldPoint The current closest world-space hit point.
        /// @return True if the collider produced a closer hit.
        private bool TryUpdateClosestColliderHit(
            Collider collider,
            Ray ray,
            ref float closestDistance,
            ref Vector3 worldPoint)
        {
            if (collider == null ||
                !collider.Raycast(ray, out RaycastHit raycastHit, k_RaycastMaxDistance))
            {
                return false;
            }

            if (raycastHit.distance >= closestDistance)
            {
                return false;
            }

            closestDistance = raycastHit.distance;
            worldPoint = raycastHit.point;

            return true;
        }

        /// @brief Update the hover scale for a panel button
        /// @param buttonCollider The collider of the button to update.
        /// @param isHovered True when the button is hovered.
        private void SetButtonHoverVisual(Collider buttonCollider, bool isHovered)
        {
            if (buttonCollider == m_SaveButtonCollider && m_SaveButtonHoverTarget != null)
            {
                m_SaveButtonHoverTarget.localScale = isHovered
                    ? m_SaveButtonBaseScale * m_HoverScale
                    : m_SaveButtonBaseScale;
            }
            else if (buttonCollider == m_CloseButtonCollider && m_CloseButtonHoverTarget != null)
            {
                m_CloseButtonHoverTarget.localScale = isHovered
                    ? m_CloseButtonBaseScale * m_HoverScale
                    : m_CloseButtonBaseScale;
            }
        }

        /// @brief Apply the configured panel size without accumulating scale changes
        private void ApplyPanelScale()
        {
            transform.localScale = m_InitialLocalScale * m_SizeMultiplier;
        }

        /// @brief Handle a press on a Markov drawing panel button
        /// @param buttonCollider The collider of the pressed button.
        public void OnButtonPressed(Collider buttonCollider)
        {
            if (buttonCollider != m_SaveButtonCollider &&
                buttonCollider != m_CloseButtonCollider)
            {
                return;
            }
            s_IsOpen = false;
            if (buttonCollider == m_CloseButtonCollider)
            {
                MarkovPenDrawingFreepaint.RestorePaintPointListsFromBackup();
            }
            else if (buttonCollider == m_SaveButtonCollider)
            {
                m_IsSaved = true;
            }

            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.EnableLine(false);
                PointerManager.m_Instance.PointerPressure = 0.0f;
                PointerManager.m_Instance.EatLineEnabledInput();
            }

            MarkovPenSketchMemoryScript.DeleteNewMarkovStrokes();

            if (PanelManager.m_Instance != null)
            {
                PanelManager.m_Instance.HidePanel(Type);
            }
        }
    }
}
