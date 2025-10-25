using UnityEngine;
using TMPro;
public class _Show_Chats : MonoBehaviour
{
    public TextMeshProUGUI chatText;
    public float displayTime = 3f;
    public float fadeSpeed = 2f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void ShowMessage(string message)
    {
        Debug.Log("ShowMessage duoc goi voi message: " + message);
        
        if (chatText == null)
        {
            Debug.LogError("chatText chua duoc gan!");
            return;
        }
        
        chatText.text = message;
        Debug.Log("Da set chatText.text thanh: " + message);
        
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
    }

    private System.Collections.IEnumerator FadeOutRoutine()
    {
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(displayTime);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        Destroy(gameObject);
    }
}
