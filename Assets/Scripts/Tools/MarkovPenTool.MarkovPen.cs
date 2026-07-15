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
using UnityEngine;

namespace TiltBrush
{
    /// @class MarkovPen
    /// @brief The Markov Pen is a technique for generating character styles.
    ///
    /// This class serves as the base for other partial classes and derives from MarkovPenTool.
    public partial class MarkovPen 
    {
        private Mapping m_ExampleMapping;
        private Mapping m_TargetMapping = new();
        private Synthesizer m_Synthesizer;

        private Curve m_ExampleStyleCurve;
        private Curve m_TargetStyleCurve;

        private BasePath m_ExampleBaseCurve;
        private BasePath m_TargetBaseCurve;

        /// @brief Constuct a MarkovPen instance
        /// 
        /// @param basePathControlPoints - Control Points of the Base Path
        /// @param styleCurveControlPoints - Control points of the Style Curve 
        public MarkovPen(List<Vector3> basePathControlPoints, List<Vector3> styleCurveControlPoints)
        {
            BasePath basePath = new BasePath(basePathControlPoints);
            Debug.Log("MarkovPen: Arclength of example base path: " + basePath.ArcLength());
            Curve styleCurve = new Curve(styleCurveControlPoints);
            Debug.Log("MarkovPen: Arclength of example style curve: " + styleCurve.ArcLength());

            m_ExampleMapping = new Mapping(basePath, styleCurve);
            m_Synthesizer = new Synthesizer(m_ExampleMapping);
        }

        /// @brief Initializes the MarkovPen with an example mapping used for synthesis.
        /// @param exampleMapping A Mapping computed from the example curves.
        public void Initialize(Mapping exampleMapping)
        {
            m_ExampleMapping = exampleMapping;

            m_Synthesizer = new Synthesizer(m_ExampleMapping);
        }

        /// @brief Reconstructs the target mapping using the Synthesizer and returns the reconstructed points.
        /// @param targetMapping A Mapping representing the growing target base path and an empty target style curve.
        /// @return A list of reconstructed point pairs on the target curve.
        public List<Tuple<Vector3, Quaternion>> Reconstruct((Vector3 position, Quaternion rotation) pointer)
        {
            // Debug.Log("MarkovPen: Up vector: " + pointer.rotation * new Vector3(0.0f, 1.0f, 0.0f));
            m_TargetMapping.BasePath.AddControlPoint(pointer.position, pointer.rotation* new Vector3(0.0f,1.0f,0.0f));
        
            return m_Synthesizer.Reconstruct(m_TargetMapping);
        }

        /// @brief Discard the current target curve so the next stroke starts fresh.
        ///
        /// Called on trigger-down to reset the growing target base/style curve between strokes.
        public void NewLine()
        {
            m_TargetMapping = new Mapping();
        }

        /// @brief Checks whether the MarkovPen is trained (example mapping present).
        /// @return True if the MarkovPen is trained; otherwise false.
        public bool IsTrained()
        {
            return m_ExampleMapping != null;
        }

        /// @brief Clears the MarkovPen state by nullifying the synthesizer and example mapping.
        public void Clear()
        {
            m_Synthesizer = null;
            m_ExampleMapping = null;
        }
    }
}