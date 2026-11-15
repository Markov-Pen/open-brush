// Copyright 2026 The Tilt Brush Authors
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
    /// <summary>
    /// Provides the selection popup for the 2D and 3D Markov Pen modes.
    /// </summary>
    public class MarkovPenOptionsPopUpWindow : OptionsPopUpWindow
    {
        /// <summary>
        /// Opens the 2D Markov Pen popup.
        /// </summary>
        public void TwoDimensional()
        {
            MarkovPenPopUpSwitcher.Switch(
                this,
                SketchControlsScript.GlobalCommands.MarkovPen2DOptions);
        }

        /// <summary>
        /// Opens the 3D Markov Pen popup.
        /// </summary>
        public void ThreeDimensional()
        {
            MarkovPenPopUpSwitcher.Switch(
                this,
                SketchControlsScript.GlobalCommands.MarkovPen3DOptions);
        }

        /// <summary>
        /// Closes the selection popup.
        /// </summary>
        public void HandleOKButton()
        {
            RequestClose(true);
        }
    }
}
