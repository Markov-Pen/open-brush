using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// @class MarkovChain
    /// @brief Stochastic MarkovChain model inheriting from MarkovModel.
    public class MarkovChain : MarkovModel
    {
        /// @brief Constructor for the MarkovChain model.
        /// @param exampleMapping The learned example mapping.  
        public MarkovChain(MarkovPen.ExampleMapping exampleMapping) : base(exampleMapping) // Konstruktor-Aufruf der Elternklasse
        {
            // Initialization is handled by the base class (MarkovModel), so no further setup is required here.
        }

        /// @brief Returns the indey of the next style sample.
        /// @param currentIndex Index of the current style sample.
        /// @return Index of the next sample to draw.
        public int GetNextSample(int currentIndex)
        {
            // Deterministic fallback reconstruction logic taken from MarkovPenTool.MarkovPen.SynthesisEngine.cs. 
            // The example style curve drawn by the user has a fixed number of sample points (e.g., Size() = 50), 
            // while the target path drawn in VR can be arbitrarily long.
            // Solution: Circular buffer (modulo arithmetic) that wraps around to index 0 after reaching the end. 
            // Prevents IndexOutOfRangeException (e.g., (49 + 1) % 50 = 0).
            return (currentIndex + 1) % m_ExampleMapping.Size(); 
        }
    }
}
