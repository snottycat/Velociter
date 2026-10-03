using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Weapon _launcher;
    public EnnemiHealth _healthSystem;
    public Rigidbody2D _body;

    void Start()
    {
        GameManager._gameManager._enemiesInScene.Add(gameObject);
    }

    void FixedUpdate()
    {
        _body.linearVelocity = Vector2.ClampMagnitude(_body.linearVelocity, _launcher._gun.speed);
        _body.AddRelativeForce(Vector2.up * _launcher._gun.speed, ForceMode2D.Impulse);
    }
}
