using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public Weapon _launcher;
    public EnnemiHealth _healthSystem;
    public Rigidbody2D _body;
    public bool _destroyAlways = true;

    void Start()
    {
        GameManager._gameManager._enemiesInScene.Add(gameObject);
        _body.linearVelocity = _launcher._gun.speed * (Vector2)transform.up;
        StartCoroutine(ILifetime(_launcher._gun.lifetime));
    }

    void FixedUpdate()
    {
        _body.linearVelocity = Vector2.ClampMagnitude(_body.linearVelocity, _launcher._gun.speed);
        if (Vector2.Distance((Vector2)transform.position, Vector2.zero) > GameManager._gameManager._waveManager._border.transform.localScale.x + 5)
        {
            GetComponent<EnnemiHealth>().HealthChange(-1);
        }
    }

    public IEnumerator ILifetime(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 6)
        {
            Player._player.HealthChange(-Mathf.RoundToInt(Mathf.Clamp(Mathf.FloorToInt(collision.GetContact(0).relativeVelocity.magnitude * 0.1f) - 1, 0, Mathf.Infinity)));
        }
        if (_destroyAlways)
        {
            GetComponent<EnnemiHealth>().HealthChange(-1);
        }
    }
}
