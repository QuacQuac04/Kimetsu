using UnityEngine;
using TMPro;

public class _PlayerController : MonoBehaviour
{
    public Transform chatSpawnPoint; // vị trí hiển thị trên đầu
    public GameObject chatBubblePrefab; // Prefab chat

    public void ShowChatMessage(string message)
    {
        Debug.Log("ShowChatMessage duoc goi voi message: " + message);
        
        if (chatBubblePrefab == null)
        {
            Debug.LogError("Chat Bubble Prefab chua duoc gan!");
            return;
        }
        
        if (chatSpawnPoint == null)
        {
            Debug.LogError("Chat Spawn Point chua duoc gan!");
            return;
        }
        
        GameObject chat = Instantiate(chatBubblePrefab, chatSpawnPoint.position, Quaternion.identity);
        Debug.Log("Chat bubble da duoc tao: " + chat.name + " tai vi tri: " + chatSpawnPoint.position);
        
        chat.transform.SetParent(chatSpawnPoint); // gắn theo đầu
        
        _Show_Chats showChatsScript = chat.GetComponent<_Show_Chats>();
        if (showChatsScript == null)
        {
            Debug.LogError("Chat bubble khong co component _Show_Chats!");
            return;
        }
        
        showChatsScript.ShowMessage(message);
    }
}
