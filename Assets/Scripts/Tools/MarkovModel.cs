using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// @class MarkovModel
    /// @brief Abstrct base class for machine learning models used in the Markov Pen.
    public abstract class MarkovModel
    {
        ///  @brief Protected example mapping containing learned base path and style curve data.
        protected MarkovPen.ExampleMapping m_ExampleMapping;

        /// @brief Public property providing access to the example mapping. 
        public MarkovPen.ExampleMapping Mapping => m_ExampleMapping;

        /// @brief Constructor initializing the base model with an example mapping.
        /// @param exampleMapping The learned mapping between basse path and style curve.
        public MarkovModel(MarkovPen.ExampleMapping exampleMapping)
        {
            m_ExampleMapping = exampleMapping;
        }
    }
}

