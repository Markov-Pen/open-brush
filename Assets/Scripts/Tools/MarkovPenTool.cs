// Copyright 2020 The Open Brush Authors
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TiltBrush
{

    /// @brief Drawing tool that synthesizes free-hand curve styles along arbitrary base paths
    ///        Until the Markov synthesis is implemented every method delegates to FreePaintTool,
    ///        so the tool behaves identically to plain free-hand drawing.
    /// @note in future versions it will be using a Markov Chain or an autoregressive hidden Markov Model
    /// 
    public class MarkovPenTool : FreePaintTool
    {

        private MarkovPen m_MarkovPen = null;

        Tuple<Vector3, Quaternion> m_LastPointer = new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);

        private int maxPoints = 50;
        /// @brief Initialise the tool and all Markov model data structures.
        public override void Init()
        {
            base.Init();

            //Debug.Log("Init");
        }

        /// @brief Activate or deactivate the Markov Pen tool
        /// @param isEnabled true to activate the tool; false to deactivate it.
        public override void EnableTool(bool isEnabled)
        {
            base.EnableTool(isEnabled);

            if (!isEnabled)
            {
                return;
            }

            List<Vector3> baseCurvePoints = MarkovPenDrawingFreepaint.BaseCurvePoints;
            List<Vector3> styleCurvePoints = MarkovPenDrawingFreepaint.StyleCurvePoints;
            if (baseCurvePoints == null || styleCurvePoints == null ||
                baseCurvePoints.Count == 0 || styleCurvePoints.Count == 0)
            {
                return;
            }

            CreateMarkovPen(baseCurvePoints, styleCurvePoints);

        }
        /// @brief Reduces a curve point list to a maximum number of points while preserving the first point, last point, and important local peaks.
        /// @param points The original list of curve points that should be reduced.
        /// @param maxPointCount The maximum number of points that should remain in the reduced curve.
        /// @return A reduced list of curve points, ordered by their original position in the curve.
        private static List<Vector3> ReduceCurvePoints(List<Vector3> points, int maxPointCount)
        {
            if (points.Count <= maxPointCount)
            {
                return new List<Vector3>(points);
            }

            if (maxPointCount == 1)
            {
                return new List<Vector3> { points[0] };
            }

            var selectedIndices = new HashSet<int>
            {
                0,
                points.Count - 1
            };

            var peaks = new List<(int Index, float Importance)>();

            for (int i = 1; i < points.Count - 1; i++)
            {
                float previousY = points[i - 1].y;
                float currentY = points[i].y;
                float nextY = points[i + 1].y;

                bool isHighPoint = currentY >= previousY && currentY >= nextY;
                bool isLowPoint = currentY <= previousY && currentY <= nextY;

                if (isHighPoint || isLowPoint)
                {
                    float importance;

                    if (isHighPoint)
                    {
                        importance = currentY - Mathf.Max(previousY, nextY);
                    }
                    else
                    {
                        importance = Mathf.Min(previousY, nextY) - currentY;
                    }

                    peaks.Add((i, importance));
                }
            }

            peaks.Sort((a, b) => b.Importance.CompareTo(a.Importance));

            foreach ((int index, _) in peaks)
            {
                if (selectedIndices.Count >= maxPointCount)
                {
                    break;
                }

                selectedIndices.Add(index);
            }

            while (selectedIndices.Count < maxPointCount)
            {
                var orderedIndices = selectedIndices.OrderBy(index => index).ToList();

                int bestIndex = -1;
                int largestGap = 0;

                for (int i = 0; i < orderedIndices.Count - 1; i++)
                {
                    int left = orderedIndices[i];
                    int right = orderedIndices[i + 1];

                    int gap = right - left;

                    if (gap > 1 && gap > largestGap)
                    {
                        largestGap = gap;

                        bestIndex = left + gap / 2;
                    }
                }

                if (bestIndex == -1)
                {
                    break;
                }

                selectedIndices.Add(bestIndex);
            }

            var result = new List<Vector3>(selectedIndices.Count);

            foreach (int index in selectedIndices.OrderBy(index => index))
            {
                result.Add(points[index]);
            }

            return result;
        }

        /// @brief Show or hide the tool's visual indicators
        /// @param isHidden true to hide the tool visuals; false to show them.
        /// @note  Later: indicates Up Vector? adds visual indicator for controller position
        public override void HideTool(bool isHidden)
        {
            base.HideTool(isHidden);
            //Debug.Log("Tool Hidden");
        }

        /// @brief Read controller input and drive synthesis
        ///
        /// Called every tool update tick.
        /// @note  Later: sample the target base path B from the controller position each frame,
        ///        run one step of DCMM synthesis to produce the next
        ///        point on B', and feed the result into the pointer manager.
        public override void UpdateTool()
        {

            if (m_MarkovPen == null)
            {
                base.UpdateTool();
                return;
            }

            bool triggerDown = InputManager.Brush.GetCommandDown(InputManager.SketchCommands.Activate);

            if (triggerDown)
            {
                m_MarkovPen.newLine();
                var start = base.GetPointerPosition();
                m_LastPointer = Tuple.Create(start.Item1, start.Item2);
            }

            List<Tuple<Vector3, Quaternion>> pointers = (m_MarkovPen.Reconstruct(base.GetPointerPosition()));

            if (pointers.Count > 0)
            {
                m_LastPointer = pointers.First();
                pointers.RemoveAt(0);
                // m_EatInput= false;
            }
            else
            {
                // m_EatInput= true;
            }

            base.UpdateTool();

            PointerScript pointer = PointerManager.m_Instance.MainPointer;

            while (pointers.Count > 0)
            {
                m_LastPointer = pointers[0];


                PointerManager.m_Instance.SetPointerTransform(
                    InputManager.ControllerName.Brush,
                    m_LastPointer.Item1,
                    m_LastPointer.Item2);

                if (pointer != null && pointer.IsCreatingStroke())
                {
                    pointer.UpdateLineFromObject();
                }

                pointers.RemoveAt(0);
            }
        }

        /// @brief Update pointer transforms
        /// 
        /// Called only on frames that UpdateTool() has been called.
        /// Guaranteed to be called after new poses have been received from OpenVR.
        public override void LateUpdateTool()
        {
            base.LateUpdateTool();
            // Debug.Log("LateUpdate");
        }


        /// @brief Return the world-space position and rotation for the brush pointer
        /// @returns A tuple of (position, rotation) in global space.
        protected override (Vector3, Quaternion) GetPointerPosition()
        {

            if (m_MarkovPen == null) return base.GetPointerPosition();

            return (m_LastPointer.Item1, m_LastPointer.Item2);
        }

        /// @brief Set the visual materials on the controller geometry to reflect tool state
        /// @param controller The controller whose materials should be updated.
        public override void AssignControllerMaterials(InputManager.ControllerName controller)
        {
            base.AssignControllerMaterials(controller);
        }


        /// @brief Adjust the brush size by the given delta
        /// @param adjustAmount Signed normalised adjustment amount.
        public override void UpdateSize(float adjustAmount)
        {
            base.UpdateSize(adjustAmount);
        }



        /// @brief Return the current brush size as a normalised [0, 1] value
        /// @returns Brush size in the [0, 1] range.
        public override float GetSize01()
        {
            return base.GetSize01();
        }

        /// @brief Return whether the brush size can currently be adjusted
        /// @returns true if size adjustment is allowed in the current application state.
        public override bool CanAdjustSize()
        {
            return base.CanAdjustSize();
        }

        /// @brief Create a new Markov pen
        /// 
        /// @param basePath - Control Points of the given example Base Path
        /// @param styleCurve - Control points of the given example Style Curve
        public void CreateMarkovPen(List<Vector3> basePath, List<Vector3> styleCurve)
        {
            m_MarkovPen = new MarkovPen(basePath, styleCurve);
        }
    }
}
