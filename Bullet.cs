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
        _body.AddRelativeForce(Vector2.up * _launcher._gun.speed, ForceMode2D.Impulse);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        switch (collision.gameObject.layer)
        {
            case 6:
                collision.gameObject.GetComponent<Player>()?.HealthChange(-_launcher._gun.damages);
                break;
            case 7:
                collision.gameObject.GetComponent<EnnemiHealth>()?.HealthChange(-_launcher._gun.damages);
                break;
            case 8:
                if (collision.gameObject.GetComponent<EnnemiHealth>()?._health <= _launcher._gun.damages)
                {
                    collision.gameObject.GetComponent<Missile>()?.Explode();
                }
                collision.gameObject.GetComponent<EnnemiHealth>()?.HealthChange(-_launcher._gun.damages);
                break;
        }
        Destroy(gameObject);
    }
}
