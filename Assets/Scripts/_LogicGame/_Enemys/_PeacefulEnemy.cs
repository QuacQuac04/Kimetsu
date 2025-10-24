using UnityEngine;
using TMPro;

public class _PeacefulEnemy : _Enemy
{
    [Header("Hành vi của Peaceful Enemy")]
    public bool canMove = true;
    public float wanderRange = 2f;
    private Vector3 startPosition;

    protected override void Start()
    {
        base.Start();

        // Không cần set tên ở đây nữa, tên được set từ Inspector
        // base.Start() đã gọi UpdateUI() để hiển thị tên từ Inspector

        attackDamage = 0;
        moveSpeed = 1f;
        startPosition = transform.position;
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
