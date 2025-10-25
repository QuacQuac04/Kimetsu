using UnityEngine;
using TMPro;

public class _PeacefulEnemy : _Enemy
{
    [Header("Hành vi của Peaceful Enemy")]
    public bool canMove = true;
    public float wanderRange = 0f;
    private Vector3 startPosition;

    protected override void Start()
    {
        base.Start();

        // Không cần set tên ở đây nữa, tên được set từ Inspector
        // base.Start() đã gọi UpdateUI() để hiển thị tên từ Inspector

        attackDamage = 0;
        moveSpeed = 1f;
        startPosition = transform.position;
        
        // Kiểm tra giá trị hợp lệ
        if (wanderRange <= 0)
        {
            Debug.LogWarning(gameObject.name + ": wanderRange <= 0, disabling movement");
            canMove = false; // Tắt movement nếu wanderRange <= 0
        }
        
        if (float.IsNaN(startPosition.x) || float.IsNaN(startPosition.y) || float.IsNaN(startPosition.z))
        {
            Debug.LogError(gameObject.name + ": startPosition is NaN!");
            startPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        if (canMove)
        {
            Wander();
        }
    }

    void Wander()
    {
        // Enemy di chuyển nhẹ qua lại quanh vị trí ban đầu
        float newX = Mathf.PingPong(Time.time * moveSpeed, wanderRange) + startPosition.x - wanderRange / 2;
        
        // Kiểm tra NaN trước khi gán
        if (float.IsNaN(newX))
        {
            Debug.LogError(gameObject.name + ": newX is NaN! Disabling movement.");
            canMove = false;
            return;
        }
        
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    public override void TakeDame(float damage)
    {
        base.TakeDame(damage);
    }

    public override void Die()
    {
        Destroy(gameObject);
    }
}
