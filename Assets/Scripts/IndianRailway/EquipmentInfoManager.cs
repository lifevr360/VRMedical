using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IndianRailwayVR
{
    /// <summary>
    /// Scenario 1 - free-explore mode, active AFTER the guided tour finishes.
    ///
    /// Each equipment object has a button. Clicking it:
    ///   - stops whatever audio is currently playing,
    ///   - hides the previously open info panel,
    ///   - shows this object's info panel and plays this object's audio.
    /// Clicking the same object again closes it (toggle off).
    ///
    /// Wire GuidedTourManager.onTourCompleted -> EquipmentInfoManager.EnableInteraction()
    /// so the buttons only become active once the tour is done.
    /// </summary>
    public class EquipmentInfoManager : MonoBehaviour
    {
        [Serializable]
        public class ExploreItem
        {
            [Tooltip("Label for the Inspector only.")]
            public string label;
            [Tooltip("The world-space UI Button on/near this object.")]
            public Button button;
            [Tooltip("This object's own info panel (shown/hidden).")]
            public GameObject infoPanel;
            [Tooltip("Audio played when this object is selected.")]
            public AudioClip audioClip;
        }

        [SerializeField] List<ExploreItem> m_Items = new List<ExploreItem>();
        [SerializeField] AudioSource m_AudioSource;

        [Tooltip("Leave OFF so the tour enables interaction on completion. " +
                 "Turn ON only if you want free-explore available immediately.")]
        [SerializeField] bool m_InteractableOnStart = false;

        int m_CurrentIndex = -1;
        bool m_Interactable;

        void Awake()
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                int idx = i; // capture for the closure
                var item = m_Items[i];

                if (item.button != null)
                    item.button.onClick.AddListener(() => Select(idx));

                if (item.infoPanel != null)
                    item.infoPanel.SetActive(false);
            }
        }

        void Start()
        {
            SetInteractable(m_InteractableOnStart);
        }

        /// <summary>Turn the equipment buttons on (call from the tour's onTourCompleted event).</summary>
        public void EnableInteraction() => SetInteractable(true);

        /// <summary>Turn the equipment buttons off (e.g. while the guided tour is running).</summary>
        public void DisableInteraction()
        {
            Deselect();
            SetInteractable(false);
        }

        void SetInteractable(bool value)
        {
            m_Interactable = value;
            foreach (var item in m_Items)
            {
                if (item.button != null)
                    item.button.interactable = value;
            }
        }

        /// <summary>Select an equipment item by index (also callable from any UnityEvent).</summary>
        public void Select(int index)
        {
            if (!m_Interactable) return;
            if (index < 0 || index >= m_Items.Count) return;

            // Clicking the already-open object closes it.
            if (index == m_CurrentIndex)
            {
                Deselect();
                return;
            }

            // Hide the previously open panel + stop its audio.
            HideCurrent();

            m_CurrentIndex = index;
            var item = m_Items[index];

            if (item.infoPanel != null)
                item.infoPanel.SetActive(true);

            if (item.audioClip != null && m_AudioSource != null)
            {
                m_AudioSource.Stop();
                m_AudioSource.clip = item.audioClip;
                m_AudioSource.Play();
            }
        }

        /// <summary>Close whatever is currently open.</summary>
        public void Deselect() => HideCurrent();

        void HideCurrent()
        {
            if (m_CurrentIndex >= 0 && m_CurrentIndex < m_Items.Count)
            {
                var current = m_Items[m_CurrentIndex];
                if (current.infoPanel != null)
                    current.infoPanel.SetActive(false);
            }

            if (m_AudioSource != null && m_AudioSource.isPlaying)
                m_AudioSource.Stop();

            m_CurrentIndex = -1;
        }
    }
}
