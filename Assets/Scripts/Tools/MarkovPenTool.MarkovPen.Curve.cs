// Copyright 2026 Marvin Link, Katrin Lang, Artur Meshalkin
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
    public partial class MarkovPen
    {
        /// @class Curve
        /// @brief Catmull-Rom spline class
        ///
        /// The Curve class provides basic spline functionalities: adding control points, computing arc length positions, and evaluating positions and derivatives.
        public class Curve
        {
            protected List<float> m_ArcLengthPositions;
            protected List<Vector3> m_ControlPoints = new();

            private Vector3 m_LastInput = Vector3.zero;

            private const float k_Responsiveness = 1f;

            private readonly float m_Responsiveness;

            /// @brief Construct an empty Curve instance
            ///
            /// Creates a target curve with initially no knots
            ///
            /// @param responsiveness Responsiveness parameter controlling Elasticurve smoothing (default: 1 = no smoothing).
            public Curve(float responsiveness = k_Responsiveness)
            {
                m_Responsiveness = responsiveness;
                m_ArcLengthPositions = new List<float> { 0f };
            }
            
            /// @brief Construct a Curve instance from a set of control points
            /// 
            /// Creates a fully initialized example curve
            ///
            /// @params controlPoints - Constrol points of the curve from OpenBrush
            public Curve(List<Vector3> controlPoints): this()
            {
                foreach (var point in controlPoints)
                {
                    AddControlPoint(point);
                }
                Finish();
            }

            /// @brief Check if the curve has been finalized
            ///
            /// @return True if the curve is finished, otherwise false.
            public virtual bool IsFinished()
            {
                return m_ControlPoints.Count >= 2 &&
                    m_ArcLengthPositions.Count == m_ControlPoints.Count;
            }

            /// @brief Add a control point to the curve and performs necessary updates.
            /// 
            /// @param controlPoint The new control point to be added.
            public void AddControlPoint(Vector3 controlPoint)
            {
                // If the curve is empty, the control point is directly added
                if (m_ControlPoints.Count == 0)
                {
                    m_ControlPoints.Add(controlPoint);
                    return;
                }

                // If we have one control point already and another is added,
                // just record the added point
                if (m_LastInput == Vector3.zero)
                {
                    m_LastInput = controlPoint;
                    return;
                }

                // Elasticurve (Yannick Thiel, Karan Singh, and Ravin Balakrishnan. 2011) 
                // implementation adapted for Catmull-Rom splines
                Vector3[] segment =
                {
                    m_ControlPoints[m_ControlPoints.Count > 1 ? ^2 : ^1],
                    m_ControlPoints[^1],
                    m_LastInput,
                    controlPoint
                };
                Vector3 interpolatedPoint= Interpolate(segment, m_Responsiveness);

                m_LastInput = controlPoint;

                if (Vector3.Distance(segment[1], interpolatedPoint) < 0.1)
                {
                    return;
                }

                m_ControlPoints.Add(interpolatedPoint);

                if (m_ControlPoints.Count >= 3)
                {
                    m_ArcLengthPositions.Add(
                        m_ArcLengthPositions.Last() +
                        ComputeArcLength(m_ControlPoints.Count - 3));
                }
            }

            /// @brief Retrieve the total arc length of the entire curve
            ///
            /// @return The total arc length of the curve.
            public virtual float ArcLength()
            {
                return m_ArcLengthPositions.Last();
            }

            /// @brief Evaluate 3D point at given arc length position
            ///
            /// Retrieves the curve point corresponding to the given arc length position.
            /// Returns extrapolated positions when l is outside the curve range.
            ///
            /// @param l  Arc length position (may be negative or beyond the arc length of the curve)
            /// @return 3D Point at the specified arc length position
            public Vector3 PositionAt(float l)
            {
                if (l < 0)
                {
                    Vector3[] firstSegment = GetSegmentPositions(0);
                    Vector3 tangent = ComputeTangentsAtEndpoints(firstSegment).Item1;

                    return m_ControlPoints[0] + l * tangent.normalized;
                }

                if (l >= m_ArcLengthPositions.Last())
                {
                    Vector3[] lastSegment = GetSegmentPositions(m_ControlPoints.Count-2);
                    Vector3 tangent = ComputeTangentsAtEndpoints(lastSegment).Item2;

                    return m_ControlPoints[^1] +
                           (l - m_ArcLengthPositions.Last()) * tangent.normalized;
                }

                float t = TimeAt(l);

                Vector3[] segment = GetSegmentPositions(SegmentIndex(t));

                return Interpolate(segment, SegmentTime(t));
            }

            /// @brief Evaluate first derivative at given arc length position
            ///
            /// Returns the tangent vector at the specified arc length position.
            ///
            /// @param l Arc length position (may be negative or beyond the arc length of the base path).
            /// @return Derivative vector at the specified position.
            public Vector3 FirstDerivativeAt(float l)
            {
                if (l <= 0)
                {
                    Vector3[] firstSegment = GetSegmentPositions(0);
                    return ComputeTangentsAtEndpoints(firstSegment).Item1;
                }

                if (l >= m_ArcLengthPositions.Last())
                {
                    Vector3[] lastSegment = GetSegmentPositions(m_ControlPoints.Count - 2);
                    return ComputeTangentsAtEndpoints(lastSegment).Item2;
                }

                float t = TimeAt(l);

                Vector3[] segment = GetSegmentPositions(SegmentIndex(t));

                return EvaluateFirstDerivative(segment, SegmentTime(t));
            }

            /// @brief Get an array of four 3D positions making up the segment at the given index
            ///
            /// @param index The segment index (index of the second control point).
            /// @return An array of four 3D positions.
            protected Vector3[] GetSegmentPositions(int index)
            {
                Vector3 point1 = m_ControlPoints[index == 0 ? index : index - 1];
                Vector3 point2 = m_ControlPoints[index];
                Vector3 point3 = m_ControlPoints[index + 1];
                Vector3 point4 = m_ControlPoints[index == m_ControlPoints.Count - 2 ? index + 1 : index + 2];

                return new[] { point1, point2, point3, point4 };
            }

            /// @brief Perform cubic Hermite interpolation to compute the position at the specified time value.
            ///
            /// @param segment The segment consisting of four control points
            /// @param t The time value for interpolation ranging between 0 and 1.
            /// @return The interpolated 3D point.
            public Vector3 Interpolate(Vector3[] segment, float t)
            {
                (Vector3, Vector3) tangents = ComputeTangentsAtEndpoints(segment);

                // Hermite basis functions
                float h1 = (float)(2 * Math.Pow(t, 3) - 3 * Math.Pow(t, 2) + 1);
                float h2 = (float)((-2) * Math.Pow(t, 3) + 3 * Math.Pow(t, 2));
                float h3 = (float)(Math.Pow(t, 3) - 2 * Math.Pow(t, 2) + t);
                float h4 = (float)(Math.Pow(t, 3) - Math.Pow(t, 2));

                return
                    h1 * segment[1] +
                    h2 * segment[2] +
                    h3 * tangents.Item1 +
                    h4 * tangents.Item2;
            }

            /// @brief Perform cubic Hermite interpolation to compute the first derivative at the specified time value.
            //
            /// @param segment The segment consisting of four control points
            /// @param t The time value for interpolation ranging between 0 and 1.
            /// @return The interpolated first derivative
            public static Vector3 EvaluateFirstDerivative(Vector3[] segment, float t)
            {

                (Vector3, Vector3) tangents = ComputeTangentsAtEndpoints(segment);

                // First derivatives of hermite basis functions
                float h1 = (float)(6 * Math.Pow(t, 2) - 6 * Math.Pow(t, 1));
                float h2 = (float)((-6) * Math.Pow(t, 2) + 6 * Math.Pow(t, 1));
                float h3 = (float)(3 * Math.Pow(t, 2) - 4 * Math.Pow(t, 1) + 1);
                float h4 = (float)(3 * Math.Pow(t, 2) - 2 * Math.Pow(t, 1));

                return
                    h1 * segment[1] +
                    h2 * segment[2] +
                    h3 * tangents.Item1 +
                    h4 * tangents.Item2;
            }

            /// @brief Compute the tangent vectors at the endpoints of a segment
            ///
            /// @param segment The segment consisting of four control points.
            /// @return The computed tangent vectors.
            protected static (Vector3, Vector3) ComputeTangentsAtEndpoints(Vector3[] segment)
            {
                return ((segment[2] - segment[0]) / 2, (segment[3] - segment[1]) / 2);
            }

            /// @brief Compute the local time parameter within a curve segment based on the global one
            /// @param t The global time value.
            /// @return The local time value within the curve segment (ranging between 0 and 1).
            protected float SegmentTime(float t)
            {
                return t % 1;
            }

            /// @brief Compute the integer segment index from the given global time value.
            /// 
            /// @param t The global time value.
            /// @return The rounded down integer value of 't'.
            protected int SegmentIndex(float t)
            {
                return Mathf.FloorToInt(t);
            }

            /// @brief Convert arc length position to time value
            ///
            /// Computes the time value corresponding to a given arc length position.
            ///
            /// @param l Specified arc length position.
            /// @return Time value corresponding to arc length position.
            protected float TimeAt(float l)
            {
                int i = 0;

                while (i < m_ArcLengthPositions.Count - 2 &&
                       l >= m_ArcLengthPositions[i + 1])
                {
                    i++;
                }

                float t =
                    (l - m_ArcLengthPositions[i]) /
                    (m_ArcLengthPositions[i + 1] - m_ArcLengthPositions[i]);

                return i + t;
            }

            /// @brief Computes the arc length of a Catmull-Rom curve segment
            ///
            /// @param i The index of the segment.
            /// @return The arc length of the curve segment at the given index.
            private float ComputeArcLength(int i)
            {
                Vector3[] segment = GetSegmentPositions(i);

                // If the segment is a straight line, just return distance between endpoints
                if (Vector3.Distance(segment[0], segment[1]) == 0 &&
                    Vector3.Distance(segment[2], segment[3]) == 0)
                {
                    return Vector3.Distance(segment[1], segment[2]);
                }

                return ComputeArcLength(segment, 0f, 1f);
            }

            /// @brief Compute the arc length between two points within a segment
            /// 
            /// @param segment The segment consisting of four control points.
            /// @param t1 The parameter value for the first interpolated point.
            /// @param t2 The parameter value for the second interpolated point.
            /// @param threshold The distance threshold for the recursion to end (default: 0.001).
            /// @return The arc length distance between the two interpolated points.
            public float ComputeArcLength(
                Vector3[] segment,
                float t1,
                float t2,
                float threshold = 0.001f)
            {
                Vector3 point1 = Interpolate(segment, t1);
                Vector3 point2 = Interpolate(segment, t2);

                float distance = Vector3.Distance(point1, point2);

                if (distance < threshold)
                {
                    return distance;
                }
                else
                {
                    float tMid = t1 + (t2 - t1) / 2;

                    return ComputeArcLength(segment, t1, tMid, threshold) +
                           ComputeArcLength(segment, tMid, t2, threshold);
                }
            }

            /// @brief Check if the curve has any control points
            ///
            /// @return True if the curve has no control points, otherwise false.
            public bool IsEmpty()
            {
                return m_ControlPoints.Count == 0;
            }

            /// @brief Finalize the curve
            ///
            /// Adds cached control point and complements the last two segments' arc length.
            public virtual void Finish()
            {
                Vector3[] segment =
                {
                    m_ControlPoints[m_ControlPoints.Count > 1 ? ^2 : ^1],
                    m_ControlPoints[^1],
                    m_LastInput,
                    m_LastInput
                };

                // Elasticurve implementation
                m_ControlPoints.Add(Interpolate(segment, m_Responsiveness));

                // Compute arcLength for segment before last
                if (m_ControlPoints.Count >= 3)
                {
                    m_ArcLengthPositions.Add(
                        m_ArcLengthPositions.Last() +
                        ComputeArcLength(m_ControlPoints.Count - 3));
                }

                // Compute arcLength for last segment
                m_ArcLengthPositions.Add(
                    m_ArcLengthPositions.Last() +
                    ComputeArcLength(m_ControlPoints.Count - 2));
            }

            public static Vector3 Orthogonal(Vector3 v)
            {
                return new Vector3(-v.y, v.x);
            }
        }
    }
}
