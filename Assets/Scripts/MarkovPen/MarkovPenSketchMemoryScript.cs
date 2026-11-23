using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace TiltBrush
{
    /// <summary>
    /// Tracks strokes that existed before Markov drawing starts and deletes newly created strokes
    /// </summary>
    /// <remarks>
    /// Uses reflection to read current stroke references from SketchMemoryScript without modifying it.
    /// </remarks>
    public class MarkovPenSketchMemoryScript
    {
        private static readonly HashSet<Stroke> s_StrokesBeforeMarkovDrawing = new();

        /// <summary>
        /// Start capturing the current sketch stroke state before Markov drawing begins
        /// </summary>
        /// <remarks>
        /// Stores all currently existing strokes so newly created Markov strokes can be detected later.
        /// </remarks>
        public static void BeginMarkovStrokeCapture()
        {
            s_StrokesBeforeMarkovDrawing.Clear();

            foreach (Stroke stroke in GetAllCurrentStrokes())
            {
                if (stroke != null)
                {
                    s_StrokesBeforeMarkovDrawing.Add(stroke);
                }
            }
        }

        /// <summary>
        /// End Markov stroke capture
        /// </summary>
        /// <remarks>
        /// Keeps the captured stroke snapshot available until deletion or the next capture begins.
        /// </remarks>
        public static void EndMarkovStrokeCapture()
        {
        }

        /// <summary>
        /// Delete all strokes that were created after Markov stroke capture started
        /// </summary>
        /// <remarks>
        /// Keeps the saved Markov point lists unchanged.
        /// </remarks>
        public static void DeleteNewMarkovStrokes()
        {
            if (SketchMemoryScript.m_Instance == null)
            {
                return;
            }

            foreach (Stroke stroke in GetAllCurrentStrokes())
            {
                if (stroke == null)
                {
                    continue;
                }

                if (!s_StrokesBeforeMarkovDrawing.Contains(stroke))
                {
                    SketchMemoryScript.m_Instance.MemorizeDeleteSelection(stroke);
                }
            }

            s_StrokesBeforeMarkovDrawing.Clear();
        }

        /// <summary>
        /// Get all current strokes found inside SketchMemoryScript through reflection
        /// </summary>
        /// <returns>Enumerable collection of current stroke references.</returns>
        private static IEnumerable<Stroke> GetAllCurrentStrokes()
        {
            if (SketchMemoryScript.m_Instance == null)
            {
                yield break;
            }

            object memory = SketchMemoryScript.m_Instance;
            Type memoryType = memory.GetType();

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo[] fields = memoryType.GetFields(flags);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(memory);

                if (value == null)
                {
                    continue;
                }

                if (value is Stroke singleStroke)
                {
                    yield return singleStroke;
                    continue;
                }

                if (value is IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        if (item is Stroke stroke)
                        {
                            yield return stroke;
                        }
                    }
                }
            }
        }
    }
}
