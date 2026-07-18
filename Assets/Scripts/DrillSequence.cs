using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DrillSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRGrabInteractable drillGrab;
    [SerializeField] private GameObject[] screwGhosts;     // 3 ghosts in drill order
    public GameObject screwGhostsPlacement;
    private Blink[] ghostBlinks;
    private int  currentIndex = 0;
    private bool done = false;
    private bool drillHeld = false;

    public bool IsDone => done;

    private void Awake()
    {
        ghostBlinks = new Blink[screwGhosts.Length];
        for (int i = 0; i < screwGhosts.Length; i++)
            if (screwGhosts[i] != null) ghostBlinks[i] = screwGhosts[i].GetComponent<Blink>();

        if (drillGrab != null)
        {
            drillGrab.selectEntered.AddListener(OnGrabbed);
            drillGrab.selectExited.AddListener(OnReleased);
        }
    }

    private void Start()
    {
        for (int i = 0; i < screwGhosts.Length; i++)
            if (screwGhosts[i] != null) screwGhosts[i].SetActive(false);
    }

    private void OnDestroy()
    {
        if (drillGrab != null)
        {
            drillGrab.selectEntered.RemoveListener(OnGrabbed);
            drillGrab.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs _)
    {
        drillHeld = true;
        if (done) return;
        ShowGhost(currentIndex);
    }

    private void OnReleased(SelectExitEventArgs _)
    {
        drillHeld = false;
        if (done) return;
        HideGhost(currentIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (done || !drillHeld) return;
        if (currentIndex >= screwGhosts.Length) return;
        if (other.gameObject != screwGhosts[currentIndex]) return;

        HideGhost(currentIndex);
        currentIndex++;
        if (currentIndex >= screwGhosts.Length)
        {
            done = true;
            screwGhostsPlacement.SetActive(true);
            return;
        }
        ShowGhost(currentIndex);   // drill still held -> light up next one
    }

    private void ShowGhost(int i)
    {
        if (i < 0 || i >= screwGhosts.Length || screwGhosts[i] == null) return;
        screwGhosts[i].SetActive(true);
        if (ghostBlinks[i] != null) ghostBlinks[i].isBlink = true;
    }

    private void HideGhost(int i)
    {
        if (i < 0 || i >= screwGhosts.Length || screwGhosts[i] == null) return;
        if (ghostBlinks[i] != null) ghostBlinks[i].isBlink = false;
        screwGhosts[i].SetActive(false);
    }
}
