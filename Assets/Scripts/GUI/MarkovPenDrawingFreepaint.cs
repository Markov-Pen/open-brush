using System.Collections.Generic;
using OpenBrush.Multiplayer;
using UnityEngine;

namespace TiltBrush
{
    /// @brief Provides a Markov drawing tool that paints only on the Markov drawing panel
    /// Stores drawn points, separates base curve and style curve points, and manages
    /// pointer state while the drawing panel is active.
    public class MarkovPenDrawingFreepaint : FreePaintTool
    {
        /// @brief Progress of the Markov drawing on the drawing panel
        private enum DrawingState
        {
            Started,
            DrawingBaseCurve,
            DoneBaseCurve,
            DrawingStyleCurve,
            Finished
        }

        private const float k_MinDirectionIndicatorLength = 0.001f;
        private const float k_MinDirectionIndicatorLengthSquared =
            k_MinDirectionIndicatorLength * k_MinDirectionIndicatorLength;
        private const string k_GuideLineObjectName = "GuideLine";

        [SerializeField] private LineRenderer m_DirectionIndicator;
        [SerializeField, Min(0.0f)] private float m_DirectionIndicatorPanelOffset = 0.03f;
        [SerializeField, Min(0.0f)] private float m_DirectionIndicatorControllerOffset = 0.02f;

        private static readonly List<Vector3> s_ControlPoints = new();
        private static readonly List<Vector3> s_BaseCurvePoints = new();
        private static readonly List<Vector3> s_StyleCurvePoints = new();

        private static readonly List<Vector3> s_BackupControlPoints = new();
        private static readonly List<Vector3> s_BackupBaseCurvePoints = new();
        private static readonly List<Vector3> s_BackupStyleCurvePoints = new();

        private static readonly List<GameObject> s_HiddenGuideLines = new();

        private static readonly Color s_BaseCurveColor = ParseHexColor("#ffffff");
        private static readonly Color s_StyleCurveColor = ParseHexColor("#0090da");

        private static DrawingState s_DrawingState = DrawingState.Started;
        private static Color s_PointerColorBeforeMarkovDrawing = Color.white;

        private static bool s_WasButtonPressed;
        private static bool s_IsWaitingForFirstTriggerRelease;
        private static bool s_HasSavedPointerColor;

        /// @brief Get all active points drawn on the Markov drawing panel
        public static IReadOnlyList<Vector3> ControlPoints => s_ControlPoints;

        /// @brief Get all active points belonging to the base curve
        public static List<Vector3> BaseCurvePoints => s_BaseCurvePoints;

        /// @brief Get all active points belonging to the style curve
        public static List<Vector3> StyleCurvePoints => s_StyleCurvePoints;

        /// @brief Get the backed-up drawing points from the last saved Markov drawing
        public static IReadOnlyList<Vector3> BackupControlPoints => s_BackupControlPoints;

        /// @brief Get the backed-up base curve points from the last saved Markov drawing
        public static IReadOnlyList<Vector3> BackupBaseCurvePoints => s_BackupBaseCurvePoints;

        /// @brief Get the backed-up style curve points from the last saved Markov drawing
        public static IReadOnlyList<Vector3> BackupStyleCurvePoints => s_BackupStyleCurvePoints;

        /// @brief Get whether the Markov drawing panel is currently open and available
        private static bool IsDrawingPanelOpen =>
            MarkovPenDrawingPanel.IsOpen &&
            MarkovPenDrawingPanel.Instance != null;

        /// @brief Get whether a base curve or style curve stroke is currently being drawn
        private static bool IsDrawingCurve =>
            s_DrawingState == DrawingState.DrawingBaseCurve ||
            s_DrawingState == DrawingState.DrawingStyleCurve;

        /// @brief Update the tool and redirects painting input to the Markov drawing panel
        public override void UpdateTool()
        {
            base.UpdateTool();

            if (!IsDrawingPanelOpen)
            {
                SetDirectionIndicatorActive(false);
                return;
            }

            ApplyMarkovPanelPaintingOverride();
        }

        /// @brief Reset drawing state when the Markov drawing panel is opened
        /// Clears active point lists and waits for the first trigger release before drawing is allowed.
        public static void OnPanelOpened()
        {
            ClearPaintPointLists();
            ResetInteractionState(waitForFirstTriggerRelease: true);

            HideGuideLines();
            ResetPointer();
        }

        /// @brief Reset interaction state when the Markov drawing panel is closed
        /// Keeps backup point lists unchanged so they can be used after closing the panel.
        public static void OnPanelClosed()
        {
            RestorePointerColorIfNeeded();
            ResetInteractionState(waitForFirstTriggerRelease: false);

            RestoreGuideLines();
            ResetPointer();
        }

