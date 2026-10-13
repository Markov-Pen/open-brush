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
using Vector3 = UnityEngine.Vector3;

namespace TiltBrush
{
    public partial class MarkovPen
    { 
        /// @class BasePath
        /// @brief Represents the base path of a Markov Pen
        ///
        /// The BasePath class extends the functionality of the Curve class and provides
        /// additional features such as projection and smoothing functionalities.
        public class BasePath : Curve
        {
            /// @brief The tap (half-window size) used for normal smoothing
            public float Tap { get; private set; }

            /// brief The list of up vectors for each knot
            private readonly List<Vector3> m_UpVectors = new();

            /// @brief The list of smooth normals for each knot
            private readonly List<Vector3> m_SmoothNormals = new();

            /// @brief Construct an empty BasePath instance
            ///
            /// Creates a new target base path with initially no knots and zero arc length.
            ///
            /// @param smoothingTap The tap (half window size) for normal smoothing
            /// @param responsiveness Responsiveness parameter controlling Elasticurve smoothing (default: 0.75).
            public BasePath(float smoothingTap, float responsiveness = 0.75f) : base(responsiveness)
            {
                Tap = smoothingTap;
            }

            /// @brief Construct a BasePath instance from a list of control points
            ///
            /// Creates a fully initialized example base path
            ///
            /// @param List<Vector3> controlPoints 3D Points forming the base path in OpenBrush
            public BasePath(List<Vector3> controlPoints)
            {
                Vector3 upVector = Orthogonal(controlPoints.Last() - controlPoints.First()).normalized;

                AddControlPoint(controlPoints.First(), upVector);
                AddControlPoint(controlPoints.Last(), upVector);
                Finish();
            }

            /// @brief Add a control point to a target base path perform necessary updates
            ///
            /// Extends the base class method to incorporate smoothing of up vector and normals.
            ///
            /// @param controlPoint The new control point to be added
            /// @param upVector The up vector associated with the control point
            public void AddControlPoint(Vector3 controlPoint, Vector3 upVector)
            {
                base.AddControlPoint(controlPoint);

                m_UpVectors.Add(
                    m_UpVectors.Count > 0
                        ? (0.25f * upVector + 0.75f * m_UpVectors.Last()).normalized
                        : upVector);

                if (m_ControlPoints.Count < 4)
                {
                    return;
                }

                while (Tap <= m_ArcLengthPositions.Last() - m_ArcLengthPositions[m_SmoothNormals.Count])
                {
                    int index = m_SmoothNormals.Count;

                    m_SmoothNormals.Add(ComputeSmoothNormal(index));

                    if (m_SmoothNormals.Count > 1 &&
                        Vector3.Dot(m_SmoothNormals[^1], m_SmoothNormals[^2]) < 0)
                    {
                        m_SmoothNormals[^1] *= -1;
                    }
                }
            }

            /// @brief Request the total arc length of the base path
            /// 
            /// Returns the last arc length position for which smooth normals have already been computed.
            /// 
            /// @return The total arc length of the base path
            public override float ArcLength()
            {
                if (Tap == 0)
                {
                    return base.ArcLength();
                }

                return m_ArcLengthPositions[Math.Max(m_SmoothNormals.Count - 1, 0)];
            }

            public void Smooth(float tap)
            {
                Tap = tap;
            }

            /// @brief Evaluate smoothed normal vector at given arc length position
            /// 
            /// @param l Arc length position (may be negative or beyond the arc length of the base path)
            ///
            /// @return Smoothed normal vector
            public Vector3 SmoothNormalAt(float l)
            {
                if (l <= 0)
                {
                    return m_SmoothNormals[0];
                }

                if (l >= m_ArcLengthPositions.Last())
                {
                    return m_SmoothNormals.Last();
                }

                float t = TimeAt(l);
                int index = SegmentIndex(t);

                Vector3[] smoothNormals =
                {
                    index == 0 ?
                        m_SmoothNormals[0] :
                        m_SmoothNormals[index - 1].normalized,
                    m_SmoothNormals[index].normalized,
                    m_SmoothNormals[index + 1].normalized,
                    index == m_SmoothNormals.Count - 2 ?
                        m_SmoothNormals[index + 1].normalized :
                        m_SmoothNormals[index + 2].normalized,
                };

                return Interpolate(smoothNormals, SegmentTime(t));
            }

            /// @brief Compute smoothed tangent vector at given index
            /// 
            /// @param index The knot index starting from 0
            /// 
            /// @return Smoothed tangent vector
            private Vector3 ComputeSmoothTangent(int index)
            {
                float center = m_ArcLengthPositions[index];
                float windowSize = 2 * Tap + 1;
                
                Vector3 smoothTangentVector = Vector3.zero;
                // Add up tangent vectors
                for (float l = center - Tap; l <= center + Tap; l += (1.0f / windowSize))
                {
                    smoothTangentVector += FirstDerivativeAt(l).normalized;
                }

                return smoothTangentVector / windowSize;
            }

