// Copyright 2020 The Open Brush Authors
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
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace TiltBrush
{

    /// @brief Drawing tool that synthesizes free-hand curve styles along arbitrary base paths
    ///        Until the Markov synthesis is implemented every method delegates to FreePaintTool,
    ///        so the tool behaves identically to plain free-hand drawing.
    /// @note in future versions it will be using a Markov Chain or an autoregressive hidden Markov Model
    /// 
    public class MarkovPenTool : FreePaintTool
    {
        [SerializeField] private float m_DebugPointSize = 0.015f;
        [SerializeField] private bool m_ShowReconstructedPoints = true;

        private readonly List<GameObject> m_DebugPointObjects = new();
        private int m_DebugPointIndex;
        private MarkovPen m_MarkovPen;

        Tuple<Vector3, Quaternion> m_LastPointer= new Tuple<Vector3, Quaternion> (Vector3.zero, Quaternion.identity);

        /// @brief Initialise the tool and all Markov model data structures.
        public override void Init()
        {
            base.Init();
        
            //Debug.Log("Init");
       }

        /// @brief Activate or deactivate the Markov Pen tool
        /// @param isEnabled true to activate the tool; false to deactivate it.
        public override void EnableTool(bool isEnabled)
        {
            base.EnableTool(isEnabled);
            //Debug.Log("Tool Enabled");
            List<Vector3> exampleListBasePath = new List<Vector3>() { new(0.0f, 0.0f, 0.0f), new(0.25f, 0.0f, 0.0f), new(0.5f, 0.0f, 0.0f), new(0.75f, 0.0f, 0.0f), new(1.0f, 0.0f, 0.0f) };

            List<Vector3> exampleListStyleCurve = new List<Vector3>() { new(0.0f, 0.0f, 0.0f), new(0.125f, 0.125f, 0.0f), new(0.25f, 0.25f, 0.0f), new(0.375f, 0.125f, 0.0f), new(0.5f, 0.0f, 0.0f), new(0.625f, 0.125f, 0.0f), new(0.75f, 0.25f, 0.0f), new(0.875f, 0.125f, 0.0f), new(1.0f, 0.0f, 0.0f) };
            CreateMarkovPen(exampleListBasePath, exampleListStyleCurve);
        }

        /// @brief Show or hide the tool's visual indicators
        /// @param isHidden true to hide the tool visuals; false to show them.
        /// @note  Later: indicates Up Vector? adds visual indicator for controller position
        public override void HideTool(bool isHidden)
        {
            base.HideTool(isHidden);
            //Debug.Log("Tool Hidden");
        }

        /// @brief Read controller input and drive synthesis
        ///
        /// Called every tool update tick.
        /// @note  Later: sample the target base path B from the controller position each frame,
        ///        run one step of DCMM synthesis to produce the next
        ///        point on B', and feed the result into the pointer manager.
        public override void UpdateTool()
        {
            bool triggerDown = InputManager.Brush.GetCommandDown(InputManager.SketchCommands.Activate);

            if (triggerDown)
            {
                m_MarkovPen.newLine();
                var start = base.GetPointerPosition();
                m_LastPointer = Tuple.Create(start.Item1, start.Item2);
            }

            List<Tuple<Vector3, Quaternion>> pointers = (m_MarkovPen.Reconstruct(base.GetPointerPosition()));

            if (pointers.Count > 0)
            {
                 m_LastPointer= pointers.First();
                 pointers.RemoveAt(0);
                // m_EatInput= false;
            }
            else{
               // m_EatInput= true;
            }

            base.UpdateTool();

            while(!(pointers.Count == 0))
            {
                m_LastPointer= pointers.First();
                base.UpdateTool();
                pointers.RemoveAt(0);     

            }



/*
            if (triggerDown || m_CurrentPoint == null)
            {
                m_MarkovPen.ResetTarget();
                m_Overflow.Clear();

                var start = base.GetPointerPosition();
                m_CurrentPoint = Tuple.Create(start.Item1, start.Item2);
            }
            base.UpdateTool();

            PointerScript pointer = PointerManager.m_Instance.MainPointer;

            if (m_brushTrigger && pointer.IsCreatingStroke())
            {
                var toDraw = new List<Tuple<Vector3, Quaternion>>(m_Overflow);
                m_Overflow.Clear();
                toDraw.AddRange(m_MarkovPen.Reconstruct(base.GetPointerPosition()));

                bool advanced = false;
                for (int i = 0; i < toDraw.Count; i++)
                {
                    if (pointer.ShouldCurrentLineEnd())
                    {
                        for (int j = i; j < toDraw.Count; j++)
                        {
                            m_Overflow.Enqueue(toDraw[j]);
                        }
                        break;
                    }

                    if (i > 0)
                    {
                        var prev = toDraw[i - 1];
                        PointerManager.m_Instance.SetPointerTransform(
                            InputManager.ControllerName.Brush, prev.Item1, prev.Item2);
                        pointer.UpdateLineFromObject();
                    }

                    m_CurrentPoint = toDraw[i];
                    advanced = true;
                }

                // Leave the frontier as the pointer position so the manager's sample draws it (once).
                if (advanced)
                {
                    PointerManager.m_Instance.SetPointerTransform(
                        InputManager.ControllerName.Brush, m_CurrentPoint.Item1, m_CurrentPoint.Item2);
                }
            }

            //Debug.Log("Update");
            */
        }

        /// @brief Update pointer transforms
        /// 
        /// Called only on frames that UpdateTool() has been called.
        /// Guaranteed to be called after new poses have been received from OpenVR.
        public override void LateUpdateTool()
        {
            base.LateUpdateTool();
            // Debug.Log("LateUpdate");
        }

        private void ShowDebugPoint(Vector3 position)
        {
            if (!m_ShowReconstructedPoints) { return; }

            // Unterschiedliche Farbe pro Punkt: Rot -> Gelb -> Grün -> Blau ...
            Color color = Color.HSVToRGB(
                Mathf.Repeat(m_DebugPointIndex * 0.12f, 1.0f),
                0.9f,
                1.0f);

            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.name = $"MarkovPoint_{m_DebugPointIndex}";
            point.transform.position = position;
            point.transform.localScale = Vector3.one * m_DebugPointSize;

            // Collider wird für reine Visualisierung nicht benötigt.
            Destroy(point.GetComponent<Collider>());

            Renderer renderer = point.GetComponent<Renderer>();
            renderer.material.color = color;

            m_DebugPointObjects.Add(point);
            m_DebugPointIndex++;
        }

        private void ClearDebugPoints()
        {
            foreach (GameObject point in m_DebugPointObjects)
            {
                if (point != null)
                {
                    Destroy(point);
                }
            }

            m_DebugPointObjects.Clear();
            m_DebugPointIndex = 0;
        }

        /// @brief Return the world-space position and rotation for the brush pointer
        /// @returns A tuple of (position, rotation) in global space.
        protected override (Vector3, Quaternion) GetPointerPosition()
        {
            return (m_LastPointer.Item1, m_LastPointer.Item2);
        }

        /// @brief Set the visual materials on the controller geometry to reflect tool state
        /// @param controller The controller whose materials should be updated.
        public override void AssignControllerMaterials(InputManager.ControllerName controller)
        {
            base.AssignControllerMaterials(controller);
        }


        /// @brief Adjust the brush size by the given delta
        /// @param adjustAmount Signed normalised adjustment amount.
        public override void UpdateSize(float adjustAmount)
        {
            base.UpdateSize(adjustAmount);
        }



        /// @brief Return the current brush size as a normalised [0, 1] value
        /// @returns Brush size in the [0, 1] range.
        public override float GetSize01()
        {
            return base.GetSize01();
        }

        /// @brief Return whether the brush size can currently be adjusted
        /// @returns true if size adjustment is allowed in the current application state.
        public override bool CanAdjustSize()
        {
            return base.CanAdjustSize();
        }

        /// @brief Create a new Markov pen
        /// 
        /// @param basePath - Control Points of the given example Base Path
        /// @param styleCurve - Control points of the given example Style Curve
        public void CreateMarkovPen(List<Vector3> basePath, List<Vector3> styleCurve)
        {
            m_MarkovPen = new MarkovPen(basePath, styleCurve);
        }
    }
}
