using System.Collections;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Handles transitions between Markov Pen popups.
    /// </summary>
    public static class MarkovPenPopUpSwitcher
    {
        /// <summary>
        /// Closes the current popup and requests a new popup.
        /// </summary>
        /// <param name="currentPopup">
        /// The popup requesting the transition.
        /// </param>
        /// <param name="command">
        /// The command identifying the next popup.
        /// </param>
        public static void Switch(
            PopUpWindow currentPopup,
            SketchControlsScript.GlobalCommands command)
        {
            if (currentPopup == null)
            {
                return;
            }

            UIComponentManager manager =
                currentPopup.GetComponentInParent<UIComponentManager>();

            BasePanel panel = manager != null
                ? manager.GetPanelForPopUps()
                : null;

            if (panel == null)
            {
                Debug.LogError(
                    "MarkovPenPopUpSwitcher: Popup panel not found.");
                return;
            }

            // Preserve the popup position before closing it.
            Vector3 position = currentPopup.transform.position;

            // Run the transition on the panel, which survives
            // the destruction of the current popup.
            panel.StartCoroutine(
                SwitchCoroutine(
                    panel, currentPopup, command, position));

            currentPopup.RequestClose(true);
        }

        /// <summary>
        /// Waits for the previous popup to close before
        /// creating the requested popup.
        /// </summary>
        /// <param name="panel">
        /// The panel managing the popups.
        /// </param>
        /// <param name="previousPopup">
        /// The popup being closed.
        /// </param>
        /// <param name="command">
        /// The command identifying the next popup.
        /// </param>
        /// <param name="position">
        /// The desired popup position.
        /// </param>
        /// <returns>
        /// An enumerator controlling the transition.
        /// </returns>
        private static IEnumerator SwitchCoroutine(
            BasePanel panel,
            PopUpWindow previousPopup,
            SketchControlsScript.GlobalCommands command,
            Vector3 position)
        {
            // Wait until the old popup is no longer active.
            while (panel != null &&
                panel.PanelPopUp == previousPopup)
            {
                yield return null;
            }

            if (panel == null)
            {
                yield break;
            }

            // Let the current UI input finish.
            yield return new WaitForEndOfFrame();

            // Avoid replacing another popup.
            if (panel.PanelPopUp != null)
            {
                yield break;
            }

            panel.CreatePopUp(command, -1, -1, "PopupText");

            if (panel.PanelPopUp == null)
            {
                Debug.LogError(
                    "MarkovPenPopUpSwitcher: Failed to create popup.");
                yield break;
            }

            panel.PositionPopUp(position);
        }
    }
}
