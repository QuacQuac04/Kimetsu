using UnityEngine;
using UnityEngine.UI;

public class _Open_UI : MonoBehaviour
{
    [Header("Danh sach UI can MO")]
    [Tooltip("Keo cac UI muon mo vao day")]
    public GameObject[] uisToOpen;

    [Header("Danh sach UI can DONG")]
    [Tooltip("Keo cac UI muon dong vao day")]
    public GameObject[] uisToClose;

    [Header("Am thanh (Optional)")]
    public AudioClip clickSound;

    private Button button;
    private AudioSource audioSource;

    private void Start()
    {
        // Tu dong gan button
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // Setup AudioSource neu co sound
        if (clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void OnButtonClick()
    {
        // Phat am thanh
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Dong cac UI trong danh sach uisToClose
        if (uisToClose != null)
        {
            foreach (var ui in uisToClose)
            {
                if (ui != null && ui.activeSelf)
                {
                    ui.SetActive(false);
                }
            }
        }

        // Mo cac UI trong danh sach uisToOpen
        if (uisToOpen != null)
        {
            foreach (var ui in uisToOpen)
            {
                if (ui != null && !ui.activeSelf)
                {
                    ui.SetActive(true);
                }
            }
        }
    }
}
