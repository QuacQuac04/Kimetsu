using UnityEngine;

/// <summary>
/// Interface cho các đối tượng có thể target được
/// </summary>
public interface ITargetable
{
    // Thông tin cơ bản
    string GetDisplayName();
    int GetLevel();
    Sprite GetAvatar();
    
    // Thông tin máu
    float GetCurrentHealth();
    float GetMaxHealth();
    
    // Loại mục tiêu
    TargetType GetTargetType();
    
    // Transform để tính khoảng cách
    Transform GetTransform();
    
    // Check xem có còn sống không
    bool IsAlive();
    
    // Check xem có thể target được không
    bool IsTargetable();
}

/// <summary>
/// Loại mục tiêu (để ưu tiên)
/// </summary>
public enum TargetType
{
    None = 0,
    Player = 1,      // Người chơi
    Enemy = 2,       // Quái thường
    Boss = 3,        // Boss
    NPC = 4,         // NPC
    Item = 5         // Item
}
