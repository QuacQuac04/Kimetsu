using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class _Animation_Chat : MonoBehaviour
{
    [System.Serializable]
    public class ChatButton
    {
        public string name;                        // Ten button (cho de nhin)
        public Button button;                      // Button component
        public Image imageEffect;                  // Image (1) - hieu ung khi active
        public TextMeshProUGUI text;               // Text (TMP)
    }
    
    [Header("Danh sach tat ca cac Chat Buttons")]
    [SerializeField] private ChatButton[] chatButtons;
    
    [Header("Mau sac Text")]
    [SerializeField] private Color colorTextActive = Color.yellow;  // Mau chu khi active
    [SerializeField] private Color colorTextInactive = Color.white; // Mau chu khi inactive
    
    [Header("Mac dinh active button nao (index)")]
    [SerializeField] private int defaultActiveIndex = 0; // 0 = Chung
    
    private void Start()
    {
        // Gan su kien click cho tat ca cac button
        for (int i = 0; i < chatButtons.Length; i++)
        {
            int index = i; // Capture index for lambda
            if (chatButtons[i].button != null)
            {
                chatButtons[i].button.onClick.AddListener(() => OnClickChatButton(index));
            }
        }
        
        // Mac dinh active button dau tien
        if (chatButtons.Length > 0 && defaultActiveIndex < chatButtons.Length)
        {
            OnClickChatButton(defaultActiveIndex);
        }
    }
    
    /// <summary>
    /// Khi click vao bat ky button nao
    /// </summary>
    /// <param name="clickedIndex">Index cua button duoc click</param>
    public void OnClickChatButton(int clickedIndex)
    {
        // Duyet qua tat ca cac buttons
        for (int i = 0; i < chatButtons.Length; i++)
        {
            bool isActive = (i == clickedIndex);
            
            // AN/HIEN Image (1) - hieu ung
            if (chatButtons[i].imageEffect != null)
            {
                chatButtons[i].imageEffect.gameObject.SetActive(isActive);
            }
            
            // DOI MAU TEXT
            if (chatButtons[i].text != null)
            {
                chatButtons[i].text.color = isActive ? colorTextActive : colorTextInactive;
            }
        }
    }
}
