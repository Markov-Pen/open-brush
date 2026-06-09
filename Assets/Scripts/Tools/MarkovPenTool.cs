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

using UnityEngine;

namespace TiltBrush
{
    /// @brief Drawing tool that synthesizes free-hand curve styles along arbitrary base paths
    ///        using an autoregressive Double Chain Markov Model (DCMM)
    ///        Until the Markov synthesis is implemented every method delegates to FreePaintTool,
    ///        so the tool behaves identically to plain free-hand drawing.
    public class MarkovPenTool : FreePaintTool
    {
        /// @brief Initialises the tool and all Markov model data structures.
        /// @note  Later: allocate the example curve buffer (A / A'), the transition matrix P,
        ///        the hidden-state matrix Q and per-sample emission matrices R of the DCMM.
        public override void Init()
        {
            base.Init();
        }

        /// @brief Activates or deactivates the Markov Pen tool.
        /// @param isEnabled true to activate the tool; false to deactivate it.
        /// @note  Later: on enable, reset the circle buffer and synthesis state;
        ///        on disable, clear the synthesized output curve B'.
        public override void EnableTool(bool isEnabled)
        {
            base.EnableTool(isEnabled);
        }

        /// @brief Shows or hides the tool's visual indicators.
        /// @param isHidden true to hide the tool visuals; false to show them.
        /// @note  Later: toggle visibility of the example base path A and style curve A' overlays.
        public override void HideTool(bool isHidden)
        {
            base.HideTool(isHidden);
        }

        /// @brief Called every tool update tick. Reads controller input and drives synthesis.
        /// @note  Later: sample the target base path B from the controller position each frame,
        ///        run one step of DCMM synthesis to produce the next
        ///        point on B', and feed the result into the pointer manager.
        public override void UpdateTool()
        {
            base.UpdateTool();
        }

        /// @brief Called after UpdateTool() on each frame. Updates pointer transforms.
        /// @note  Later: apply the folding-avoidance normal-smoothing before
        ///        committing the synthesized pointer position for this frame.
        public override void LateUpdateTool()
        {
            base.LateUpdateTool();
        }

        /// @brief Returns the world-space position and rotation for the brush pointer.
        /// @returns A tuple of (position, rotation) in global space.
        /// @note  Later: instead of returning the raw controller attach point, return the
        ///        synthesized sample position on B', offset by the DCMM-sampled normal s_i.
        protected override (Vector3, Quaternion) GetPointerPosition()
        {
            return base.GetPointerPosition();
        }

        /// @brief Sets the visual materials on the controller geometry to reflect tool state.
        /// @param controller The controller whose materials should be updated.
        /// @note  Later: show a custom hint when the user is recording the example style A'.
        public override void AssignControllerMaterials(InputManager.ControllerName controller)
        {
            base.AssignControllerMaterials(controller);
        }


        /// @brief Adjusts the brush size by the given delta.
        /// @param adjustAmount Signed normalised adjustment amount.
        /// @note  Later: map the size to the sub-sampling interval so that
        ///        increasing the brush size coarsens the style curve resolution.
        public override void UpdateSize(float adjustAmount)
        {
            base.UpdateSize(adjustAmount);
        }

        /// @brief Returns the current brush size as a normalised [0, 1] value.
        /// @returns Brush size in the [0, 1] range.
        public override float GetSize01()
        {
            return base.GetSize01();
        }

        /// @brief Returns whether the brush size can currently be adjusted.
        /// @returns true if size adjustment is allowed in the current application state.
        public override bool CanAdjustSize()
        {
            return base.CanAdjustSize();
        }
    }
}