            /// @brief Compute smoothed normal vector at given index
            /// 
            /// @param index The knot index starting from 0
            ///
            /// @return Smoothed normal vector
            private Vector3 ComputeSmoothNormal(int index)
            {
                Vector3 upVector = m_UpVectors[index].normalized * 100;
                Vector3 smoothTangentDirection = ComputeSmoothTangent(index).normalized;

                // Project prologenged up vector onto smooth tangent
                float projection = Vector3.Dot(upVector, smoothTangentDirection);

                // make up vector perpendicular to smoot tangent
                return (upVector - smoothTangentDirection * projection).normalized;
            }

            /// @brief Project a point onto the base path
            /// 
            /// Determines the arc length positions 
            /// where the prolonged smooth normal 
            /// (approximately) runs through the given 2D point.
            /// 
            /// @note There may be multiple approximate projection points.
            /// 
            /// @param to_project Point to project
            /// 
            /// @return List of arc length positions the point projects to
            public List<float> Project(Vector3 toProject)
            {
                List<float> projections = new List<float>();

                // If the curve is a straight line, just project on that
                if(m_ControlPoints.Count == 2)
                {
                    Vector3 tangentDirection =
                        Vector3.Normalize(m_ControlPoints.Last() - m_ControlPoints.First());

                    Vector3 toPoint = toProject - m_ControlPoints[0];  
                    
                    projections.Add(Vector3.Dot(toPoint, tangentDirection));

                    return projections;
                }

                // Try to project on each segment
                for (int index = 1; index < m_ArcLengthPositions.Count; ++index)
                {

                    Project(
                        toProject,
                        m_ArcLengthPositions[index - 1],
                        m_ArcLengthPositions[index],
                        projections);
                }

                // If no projection has been found, project on tangent of first or last point
                if (projections.Count == 0)
                {
                    if (Vector3.Distance(toProject, m_ControlPoints[0]) <
                        Vector3.Distance(toProject, m_ControlPoints[^1]))
                    {
                        Vector3 toPoint = toProject - m_ControlPoints[0];

                        Vector3[] firstSegment = GetSegmentPositions(0);
                        Vector3 tangent = ComputeTangentsAtEndpoints(firstSegment).Item1;

                        float l = Vector3.Dot(toPoint, tangent.normalized);

                        projections.Add(l);
                    }
                    else
                    {
                        Vector3 toPoint = toProject - m_ControlPoints[^1];

                        Vector3[] lastSegment = GetSegmentPositions(m_ControlPoints.Count - 2);
                        Vector3 tangent = ComputeTangentsAtEndpoints(lastSegment).Item2;

                        float l = Vector3.Dot(toPoint, tangent.normalized);

                        projections.Add(m_ArcLengthPositions.Last() + l);
                    }
                }

                return projections;
            }

            /// @brief Project a point onto a portion of the base path
            /// 
            /// Determines the arc length positions within a specified interval 
            /// where the prolonged smooth normal 
            /// (approximately) runs through the given 2D point.
            /// 
            /// @note There may be multiple approximate projection points.
            /// 
            /// @param point Point to project
            /// @param l1 Beginning of arc length interval
            /// @param l2 End of arc length interval
            /// @param projections List of arc length positions the point projects to
            private void Project(
                Vector3 point,
                float l1,
                float l2,
                List<float> projections)
            {
                double d1 = Shoot(point, l1);
                double d2 = Shoot(point, l2);

                if (d1 < 0.0 && d2 < 0.0)
                {
                    return;
                }

                if (d1 > 0.0 && d2 > 0.0)
                {
                    return;
                }

                float middle = l1 + (l2 - l1) / 2.0f;

                if (Math.Abs(d1) < 0.001 && Math.Abs(d2) < 0.001)
                {
                    projections.Add(middle);
                    return;
                }

                Project(point, l1, middle, projections);
                Project(point, middle, l2, projections);
            }

            /// @brief Compute the distance of a 2D point to the prolonged smooth normal at the given arc length position
            /// 
            /// Projects a point onto the smoothed normal vector 
            /// at the specific arc length position
            /// and computes the signed distance of the point to the projected point.
            /// 
            /// @param point The point to project
            /// @param l Arc length position at which the smooth normal is to be evaluated
            /// 
            /// @return Signed distance to the normal line at l
            private float Shoot(Vector3 point, float l)
            {
                Vector3 basePoint = PositionAt(l);
                Vector3 toPoint = point - basePoint;

                Vector3 normal= SmoothNormalAt(l).normalized;

                float projection = Vector3.Dot(normal, toPoint);
                Vector3 offset = normal * projection;
                Vector3 closestPoint = basePoint + offset;

                float distance = Vector3.Distance(point, closestPoint);

                double determinant = toPoint.x * normal.y - toPoint.y * normal.x;

                if (determinant < 0)
                {
                    distance *= -1;
                }

                return distance;
            }

            /// @brief Finalize the base path
            ///
            /// Computes remaining smooth normals
            public override void Finish()
            {
                base.Finish();

                for (int index = m_SmoothNormals.Count;
                    index < m_ControlPoints.Count;
                    index++)
                {
                    m_SmoothNormals.Add(ComputeSmoothNormal(index));
                }
            }
        }
    }
}
