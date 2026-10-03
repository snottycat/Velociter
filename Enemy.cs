using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public Weapon _weapon;
    public Rigidbody2D _body;
    public Animator _anim;
    public Renderer _renderer;
    public GameObject _eye, _iris, _pupil, _pinPrefab, _pin;
    public int _difficulty;

    void Awake()
    {
        _weapon._currentTarget = Player._player.gameObject;
        _iris = _eye.transform.GetChild(0).GetChild(0).gameObject;
        _pupil = _iris.transform.GetChild(0).gameObject;
        _iris.GetComponent<SpriteRenderer>().color = new Color(Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f), 1f);
    }

    void Start()
    {
        _anim.Play("OpenEye");
        StartCoroutine(IEyeBlink(Random.Range(2f, 5f)));
        StartCoroutine(ISpawn(Random.Range(1f, 2f)));
    }
    
    void FixedUpdate()
    {
        _eye.transform.rotation = Quaternion.Euler(0, 0, 0);
        _iris.transform.position = _iris.transform.parent.TransformPoint((Player._player.gameObject.transform.position - _iris.transform.parent.position).normalized * 0.2f);
        if (!_renderer.isVisible)
        {
            if (_pin == null)
            {
                _pin = GameObject.Instantiate(_pinPrefab);
                _pin.transform.parent = Player._player.gameObject.transform;
                _pin.transform.position = Player._player.gameObject.transform.position + Vector3.up * 5f;
            }
            else
            {
                float angleTarget = GameManager.DirectionToAngle((Vector2)(transform.position - Player._player.gameObject.transform.position).normalized);
                float anglePin = GameManager.DirectionToAngle((Vector2)(_pin.transform.position - Player._player.gameObject.transform.position).normalized);
                _pin.transform.RotateAround(Player._player.gameObject.transform.position, Vector3.forward, angleTarget - anglePin);
            }
        }
        else
        {
            if (_pin != null)
            {
                Destroy(_pin);
            }
        }
    }

    public IEnumerator IEyeBlink(float duration)
    {
        yield return new WaitForSeconds(duration);
        _anim.Play("EyeBlinking");
        StartCoroutine(IEyeBlink(Random.Range(2f, 5f)));
    }

    public IEnumerator ISpawn(float duration)
    {
        _weapon._active = false;
        yield return new WaitForSeconds(duration);
        _weapon._active = true;
    }
}
