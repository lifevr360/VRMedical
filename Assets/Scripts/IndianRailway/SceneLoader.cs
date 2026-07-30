using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianRailwayVR
{
    /// <summary>
    /// Loads scenes from UI buttons. Put one in every scene (home + each scenario).
    ///
    /// - Home menu buttons: wire each button's On Click -> SceneLoader.LoadScene, and type
    ///   the scenario scene name.
    /// - In-scenario "Home" button: wire On Click -> SceneLoader.LoadHome.
    ///
    /// IMPORTANT: every scene you load must be added to
    /// File > Build Settings > Scenes In Build, or the load will fail.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Tooltip("Exact name of the home/menu scene (used by LoadHome).")]
        [SerializeField] string m_HomeSceneName = "Home";

        [Tooltip("Optional short delay before loading, so a click sound can play.")]
        [SerializeField] float m_LoadDelay = 0.15f;

        [Tooltip("Optional. If assigned, the screen fades to black before loading.")]
        [SerializeField] ScreenFader m_Fader;

        bool m_Loading;

        /// <summary>Load a scene by name (wire from a button, type the scene name).</summary>
        public void LoadScene(string sceneName)
        {
            if (m_Loading) return;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] No scene name provided.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        /// <summary>Return to the home/menu scene.</summary>
        public void LoadHome() => LoadScene(m_HomeSceneName);

        /// <summary>Reload the scene currently active (e.g. a "Restart" button).</summary>
        public void ReloadCurrent() => LoadScene(SceneManager.GetActiveScene().name);

        /// <summary>Quit the application (ignored in the editor).</summary>
        public void QuitApp()
        {
            Debug.Log("[SceneLoader] Quit requested.");
            Application.Quit();
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            m_Loading = true;
            Debug.Log($"[SceneLoader] Loading scene '{sceneName}'.");

            if (m_Fader != null)
            {
                // Fade to black, then load once the fade has finished.
                m_Fader.FadeOut();
                yield return new WaitForSeconds(m_Fader.Duration);
            }
            else if (m_LoadDelay > 0f)
            {
                yield return new WaitForSeconds(m_LoadDelay);
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