        /// @brief Copy the active point lists into the backup point lists
        public static void BackupPaintPointLists()
        {
            CopyPoints(s_ControlPoints, s_BackupControlPoints);
            CopyPoints(s_BaseCurvePoints, s_BackupBaseCurvePoints);
            CopyPoints(s_StyleCurvePoints, s_BackupStyleCurvePoints);
        }

        /// @brief Restore the active point lists from the backup point lists
        public static void RestorePaintPointListsFromBackup()
        {
            CopyPoints(s_BackupControlPoints, s_ControlPoints);
            CopyPoints(s_BackupBaseCurvePoints, s_BaseCurvePoints);
            CopyPoints(s_BackupStyleCurvePoints, s_StyleCurvePoints);
        }

        /// @brief Clear all active points stored for the Markov drawing panel
        public static void ClearPaintPointLists()
        {
            s_ControlPoints.Clear();
            s_BaseCurvePoints.Clear();
            s_StyleCurvePoints.Clear();
        }

        /// @brief Reset pointer state and stops any active line drawing
        public static void ResetPointer()
        {
            PointerManager pointerManager = PointerManager.m_Instance;

            if (pointerManager == null)
            {
                return;
            }

            pointerManager.StraightEdgeModeEnabled = false;
            pointerManager.EnableLine(false);
            pointerManager.PointerPressure = 0.0f;
            pointerManager.EatLineEnabledInput();
        }

        /// @brief Parse an HTML hexadecimal color string
        /// @param hexColorString The hexadecimal color string, for example "#11bb72".
        /// @return The parsed color, or white if parsing fails.
        private static Color ParseHexColor(string hexColorString)
        {
            return ColorUtility.TryParseHtmlString(hexColorString, out Color parsedColor)
                ? parsedColor
                : Color.white;
        }

        private static void ResetInteractionState(bool waitForFirstTriggerRelease)
        {
            s_WasButtonPressed = false;
            s_IsWaitingForFirstTriggerRelease = waitForFirstTriggerRelease;
            s_DrawingState = DrawingState.Started;
        }

        private static void CopyPoints(List<Vector3> source, List<Vector3> destination)
        {
            destination.Clear();
            destination.AddRange(source);
        }

        /// @brief Disable all active scene guide lines and remember them for restoring
        private static void HideGuideLines()
        {
            RestoreGuideLines();

            foreach (Transform currentTransform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (currentTransform == null ||
                    currentTransform.name != k_GuideLineObjectName)
                {
                    continue;
                }

                GameObject guideLineObject = currentTransform.gameObject;

                if (!guideLineObject.scene.IsValid() || !guideLineObject.activeSelf)
                {
                    continue;
                }

                guideLineObject.SetActive(false);
                s_HiddenGuideLines.Add(guideLineObject);
            }
        }

        /// @brief Re-enable the guide lines that were disabled when the panel opened
        private static void RestoreGuideLines()
        {
            foreach (GameObject guideLineObject in s_HiddenGuideLines)
            {
                if (guideLineObject != null)
                {
                    guideLineObject.SetActive(true);
                }
            }

            s_HiddenGuideLines.Clear();
        }

        /// @brief Restore the pointer color active before Markov drawing started
        private static void RestorePointerColorIfNeeded()
        {
            PointerManager pointerManager = PointerManager.m_Instance;

            if (pointerManager == null || !s_HasSavedPointerColor)
            {
                return;
            }

            pointerManager.PointerColor = s_PointerColorBeforeMarkovDrawing;
            s_HasSavedPointerColor = false;
        }

        /// @brief Set the current pointer color for Markov drawing
        /// Saves the original pointer color first and only applies changed colors,
        /// because every color change notifies all pointer color listeners.
        /// @param pointerManager The active pointer manager.
        /// @param color The color to apply to the pointer.
        private static void SetPointerColor(PointerManager pointerManager, Color color)
        {
            if (!s_HasSavedPointerColor)
            {
                s_PointerColorBeforeMarkovDrawing = pointerManager.PointerColor;
                s_HasSavedPointerColor = true;
            }

            if (pointerManager.PointerColor != color)
            {
                pointerManager.PointerColor = color;
            }
        }

        /// @brief Finish the style curve stroke and restore normal pointer state
        private static void FinishStyleCurve()
        {
            if (s_DrawingState != DrawingState.DrawingStyleCurve)
            {
                return;
            }

            s_DrawingState = DrawingState.Finished;
            ResetPointer();
            RestorePointerColorIfNeeded();
        }

