using UnityEngine;

namespace TiltBrush
{
    /// @brief Draws a debug line that visualizes the up vector of a target controller.
    /// Finds the configured controller object by name and renders a world-space line from
    /// the controller position in its local up direction.
    public class MarkovPenTool : BaseTool
    {
        public string TargetName = "monterey_controller_R";
        public float Length = 10f;
        public float Width = 0.01f;

        private Transform m_Target;
        private LineRenderer m_Line;

        /// @brief Initialize the Markov pen tool and create the debug line renderer.
        /// Configures the line renderer to draw a two-point world-space line.
        protected override void Awake()
        {
            base.Awake();

            m_Line = gameObject.AddComponent<LineRenderer>();
            m_Line.positionCount = 2;
            m_Line.startWidth = Width;
            m_Line.endWidth = Width;
            m_Line.useWorldSpace = true;
            m_Line.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// @brief Update the Markov pen tool and draw the controller up vector.
        /// Finds the target controller object if needed and updates the debug line each frame.
        public override void UpdateTool()
        {
            base.UpdateTool();

            if (m_Target == null)
            {
                TryFindTarget();

                if (m_Target == null)
                {
                    return;
                }
            }

            DrawUpVector();
        }

        /// @brief Try to find the configured controller object in the active scene.
        /// Stores the transform if an object with the configured target name exists.
        private void TryFindTarget()
        {
            GameObject targetObject = GameObject.Find(TargetName);

            if (targetObject == null)
            {
                return;
            }

            m_Target = targetObject.transform;
        }

        /// @brief Draw the target controller up vector as a debug line.
        /// Uses the target position as the start point and the target local up vector as direction.
        private void DrawUpVector()
        {
            Vector3 startPosition = m_Target.position;
            Vector3 endPosition = startPosition + m_Target.up * Length;

            m_Line.SetPosition(0, startPosition);
            m_Line.SetPosition(1, endPosition);
        }
    }
}
