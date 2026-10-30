using System.Collections.Generic;

namespace TiltBrush
{
    /// @brief Tracks strokes that existed before Markov drawing starts and deletes newly created strokes
    /// Reads the stroke memory list of SketchMemoryScript without modifying it.
    public static class MarkovPenSketchMemoryScript
    {
        private static readonly HashSet<Stroke> s_StrokesBeforeMarkovDrawing = new();

        /// @brief Start capturing the current sketch stroke state before Markov drawing begins
        /// Stores all currently existing strokes so newly created Markov strokes can be detected later.
        public static void BeginMarkovStrokeCapture()
        {
            s_StrokesBeforeMarkovDrawing.Clear();

            LinkedList<Stroke> memoryList = GetMemoryList();

            if (memoryList == null)
            {
                return;
            }

            foreach (Stroke stroke in memoryList)
            {
                if (stroke != null)
                {
                    s_StrokesBeforeMarkovDrawing.Add(stroke);
                }
            }
        }

        /// @brief Delete all strokes that were created after Markov stroke capture started
        /// Keeps the saved Markov point lists unchanged.
        public static void DeleteNewMarkovStrokes()
        {
            LinkedList<Stroke> memoryList = GetMemoryList();

            if (memoryList != null)
            {
                foreach (Stroke stroke in memoryList)
                {
                    // Skip strokes that are already hidden, e.g. deleted or undone strokes.
                    if (stroke == null ||
                        !stroke.IsGeometryEnabled ||
                        s_StrokesBeforeMarkovDrawing.Contains(stroke))
                    {
                        continue;
                    }

                    // Deletion is deferred, so the memory list is not modified while iterating.
                    SketchMemoryScript.m_Instance.MemorizeDeleteSelection(stroke);
                }
            }

            s_StrokesBeforeMarkovDrawing.Clear();
        }

        /// @brief Get the stroke memory list of the active SketchMemoryScript
        /// @return The memory list, or null if no SketchMemoryScript instance exists.
        private static LinkedList<Stroke> GetMemoryList()
        {
            return SketchMemoryScript.m_Instance != null
                ? SketchMemoryScript.m_Instance.GetMemoryList
                : null;
        }
    }
}
