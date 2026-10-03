using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Missile : MonoBehaviour
{
    public GameObject _target, _explosionEffect;
    public Rigidbody2D _body;
    public CircleCollider2D _explosionCollider;
    public Vector2 _direction;
    public EnnemiHealth _healthSystem;
    public Weapon _launcher;
    public bool _canExplode = true;
    public float _speed;
    public List<GameObject> _objectsInExplosion = new List<GameObject>();

    public void Start()
    {
        GameManager._gameManager._enemiesInScene.Add(gameObject);
        GetComponent<SpriteRenderer>().color = _launcher._rocket.missileColor;
        _launcher._rocket.rotationSpeed = Mathf.Clamp(_launcher._rocket.rotationSpeed, 0f, 1f);
        _body.linearDamping = _launcher._rocket.precision;
        if (_body.sharedMaterial == null)
        {
            GetComponent<Collider2D>().sharedMaterial.bounciness = _launcher._rocket.explosionForce;
        }
        else
        {
            _body.sharedMaterial.bounciness = _launcher._rocket.explosionForce;
        }
        _explosionEffect.transform.localScale = new Vector3(_launcher._rocket.explosionRadius, _launcher._rocket.explosionRadius, 1f);
        if (_launcher._mainUser.TryGetComponent<Rigidbody2D>(out Rigidbody2D parentBody))
        {
            _body.linearVelocity = parentBody.linearVelocity;
        }
        GetComponent<CircleCollider2D>().enabled = true;
        StartCoroutine(ILifetime(_launcher._rocket.lifetime));
    }

    void FixedUpdate()
    {
        if (_target != null)
        {
            _direction = (_target.transform.position - transform.position).normalized;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, GameManager.DirectionToAngle(_direction));
        _body.linearVelocity = Vector2.ClampMagnitude(_body.linearVelocity, _launcher._rocket.maxSpeed);
        _speed = _body.linearVelocity.magnitude;
        _body.AddRelativeForce(Vector2.up * _launcher._rocket.acceleration);
    }

    public IEnumerator ILifetime(float duration)
    {
        yield return new WaitForSeconds(duration);
        Explode();
    }

    public void Explode()
    {
        if (_canExplode)
        {
            _canExplode = false;
            ParticleEffect();
            List<GameObject> objectsInExplosionCopy = new List<GameObject>(_objectsInExplosion);
            foreach(var item in objectsInExplosionCopy)
            {
                if (item.TryGetComponent<Rigidbody2D>(out Rigidbody2D explosedBody))
                {
                    explosedBody.AddForce((item.transform.position - transform.position).normalized * _launcher._rocket.explosionForce * 10f * (Vector2.Distance(item.transform.position, transform.position) / _launcher._rocket.explosionRadius));
                }
                switch (item.layer)
                {
                    case 6:
                        item.GetComponent<Player>()?.HealthChange(-_launcher._rocket.damages);;
                        GameManager._gameManager.CameraShake(5, 0.4f);
                        break;
                    case 7:
                        item.GetComponent<EnnemiHealth>()?.HealthChange(-_launcher._rocket.damages);
                        break;
                    case 8:
                        if (item.GetComponent<EnnemiHealth>()?._health <= _launcher._rocket.damages)
                        {
                            item.GetComponent<Missile>()?.Explode();
                        }
                        item.GetComponent<EnnemiHealth>()?.HealthChange(-_launcher._rocket.damages);
                        break;
                    case 12:
                        item.GetComponent<EnnemiHealth>()?.HealthChange(-_launcher._rocket.damages);
                        break;
                }
            }
            _healthSystem.HealthChange(-_launcher._rocket.damages);
        }
    }

    public void ParticleEffect()
    {
        GameObject explosion = Instantiate(GameManager._gameManager._explosionParticle);
        explosion.transform.position = transform.position;
        var main = explosion.GetComponent<ParticleSystem>().main;
        main.startSize = new ParticleSystem.MinMaxCurve(GameManager._gameManager._explosionParticle.GetComponent<ParticleSystem>().main.startSize.constantMin * _launcher._rocket.explosionRadius, GameManager._gameManager._explosionParticle.GetComponent<ParticleSystem>().main.startSize.constantMax * _launcher._rocket.explosionRadius);
        main.startSpeed = new ParticleSystem.MinMaxCurve(GameManager._gameManager._explosionParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMin * _launcher._rocket.explosionRadius, GameManager._gameManager._explosionParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMax * _launcher._rocket.explosionRadius);
        var shape = explosion.GetComponent<ParticleSystem>().shape;
        if (transform.localScale.x == transform.localScale.y)
        {
            shape.radius = transform.localScale.x;
            shape.scale = new Vector3(1f, 1f, 1f);
        }
        else
        {
            shape.radius = 1f;
            shape.scale = new Vector3(transform.localScale.x, transform.localScale.y, 1f);
        }
        var emission = explosion.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(GameManager._gameManager._explosionParticle.GetComponent<ParticleSystem>().emission.rateOverTime.constant * _launcher._rocket.explosionRadius * _launcher._rocket.damages);
    }
    
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (_launcher._mainUser != collision.gameObject)
        {
            Explode();
        }
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        _objectsInExplosion.Add(collider.gameObject);
    }

    public void OnTriggerStay2D(Collider2D collider)
    {
        if (!_objectsInExplosion.Contains(collider.gameObject))
        {
            _objectsInExplosion.Add(collider.gameObject);
        }
    } 

    public void OnTriggerExit2D(Collider2D collider)
    {
        _objectsInExplosion.Remove(collider.gameObject);
    }
}
