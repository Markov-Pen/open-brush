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
    partial class MarkovPen
    {
        /// @class Mapping
        /// @brief Represents the mapping between a style curve and a base path
        public abstract class Mapping
        {
            // @brief The base path
            protected BasePath m_BasePath;
            // @brief The style curve
            protected Curve m_StyleCurve;

            // @brief The actual mapping containing associations between style curve and base path
            protected List<Vector2> m_Mapping = new();

            /// 
            public int Size()
            {
                return m_Mapping.Count;
            }

            public Vector2 Last()
            {
                return m_Mapping.Last();
            }

            /// @brief Check if the mapping is empty.
            /// @return True if the mapping is empty; otherwise false.
            public bool IsEmpty()
            {
                return m_Mapping.Count == 0;
            }

            /// @brief Gets the association at the specified index.
            /// @param index The index of the association to retrieve.
            /// @return A Vector2 representing the association (x = arc length position, y = offset).
            public Vector2 GetAssociation(int index)
            {
                return m_Mapping[index];
            }
        }

        /// @class ExampleMapping
        /// @brief Represents an example mapping to train a Markov model with
        /// 
        /// Handles the projection of samples on the style curve
        /// onto the base path and computes the offsets
        /// needed to train the probabilistic model.
        public class ExampleMapping : Mapping
        {
            /// @brief Default sampling interval
            private const float k_SamplingInterval = 0.025f;
            /// @brief actual sampling interval
            private float m_SamplingInterval = k_SamplingInterval;

            /// @brief Offsets between each projection point on the base path
            /// and its predecessor
            private List<float> m_OffsetsAlongBasePath;

            /// @brief Construct an example mapping between a style curve and a base path
            /// 
            /// Associates sample points on the style curve with their projections on the base path.
            /// 
            /// @param styleCurve The style curve for the mapping
            /// @param basePath The base path for the mapping
            public ExampleMapping(BasePath basePath, Curve styleCurve)
            {
                Debug.Log("MarkovPen: compute Mapping");

                m_BasePath = basePath;
                m_StyleCurve = styleCurve;

                // Compute sampling interval
                float samplingInterval = ComputeSamplingInterval();
                Debug.Log("MarkovPen: Sampling interval on style curve: " + samplingInterval);

                // Sample style curve
                List<Vector3> samples = SampleStyleCurve(samplingInterval);

                // Project samples onto base path
                List<float> projections = Project(samples);

                // Compute mapping
                ComputeMapping(samples, projections);

                // Compute maximum offset
                float maxOffsetAlongNormals = ComputeMaxOffsetInNormalDirection();
                basePath.Smooth(maxOffsetAlongNormals);
                Debug.Log("MarkovPen: Filter tap for normal smoothing: " + maxOffsetAlongNormals);

                // Compute offsets along base path
                ComputeOffsetsAlongBasePath();

                Debug.Log("MarkovPen: Mapping size: " + m_Mapping.Count);
            }

            /// @brief Report if the mapping is repetitive
            /// 
            /// If the beginning of the mapping fits its end, the mapping can be repeated cyclically.
            /// 
            /// @return true if the mapping is repetitive, else false
            public bool IsRepetitive()
            {
                return true;
            }

            /// @brief Get the offsets along base path and normal at a specific index
            ///
            /// Returns the offsets for joining a new association to the last one.
            /// 
            /// @param index Index in the mapping
            /// @return Point with x = arc length distance to predecessor, y = offset along smooth normal
            public Vector2 GetOffsets(int index)
            {
                return new Vector2(
                    m_OffsetsAlongBasePath[index],
                    m_Mapping[index].y);
            }

            /// @brief Get the maximum offset along smooth normal vectors in the mapping
            /// 
            /// @return Maximum offset
            public float GetMaxOffsetAlongNormals()
            {
                return m_BasePath.Tap;
            }

            /// @brief Compute optimal sampling interval
            ///
            /// Adjusts the sampling interval to evenly distribute samples
            /// along the curve.
            /// 
            /// @return The adjusted sampling interval
            public float ComputeSamplingInterval()
            {
                int numSamples =
                    Mathf.RoundToInt(m_StyleCurve.ArcLength() / m_SamplingInterval);

                numSamples = Math.Max(numSamples, 5);

                return m_StyleCurve.ArcLength() / numSamples;
            }

            /// @brief Sample the style curve based on the given sampling interval
            /// 
            /// @param samplingInterval The arc length interval separating the samples
            /// 
            /// @return A list of sample points on the style curve
            public List<Vector3> SampleStyleCurve(float samplingInterval)
            {
                List<Vector3> samples = new List<Vector3>();

                int numSamples =
                    Mathf.RoundToInt(m_StyleCurve.ArcLength() / m_SamplingInterval) + 1;

                for (int i = 0; i < numSamples; i++)
                {
                    samples.Add(m_StyleCurve.PositionAt(i * samplingInterval));
                }

                return samples;
            }

            /// @brief Project a number of points onto the base path
            /// 
            /// @param samples A list of 3D points to be projected
            /// 
            /// @return Arc length positions of the projected locations
            public List<float> Project(List<Vector3> samples)
            {
                List<float> projections = new List<float>();

                foreach (var sample in samples)
                {
                    float projection = m_BasePath.Project(sample)[0];
                    projections.Add(projection);
                }

                return projections;
            }

            /// @brief Compute the actual mapping between style curve and base path
            ///
            /// Records the offsets between samples and their projections
            ///
            /// @param samples The samples to be projected
            /// @param projections The arc length positions of the projected locations
            private void ComputeMapping(List<Vector3> samples, List<float> projections)
            {
                m_Mapping = new List<Vector2>();

                for (int i = 0; i < samples.Count; ++i)
                {
                    Vector3 basePoint = m_BasePath.PositionAt(projections[i]);

                    Vector3 smoothNormal =
                        m_BasePath.SmoothNormalAt(projections[i]);

                    Vector3 toSample = samples[i] - basePoint;

                    float offsetAlongNormal =
                        Vector3.Distance(basePoint, samples[i]);

                    if (Vector3.Dot(smoothNormal, toSample) < 0)
                    {
                        offsetAlongNormal *= -1;
                    }

                    m_Mapping.Add(
                        new Vector2(projections[i], offsetAlongNormal));
                }
            }

            /// @brief Compute the maximum offset in normal direction of all associations in the mapping
            private float ComputeMaxOffsetInNormalDirection()
            {
                float maxOffset = 0;

                foreach (var association in m_Mapping)
                {
                    maxOffset =
                        Math.Max(maxOffset, Math.Abs(association.y));
                }

                return maxOffset;
            }

            /// @brief Compute offsets between each projection point on the base path and its predecessor
            /// 
            /// Records the arc length differences between consecutive associations
            /// (arc length position and normal offset pairs)
            private void ComputeOffsetsAlongBasePath()
            {
                m_OffsetsAlongBasePath = new List<float>(m_Mapping.Count);

                for (int i = 1; i < m_Mapping.Count; ++i)
                {
                    m_OffsetsAlongBasePath.Add(
                        m_Mapping[i].x - m_Mapping[i - 1].x);
                }

                // Start and end point in mapping match
                if (IsRepetitive())
                {
                    Debug.Log("MarkovPen: Mapping is repetitive");

                    // Offset of last point becomes that of first
                    m_OffsetsAlongBasePath.Insert(
                        0,
                        m_OffsetsAlongBasePath.Last());

                    // Remove last point and offset
                    m_OffsetsAlongBasePath.RemoveAt(
                        m_OffsetsAlongBasePath.Count - 1);
                    m_Mapping.RemoveAt(m_Mapping.Count - 1);
                }
            }
        }

        /// @class TargetMapping
        /// @brief Represents an target mapping used to synthesize new style curves
        public class TargetMapping : Mapping
        {
            public int LastIndex { get; private set; }

            /// @brief Construct an empty target mapping
            ///
            ///  Creates a new mapping with initially no associations.
            /// 
            /// @param maxOffsetAlongNormal The maximum offset between samples and projections in the example mapping
            public TargetMapping(float maxOffsetAlongNormals)
            {
                LastIndex = -1;
                m_BasePath = new BasePath(maxOffsetAlongNormals);
                m_StyleCurve = new Curve();
            }

            /// @brief Add a knot point to the internal base path
            /// 
            /// @param point The 3D point to be added
            /// @param upVector The up vector corresponding to the knot
            public void AddBasePoint(Vector3 point, Vector3 upVector)
            {
                m_BasePath.AddControlPoint(point, upVector);
            }

            /// @brief Apply offsets to create a new mapping entry
            /// 
            /// Adds a new association (arc length position and normal offset pair) 
            /// to the mapping by applying the provided offsets to the previous association.
            /// 
            /// @param offsets Point with x = arc length position, y = offset along smooth normal
            /// @return true if successful, false if the generated association is beyond the end of the base path
            public bool Apply(Vector2 offsets, int index)
            {
                // Compute new arcLength position
                float l =
                    IsEmpty()
                        ? 0
                        : m_Mapping.Last().x + offsets.x;

                // Check if new arcLength position exceeds the arcLength of the base path
                if (l >= m_BasePath.ArcLength())
                {
                    return false;
                }

                // Add new association to the mapping
                m_Mapping.Add(new Vector2(l, offsets.y));

                m_StyleCurve.AddControlPoint(
                    Inflate(m_Mapping.Last()).Item2);

                LastIndex = index;

                return true;
            }

            /// @brief Inflate an association to create a point on the style curve and a projected point on the base path
            ///
            /// Converts an association (arc length position and normal offset pair)
            /// into a pair of points: The projected point on the base path 
            /// and knot point on the style curve. The latter is obtained
            /// by walking from the base path along the smoothed normal vector.
            /// 
            /// @param association Point with x = arc length position, y = offset along smooth normal
            /// @return Pair of points: first = base point, second = style curve point
            public Tuple<Vector3, Vector3> Inflate(Vector2 association)
            {
                Vector3 basePoint =
                    m_BasePath.PositionAt(association.x);

                Vector3 normal =
                    m_BasePath.SmoothNormalAt(association.x);

                Vector3 toPoint =
                    Vector3.Scale(
                        normal.normalized,
                        new Vector3(
                            association.y,
                            association.y,
                            association.y));

                return new Tuple<Vector3, Vector3>(
                    basePoint,
                    basePoint + toPoint);
            }
        }
    }
}