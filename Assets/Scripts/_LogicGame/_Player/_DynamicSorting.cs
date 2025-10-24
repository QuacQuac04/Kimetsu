using UnityEngine;
using Spine.Unity;

/// <summary>
/// Dynamic Sorting Order - LOGIC DON GIAN
/// Y thap hon (dung TRUOC) = Sorting Order cao hon = Hien thi TRUOC
/// Y cao hon (dung SAU) = Sorting Order thap hon = Hien thi SAU
/// </summary>
public class _DynamicSorting : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private string sortingLayerName = "Player"; // Sorting Layer (giong nhau)
    [SerializeField] private int sortingOrderOffset = 1000; // Offset de dam bao khong bi am
    [SerializeField] private bool updateEveryFrame = true; // Cap nhat moi frame
    
    private MeshRenderer meshRenderer;
    private SpriteRenderer spriteRenderer;
    private SkeletonAnimation skeletonAnimation;
    
    void Start()
    {
        // Lay renderer cho Spine (MeshRenderer)
        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        if (skeletonAnimation != null)
        {
            meshRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
        }
        
        // Neu khong phai Spine, lay SpriteRenderer
        if (meshRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        UpdateSortingOrder();
    }
    
    void LateUpdate()
    {
        if (updateEveryFrame)
        {
            UpdateSortingOrder();
        }
    }
    
    void UpdateSortingOrder()
    {
        // LOGIC DON GIAN:
        // Sorting Order = 1000 - (Y * 10)
        // 
        // Vi du:
        // Y = 10 → Order = 1000 - 100 = 900
        // Y = 5  → Order = 1000 - 50 = 950
        // 
        // Y=5 (950) > Y=10 (900) → Object Y=5 hien thi TRUOC
        
        float yPos = transform.position.y;
        int sortingOrder = sortingOrderOffset - Mathf.RoundToInt(yPos * 10f);
        
        // Apply len MeshRenderer (Spine)
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
        
        // Apply len SpriteRenderer
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }
}
