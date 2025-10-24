using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class _Animation_Button_Nhiem_Vu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnNhiemVuChinh;
    [SerializeField] private Button btnNhiemVuBang;
    
    [Header("Images hieu ung cua Button Nv. Chinh (2 cai)")]
    [Tooltip("Keo 2 images hieu ung khi active Nv. Chinh vao day")]
    [SerializeField] private Image imgEffectNhiemVuChinh1;
    [SerializeField] private Image imgEffectNhiemVuChinh2;
    
    [Header("Texts cua Button (TMPro)")]
    [SerializeField] private TextMeshProUGUI txtNhiemVuChinh;
    [SerializeField] private TextMeshProUGUI txtNhiemVuBang;
    
    [Header("Mau sac Text")]
    [SerializeField] private Color colorTextActive = Color.yellow;  // Mau chu khi active
    [SerializeField] private Color colorTextInactive = Color.white; // Mau chu khi inactive
    
    private void Start()
    {
        // Gan su kien click cho cac button
        if (btnNhiemVuChinh != null)
            btnNhiemVuChinh.onClick.AddListener(OnClickNhiemVuChinh);
        
        if (btnNhiemVuBang != null)
            btnNhiemVuBang.onClick.AddListener(OnClickNhiemVuBang);
        
        // Mac dinh active Nhiem Vu Chinh
        OnClickNhiemVuChinh();
    }
    
    /// <summary>
    /// Khi click vao button Nhiem Vu Chinh
    /// </summary>
    public void OnClickNhiemVuChinh()
    {
        // HIEN 2 IMAGES hieu ung cua Nv. Chinh
        if (imgEffectNhiemVuChinh1 != null)
            imgEffectNhiemVuChinh1.gameObject.SetActive(true);
        
        if (imgEffectNhiemVuChinh2 != null)
            imgEffectNhiemVuChinh2.gameObject.SetActive(true);
        
        // Doi mau text: Nv. Chinh = Active (vang)
        if (txtNhiemVuChinh != null)
            txtNhiemVuChinh.color = colorTextActive;
        
        // Doi mau text: Nv. Bang = Inactive (trang)
        if (txtNhiemVuBang != null)
            txtNhiemVuBang.color = colorTextInactive;
    }
    
    /// <summary>
    /// Khi click vao button Nhiem Vu Bang
    /// </summary>
    public void OnClickNhiemVuBang()
    {
        // AN 2 IMAGES hieu ung cua Nv. Chinh
        if (imgEffectNhiemVuChinh1 != null)
            imgEffectNhiemVuChinh1.gameObject.SetActive(false);
        
        if (imgEffectNhiemVuChinh2 != null)
            imgEffectNhiemVuChinh2.gameObject.SetActive(false);
        
        // Doi mau text: Nv. Chinh = Inactive (trang)
        if (txtNhiemVuChinh != null)
            txtNhiemVuChinh.color = colorTextInactive;
        
        // Doi mau text: Nv. Bang = Active (vang)
        if (txtNhiemVuBang != null)
            txtNhiemVuBang.color = colorTextActive;
    }
}
