using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// @brief Provides the drawing panel used by the Markov pen drawing tool
    /// Handles panel lifetime state and drawing collider raycasts.
    public class MarkovPenDrawingPanel : BasePanel
    {
        private const float k_RaycastMaxDistance = 100.0f;

        private static MarkovPenDrawingPanel s_Instance;
        private static bool s_IsOpen;

        [Header("Markov Panel Colliders")]
        [SerializeField]
        private Collider m_DrawingCollider;

        [Header("Panel Scale")]
        [SerializeField]
        private float m_SizeMultiplier = 0.85f;

        private Vector3 m_InitialLocalScale;

        /// @brief Get the active Markov drawing panel instance
        public static MarkovPenDrawingPanel Instance => s_Instance;

        /// @brief Get whether the Markov drawing panel is currently open
        public static bool IsOpen => s_IsOpen;

        /// @brief Get the collider used for drawing input
        public Collider DrawingCollider => m_DrawingCollider;

        /// @brief Initialize the panel instance and stores the initial visual state
        protected override void Awake()
        {
            base.Awake();

            s_Instance = this;
            m_InitialLocalScale = transform.localScale;
        }

        /// @brief Activate the panel and prepares the Markov drawing tool
        protected override void OnEnablePanel()
        {
            base.OnEnablePanel();

            s_IsOpen = true;

            MarkovPenSketchMemoryScript.BeginMarkovStrokeCapture();
            ApplyPanelScale();
            MarkovPenDrawingFreepaint.OnPanelOpened();
        }

        /// @brief Deactivate the panel and resets the Markov drawing tool state
        /// Creates a new Markov pen from the active point lists if both curves contain points.
        protected override void OnDisablePanel()
        {
            base.OnDisablePanel();

            s_IsOpen = false;

            MarkovPenDrawingFreepaint.OnPanelClosed();

            List<Vector3> baseCurvePoints = MarkovPenDrawingFreepaint.BaseCurvePoints;
            List<Vector3> styleCurvePoints = MarkovPenDrawingFreepaint.StyleCurvePoints;

            if (baseCurvePoints.Count == 0 || styleCurvePoints.Count == 0)
            {
                return;
            }

            MarkovPenTool.CreateMarkovPen(baseCurvePoints, styleCurvePoints);

            if (SketchSurfacePanel.m_Instance != null)
            {
                SketchSurfacePanel.m_Instance.EnableSpecificTool(BaseTool.ToolType.MarkovPenTool);
            }
        }

        /// @brief Stop the panel painting override before the panel is hidden
        public void PrepareForClose()
        {
            s_IsOpen = false;
        }

        /// @brief Try to get the closest point on the drawing collider using a ray
        /// @param ray The ray used to test the drawing collider.
        /// @param worldPoint The resulting world-space point on the drawing collider.
        /// @return True if the ray hit the drawing collider.
        public bool TryGetClosestPanelPoint(Ray ray, out Vector3 worldPoint)
        {
            if (!TryRaycast(m_DrawingCollider, ray, out RaycastHit raycastHit))
            {
                worldPoint = Vector3.zero;
                return false;
            }

            worldPoint = raycastHit.point;
            return true;
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
            if (!TryGetClosestPanelPoint(ray, out worldPoint))
            {
                point2D = Vector2.zero;
                return false;
            }

            Vector3 panelSpacePoint =
                Quaternion.Inverse(transform.rotation) * (worldPoint - transform.position);
            point2D = new Vector2(panelSpacePoint.x, panelSpacePoint.y);

            return true;
        }

        /// @brief Raycast a single collider within the maximum raycast distance
        /// @param collider The collider to test.
        /// @param ray The ray used for the collider test.
        /// @param raycastHit The resulting raycast hit.
        /// @return True if the collider was hit.
        private static bool TryRaycast(Collider collider, Ray ray, out RaycastHit raycastHit)
        {
            if (collider == null)
            {
                raycastHit = default;
                return false;
            }

            return collider.Raycast(ray, out raycastHit, k_RaycastMaxDistance);
        }

        /// @brief Apply the configured panel size without accumulating scale changes
        private void ApplyPanelScale()
        {
            transform.localScale = m_InitialLocalScale * m_SizeMultiplier;
        }
    }
}