        /// @brief Save a drawn point and assigns it to the active curve
        /// @param panelPoint The panel-space point drawn on the Markov drawing panel.
        private static void SavePaintPoint(Vector2 panelPoint)
        {
            Vector3 point = new Vector3(panelPoint.x, panelPoint.y, 0.0f);

            switch (s_DrawingState)
            {
                case DrawingState.DrawingBaseCurve:
                    s_BaseCurvePoints.Add(point);
                    break;

                case DrawingState.DrawingStyleCurve:
                    s_StyleCurvePoints.Add(point);
                    break;

                default:
                    return;
            }

            s_ControlPoints.Add(point);
        }

        private static bool IsButtonPressAllowed(
            MarkovPenDrawingPanel drawingPanel,
            Collider buttonCollider)
        {
            if (buttonCollider == drawingPanel.CloseButtonCollider)
            {
                return !IsDrawingCurve;
            }

            if (buttonCollider == drawingPanel.SaveButtonCollider)
            {
                return s_DrawingState == DrawingState.Finished;
            }

            return false;
        }

        private static bool IsApplicationPaintingAllowed()
        {
            App application = App.Instance;
            return application != null && application.IsInStateThatAllowsPainting();
        }

        private static bool IsViewOnlyMultiplayer()
        {
            MultiplayerManager multiplayerManager = MultiplayerManager.m_Instance;
            return multiplayerManager != null && multiplayerManager.IsViewOnly;
        }

        /// @brief Update the direction indicator between the controller and drawing panel
        /// @param drawingPanel The active Markov drawing panel.
        /// @param ray The ray extending from the brush controller.
        private void UpdateDirectionIndicator(MarkovPenDrawingPanel drawingPanel, Ray ray)
        {
            if (m_DirectionIndicator == null)
            {
                return;
            }

            if (ray.direction.sqrMagnitude < k_MinDirectionIndicatorLengthSquared ||
                !drawingPanel.TryGetClosestPanelPoint(ray, out Vector3 panelHitPoint))
            {
                SetDirectionIndicatorActive(false);
                return;
            }

            float controllerOffset = Mathf.Max(0.0f, m_DirectionIndicatorControllerOffset);
            float panelOffset = Mathf.Max(0.0f, m_DirectionIndicatorPanelOffset);

            float hitDistance = Vector3.Distance(ray.origin, panelHitPoint);
            float lineLength = hitDistance - controllerOffset - panelOffset;

            if (lineLength <= k_MinDirectionIndicatorLength)
            {
                SetDirectionIndicatorActive(false);
                return;
            }

            Vector3 normalizedDirection = ray.direction.normalized;
            Vector3 startPoint = ray.origin + normalizedDirection * controllerOffset;
            Vector3 endPoint = startPoint + normalizedDirection * lineLength;

            m_DirectionIndicator.positionCount = 2;
            m_DirectionIndicator.useWorldSpace = true;
            m_DirectionIndicator.SetPosition(0, startPoint);
            m_DirectionIndicator.SetPosition(1, endPoint);

            SetDirectionIndicatorActive(true);
        }

        /// @brief Enable or disable the direction indicator
        /// @param isActive True when the direction indicator should be visible.
        private void SetDirectionIndicatorActive(bool isActive)
        {
            if (m_DirectionIndicator != null)
            {
                m_DirectionIndicator.enabled = isActive;
            }
        }

        /// @brief Enable or disable drawing on the pointer
        /// @param isActive True when drawing should be active.
        private void SetDrawingActive(bool isActive)
        {
            PointerManager pointerManager = PointerManager.m_Instance;

            if (pointerManager == null)
            {
                return;
            }

            pointerManager.EnableLine(isActive);
            pointerManager.PointerPressure = isActive
                ? Mathf.Clamp01(m_brushTriggerRatio)
                : 0.0f;
        }

        /// @brief Disable drawing and update the curve state without active painting
        private void StopDrawing()
        {
            SetDrawingActive(false);
            UpdateCurveState(false);
        }

