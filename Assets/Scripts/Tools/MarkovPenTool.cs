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

namespace TiltBrush
{
    /// @brief Drawing tool that synthesizes free-hand curve styles along arbitrary base paths
    ///        using an autoregressive Double Chain Markov Model (DCMM).
    ///        Inherits input handling and pointer management from FreePaintTool.
    public partial class MarkovPenTool : FreePaintTool
    {
    

        /// @brief Unity per-frame update. Handles transform updates and visual state.
        /// @note  Input handling belongs in UpdateTool(), not here.
        private void Update()
        {
        }

  

        /// @brief Activates or deactivates the Markov Pen tool.
        ///        Delegates to OnEnableTool() or OnDisableTool() accordingly.
        /// @param isEnabled true to activate the tool; false to deactivate it.
        public override void EnableTool(bool isEnabled)
        {
            base.EnableTool(isEnabled);

            if (isEnabled)
            {
                OnEnableTool();
            }
            else
            {
                OnDisableTool();
            }
        }

        /// @brief Called every tool update tick. Reads controller input and drives
        ///        Markov Pen synthesis along the current base path.
        public override void UpdateTool()
        {
            base.UpdateTool();
        }

   

        /// @brief Initialises Markov Pen state when the tool becomes active.
        private void OnEnableTool()
        {
        }

        /// @brief Cleans up Markov Pen state when the tool is deactivated.
        private void OnDisableTool()
        {
        }
    }
}
