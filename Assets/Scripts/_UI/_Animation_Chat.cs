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
    
    [Header("Chat Input va Send Button")]
    [SerializeField] private TMP_InputField inputChat; // Input_Chat
    [SerializeField] private Button btnSend;            // _btn_Send
    [SerializeField] private _PlayerController playerController; // Reference den player controller
    
    private void Start()
    {
        Debug.Log("_Animation_Chat Start() duoc goi tren GameObject: " + gameObject.name);
        
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
        
        // Gan su kien cho button Send
        Debug.Log("Dang gan listener cho btnSend...");
        if (btnSend != null)
        {
            Debug.Log("btnSend khong null, dang add listener...");
            Debug.Log("Button name: " + btnSend.gameObject.name + ", Path: " + GetGameObjectPath(btnSend.gameObject));
            Debug.Log("Button interactable: " + btnSend.interactable);
            Debug.Log("Button active: " + btnSend.gameObject.activeInHierarchy);
            btnSend.onClick.AddListener(OnClickSendButton);
            Debug.Log("Da gan listener cho btnSend thanh cong!");
            Debug.Log("Button onClick listener count: " + btnSend.onClick.GetPersistentEventCount());
        }
        else
        {
            Debug.LogError("btnSend la NULL! Khong the gan listener!");
        }
        
        // Gan su kien Enter de gui chat
        if (inputChat != null)
        {
            Debug.Log("Da gan onSubmit listener cho inputChat");
            inputChat.onSubmit.AddListener((text) => OnClickSendButton());
        }
        else
        {
            Debug.LogError("inputChat la NULL!");
        }
    }
    
    private void Update()
    {
        // Test bang phim T
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Phim T duoc nhan - test button!");
            OnClickSendButton();
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
    
    /// <summary>
    /// Helper method de lay path cua GameObject
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
    
    /// <summary>
    /// Xu ly khi click button Send
    /// </summary>
    private void OnClickSendButton()
    {
        Debug.Log("OnClickSendButton duoc goi!");
        
        // Kiem tra input chat co text khong
        if (inputChat == null)
        {
            Debug.LogError("Input Chat chua duoc gan!");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(inputChat.text))
        {
            Debug.LogWarning("Input chat rong!");
            return;
        }
        
        // Kiem tra player controller
        if (playerController == null)
        {
            Debug.LogError("PlayerController chua duoc gan!");
            return;
        }
        
        // Lay noi dung chat
        string message = inputChat.text;
        Debug.Log("Gui chat: " + message);
        
        // Hien thi chat tren dau player
        playerController.ShowChatMessage(message);
        
        // Xoa noi dung input sau khi gui
        inputChat.text = "";
        
        // Focus lai input field de nguoi choi co the tiep tuc chat
        inputChat.ActivateInputField();
    }
}
