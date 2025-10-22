using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class _ToggleButtonShowPass : MonoBehaviour
{
    [Header("Ô nhập mật khẩu")]
    public TMP_InputField passwordField;

    [Header("Nút ẩn/hiện mật khẩu")]
    public Button toggleButton;

    [Header("Hình mắt mở và mắt đóng (UI Image)")]
    public Image eyeOpenImage;    // 👁 Mắt mở (hiện pass)
    public Image eyeClosedImage;  // 🚫 Mắt đóng (ẩn pass)

    private bool isHidden = false;

    void Start()
    {
        toggleButton.onClick.AddListener(TogglePasswordVisibility);

        // Mặc định: hiển thị pass
        passwordField.contentType = TMP_InputField.ContentType.Standard;
        passwordField.ForceLabelUpdate();

        // Mặc định: hiển thị icon "mắt mở", ẩn "mắt đóng"
        eyeOpenImage.gameObject.SetActive(true);
        eyeClosedImage.gameObject.SetActive(false);
    }

    private void TogglePasswordVisibility()
    {
        isHidden = !isHidden;

        if (isHidden)
        {
            // Ẩn mật khẩu
            passwordField.contentType = TMP_InputField.ContentType.Password;
            eyeOpenImage.gameObject.SetActive(false);
            eyeClosedImage.gameObject.SetActive(true);
        }
        else
        {
            // Hiện mật khẩu
            passwordField.contentType = TMP_InputField.ContentType.Standard;
            eyeOpenImage.gameObject.SetActive(true);
            eyeClosedImage.gameObject.SetActive(false);
        }

        passwordField.ForceLabelUpdate();
    }
}