        /// @brief Update the active curve state
        /// Handles the transition from base curve to style curve and disables drawing afterwards.
        /// @param isPaintingActive True when the user is painting on the drawing panel.
        private void UpdateCurveState(bool isPaintingActive)
        {
            PointerManager pointerManager = PointerManager.m_Instance;

            if (pointerManager == null)
            {
                return;
            }

            switch (s_DrawingState)
            {
                case DrawingState.Started:
                    if (isPaintingActive)
                    {
                        s_DrawingState = DrawingState.DrawingBaseCurve;
                        pointerManager.StraightEdgeModeEnabled = true;
                        SetPointerColor(pointerManager, s_BaseCurveColor);
                    }
                    break;

                case DrawingState.DrawingBaseCurve:
                    if (!m_brushTrigger)
                    {
                        s_DrawingState = DrawingState.DoneBaseCurve;
                        pointerManager.StraightEdgeModeEnabled = false;
                        pointerManager.EatLineEnabledInput();
                    }
                    else if (isPaintingActive)
                    {
                        pointerManager.StraightEdgeModeEnabled = true;
                        SetPointerColor(pointerManager, s_BaseCurveColor);
                    }
                    break;

                case DrawingState.DoneBaseCurve:
                    pointerManager.StraightEdgeModeEnabled = false;

                    if (isPaintingActive)
                    {
                        s_DrawingState = DrawingState.DrawingStyleCurve;
                        SetPointerColor(pointerManager, s_StyleCurveColor);
                    }
                    break;

                case DrawingState.DrawingStyleCurve:
                    pointerManager.StraightEdgeModeEnabled = false;

                    if (!m_brushTrigger)
                    {
                        FinishStyleCurve();
                    }
                    else if (isPaintingActive)
                    {
                        SetPointerColor(pointerManager, s_StyleCurveColor);
                    }
                    break;

                case DrawingState.Finished:
                    pointerManager.StraightEdgeModeEnabled = false;
                    break;
            }
        }

        /// @brief Override normal painting behavior while the Markov drawing panel is open
        /// Redirects the brush pointer onto panel colliders, handles button interaction,
        /// and stores points while painting is active.
        private void ApplyMarkovPanelPaintingOverride()
        {
            MarkovPenDrawingPanel drawingPanel = MarkovPenDrawingPanel.Instance;
            PointerManager pointerManager = PointerManager.m_Instance;
            InputManager inputManager = InputManager.m_Instance;
            Transform attachTransform = inputManager != null
                ? inputManager.GetBrushControllerAttachPoint()
                : null;

            if (drawingPanel == null ||
                pointerManager == null ||
                attachTransform == null)
            {
                SetDirectionIndicatorActive(false);
                StopDrawing();
                return;
            }

            Ray ray = new Ray(attachTransform.position, attachTransform.forward);
            UpdateDirectionIndicator(drawingPanel, ray);

            if (s_IsWaitingForFirstTriggerRelease)
            {
                HandleInitialTriggerRelease();
                return;
            }

            // Panel buttons are blocked while a curve is being drawn.
            if (!IsDrawingCurve)
            {
                Collider buttonCollider =
                    drawingPanel.TryGetButtonPoint(ray, out Vector3 buttonWorldPoint);

                drawingPanel.SetHoveredButton(buttonCollider);

                if (buttonCollider != null)
                {
                    HandleButtonHover(drawingPanel, pointerManager, buttonCollider, buttonWorldPoint);
                    return;
                }

                if (!m_brushTrigger)
                {
                    s_WasButtonPressed = false;
                }
            }

            if (!drawingPanel.TryGetDrawingPoint(
                ray,
                out Vector2 panelPoint,
                out Vector3 drawingWorldPoint))
            {
                FinishStyleCurve();
                StopDrawing();
                return;
            }

            pointerManager.SetPointerTransform(
                InputManager.ControllerName.Brush,
                drawingWorldPoint,
                drawingPanel.transform.rotation);

            bool isPaintingActive =
                m_brushTrigger &&
                s_DrawingState != DrawingState.Finished &&
                IsApplicationPaintingAllowed() &&
                !IsViewOnlyMultiplayer();

            UpdateCurveState(isPaintingActive);

            // The state machine must have an active stroke before drawing is enabled.
            bool shouldDraw = isPaintingActive && IsDrawingCurve;

            SetDrawingActive(shouldDraw);

            if (shouldDraw)
            {
                SavePaintPoint(panelPoint);
            }
        }

        private void HandleInitialTriggerRelease()
        {
            StopDrawing();

            if (m_brushTrigger)
            {
                return;
            }

            s_IsWaitingForFirstTriggerRelease = false;
            s_WasButtonPressed = false;
            ResetPointer();
        }

        private void HandleButtonHover(
            MarkovPenDrawingPanel drawingPanel,
            PointerManager pointerManager,
            Collider buttonCollider,
            Vector3 buttonWorldPoint)
        {
            pointerManager.SetPointerTransform(
                InputManager.ControllerName.Brush,
                buttonWorldPoint,
                drawingPanel.transform.rotation);

            StopDrawing();

            if (!m_brushTrigger)
            {
                s_WasButtonPressed = false;
                return;
            }

            if (s_WasButtonPressed || !IsButtonPressAllowed(drawingPanel, buttonCollider))
            {
                return;
            }

            s_WasButtonPressed = true;
            drawingPanel.OnButtonPressed(buttonCollider);
        }
    }
}
