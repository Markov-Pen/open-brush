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
    /// @brief Represents a Markov Pen, generating random sylization while drawing 
    /// 
    /// Implementation of Lang K., Alexa M.: The Markov Pen: Online Synthesis of 
    /// Free-Hand Drawing Styles. In Non-Photorealistic Animation and Rendering (2015), 
    /// Eurographics Association.
    /// 
    /// @note Currently supports only (approximately) straight example base paths.
    public partial class MarkovPen 
    {
        /// @brief Example mapping containing the user-given base path and style curve for training
        private readonly ExampleMapping m_ExampleMapping;
        /// @brief The target mapping used for synthesis
        private TargetMapping m_TargetMapping;
        /// @brief The synthesis engine creating new stylization
        private readonly SynthesisEngine m_SynthesisEngine = new();

        /// @brief Construct a MarkovPen instance
        /// 
        /// Trains a Markov pen from an example style curve and base path.
        /// 
        /// @param basePathControlPoints Control points of the base path
        /// @param styleCurveControlPoints Control points of the style curve
        public MarkovPen(List<Vector3> basePathControlPoints, List<Vector3> styleCurveControlPoints)
        {
            BasePath basePath = new BasePath(basePathControlPoints);
            Debug.Log("MarkovPen: Arclength of example base path: " + basePath.ArcLength());
            Curve styleCurve = new Curve(styleCurveControlPoints);
            Debug.Log("MarkovPen: Arclength of example style curve: " + styleCurve.ArcLength());

            m_ExampleMapping = new ExampleMapping(basePath, styleCurve);
            m_TargetMapping = new TargetMapping(m_ExampleMapping.GetMaxOffsetAlongNormals());
        }

        /// @brief Reconstruct an example mapping one-to-one, producing a new target mapping
        /// 
        /// Exactly reproduces the example mapping along a target base path,
        /// repeating it as needed to cover the entire arc length.
        /// 
        /// @param target_mapping The mapping to populate
        /// 
        /// @return The generated knots for the target style curve
        public List<Tuple<Vector3, Quaternion>> Reconstruct((Vector3 position, Quaternion rotation) pointer)
        {
            m_TargetMapping.AddBasePoint(pointer.position, pointer.rotation * new Vector3(0.0f,1.0f,0.0f));
        
            return m_SynthesisEngine.Reconstruct(m_ExampleMapping, m_TargetMapping);
        }

        /// @brief Discard the current target mapping and start a fresh one
        /// 
        /// @note Called on trigger-down
        public void NewLine()
        {
            m_TargetMapping = new TargetMapping(m_ExampleMapping.GetMaxOffsetAlongNormals());
        }

        /// @brief Check if the model has been trained
        /// 
        /// @return true if the model is trained, false otherwise
        public bool IsTrained()
        {
            return m_ExampleMapping != null;
        }

    }
}