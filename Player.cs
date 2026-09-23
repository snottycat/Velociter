using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Collections;

public class Player : MonoBehaviour
{
    public static Player _player;
    public GameObject _cam, _UIcam, _backgroundCam;
    public GameObject[] _propels;
    public Rigidbody2D _body;
    public Animator _anim;

    [Header("health")]
    public int _health;
    public int _maxHealth;
    public GameObject _prefabHealthUI;
    public GameObject _healthCountUI;
    public bool _isDiing = false;

    [Header("Weapon")]
    public Weapon _weapon;
    public bool _isShooting = false;
    public float _range;
    public GameObject _laserWeaponUI;
    public GameObject _missileWeaponUI;
    public GameObject _weaponButton;

    [Header("Dash")]
    public int _dashCount;
    public int _maxDash;
    public GameObject _prefabDashUI;
    public GameObject _dashCountUI;
    public GameObject _dashButton;
    public bool _canDash = true;

    [Header("Movement")]
    public Vector2 _movement;
    public float _speed;
    public float _acceleration;
    public float _maxSpeed;
    public TMP_Text _speedUI;
    public GameObject _joystick;
    public bool _isDrifting = false;
    public GameObject _driftButton;

    [Header("UI")]
    public TMP_Text _waveUI;
    public Color _defaultUIColor;
    public float _camCoeff;
    public float _camDamping;

    [Header("Inputs")]
    public Vector2 _worldMousePosition, _savedDirection;
    public enum CurrentDevice {Gamepad, Keyboard, Mobile, Unknow}
    public CurrentDevice _currentDevice;
    private InputAction _mousePosition, _direction, _dash, _drift, _shoot, _changeWeapon;

    void Awake()
    {
        StopAllCoroutines();
        if (Time.timeScale != 1)
        {
            Time.timeScale = 1f;
        }
        _player = this;
        _mousePosition = InputSystem.actions.FindAction("Mouse Position");
        _direction = InputSystem.actions.FindAction("Direction");
        _dash = InputSystem.actions.FindAction("Dash");
        _drift = InputSystem.actions.FindAction("Drift");
        _shoot = InputSystem.actions.FindAction("Shoot");
        _changeWeapon = InputSystem.actions.FindAction("Change Weapon");
        for (int i = 0; i < _maxHealth; i++)
        {
            Instantiate(_prefabHealthUI, _healthCountUI.transform);
        }
        for (int i = 0; i < _maxDash; i++)
        {
            Instantiate(_prefabDashUI, _dashCountUI.transform);
        }
        _currentDevice = CurrentDevice.Unknow;
        UIChangeColor(_defaultUIColor);
        WeaponChange(1);
    }

    void OnEnable()
    {
        _mousePosition.performed += InputHub;
        _mousePosition.canceled += InputHub;
        _mousePosition.Enable();
        _direction.performed += InputHub;
        _direction.canceled += InputHub;
        _direction.Enable();
        _dash.performed += InputHub;
        _dash.Enable();
        _drift.performed += InputHub;
        _drift.canceled += InputHub;
        _drift.Enable();
        _shoot.performed += InputHub;
        _shoot.canceled += InputHub;
        _shoot.Enable();
        _changeWeapon.performed += InputHub;
        _changeWeapon.Enable();
    }

    void OnDisable()
    {
        _player = null;
        _mousePosition.performed -= InputHub;
        _mousePosition.canceled -= InputHub;
        _mousePosition.Disable();
        _direction.performed -= InputHub;
        _direction.canceled -= InputHub;
        _direction.Disable();
        _dash.performed -= InputHub;
        _dash.Disable();
        _drift.performed -= InputHub;
        _drift.canceled -= InputHub;
        _drift.Disable();
        _shoot.performed -= InputHub;
        _shoot.canceled -= InputHub;
        _shoot.Disable();
        _changeWeapon.performed -= InputHub;
        _changeWeapon.Disable();
    }

    void FixedUpdate()
    {
        _movement = Vector2.zero;
        if (!_isDiing)
        {
            if (_currentDevice == CurrentDevice.Keyboard)
            {
                UpdateDirection();
            }
            if (_currentDevice == CurrentDevice.Mobile && _joystick.transform.gameObject.GetComponent<PinePie.SimpleJoystick.JoystickController>().isDraged)
            {
                UpdateDirection();
            }
        }
        _camCoeff = (GameManager._gameManager._waveManager._border.transform.localScale.x - _cam.GetComponent<Camera>().orthographicSize) / GameManager._gameManager._waveManager._border.transform.localScale.x;
        UpdateCam(_camCoeff, _camDamping);
        UpdateSpeed();
        UpdatePropels();
        if (!_isDrifting)
        {
            _body.linearVelocity = Vector2.Lerp(_body.linearVelocity, Vector2.ClampMagnitude(_body.linearVelocity, _maxSpeed), 1f);
            _movement = Vector2.up * _acceleration;
        }
        if (_isShooting)
        {
            UpdateAim();
        }
        _body.AddRelativeForce(_movement);
    }

    public void InputHub(InputAction.CallbackContext context)
    {
        if (context.control.device is Gamepad && _currentDevice != CurrentDevice.Gamepad)
        {
            _joystick.SetActive(false);
            _dashButton.SetActive(false);
            _weaponButton.SetActive(false);
            _driftButton.SetActive(false);
            _laserWeaponUI.transform.parent.localScale = new Vector3(0.05f, 0.05f, 1f);
            _currentDevice = CurrentDevice.Gamepad;
        }
        else if ((context.control.device is Keyboard || context.control.device is Mouse) && _currentDevice != CurrentDevice.Keyboard)
        {
            _joystick.SetActive(false);
            _dashButton.SetActive(false);
            _weaponButton.SetActive(false);
            _driftButton.SetActive(false);
            _laserWeaponUI.transform.parent.localScale = new Vector3(0.05f, 0.05f, 1f);
            _currentDevice = CurrentDevice.Keyboard;
        }
        else if (context.control.device is Touchscreen && _currentDevice != CurrentDevice.Mobile)
        {
            _joystick.SetActive(true);
            _dashButton.SetActive(true);
            _weaponButton.SetActive(true);
            _driftButton.SetActive(true);
            _laserWeaponUI.transform.parent.localScale = new Vector3(0.06f, 0.06f, 1f);
            _currentDevice = CurrentDevice.Mobile;
        }
        if (!_isDiing)
        {
            if (context.action == _direction)
            {
                UpdateDirection();
            }
            if (context.action == _dash)
            {
                Dash();
            }
            if (context.action == _drift)
            {
                if (context.performed)
                {
                    _isDrifting = true;
                }
                if (context.canceled)
                {
                    _isDrifting = false;
                }
            }
            if (context.action == _shoot && _currentDevice != CurrentDevice.Mobile)
            {
                if (context.performed && !GameManager._gameManager.IsOnUI())
                {
                    _isShooting = true;
                }
                if (context.canceled && _isShooting)
                {
                    _isShooting = false;
                    Shoot();
                }
            }
            if (context.action == _changeWeapon)
            {
                if (int.TryParse(context.control.name, out int value))
                {
                    WeaponChange(value);
                }
                else
                {
                    WeaponChange((int)_weapon._currentWeapon);
                }
            }
        }
    }

    public void UpdateSpeed()
    {
        _speed = _body.linearVelocity.magnitude;
        _speedUI.text = (Mathf.Round(_speed * 10) * 0.1f) + " m/s";
    }

    public void UpdateCam(float coef, float damp)
    {
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, new Vector3(transform.position.x * coef, transform.position.y * coef, _cam.transform.position.z), damp);
        _cam.GetComponent<Camera>().orthographicSize = Mathf.Lerp(_cam.GetComponent<Camera>().orthographicSize, Mathf.Clamp(7.5f + _speed * 0.1f, 5f, 25f), damp);
        _UIcam.transform.position = Vector3.forward * (-60 + _speed * 0.1f);
        _UIcam.GetComponent<Camera>().fieldOfView = Mathf.Clamp(27.5f + _speed * 0.1f, 10f, 30f);
        _backgroundCam.GetComponent<Camera>().fieldOfView = Mathf.Clamp(40 + _speed * 0.5f, 30f, 100f);
    }

    public void UpdateDirection()
    {
        if (_currentDevice == CurrentDevice.Gamepad)
        {
            _savedDirection = Vector2.Lerp(_savedDirection, _direction.ReadValue<Vector2>(), 0.5f);
            transform.rotation = Quaternion.Euler(0f, 0f, GameManager.DirectionToAngle(_savedDirection));
        }
        if (_currentDevice == CurrentDevice.Keyboard)
        {
            _worldMousePosition = (Vector2)_cam.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(_mousePosition.ReadValue<Vector2>().x, _mousePosition.ReadValue<Vector2>().y));
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, GameManager.DirectionToAngle(_worldMousePosition - (Vector2)transform.position)), 0.5f);
        }
        if (_currentDevice == CurrentDevice.Mobile)
        {
            _savedDirection = Vector2.Lerp(_savedDirection, (Vector2)(_joystick.transform.GetChild(0).GetChild(0).position - _joystick.transform.GetChild(0).position).normalized, 0.5f);
            transform.rotation = Quaternion.Euler(0f, 0f, GameManager.DirectionToAngle(_savedDirection));
        }
    }

    public void UpdatePropels()
    {
        if (!_isDrifting)
        {
            float distance = Vector2.Distance(-_body.linearVelocity, (Vector2)(_propels[0].transform.position - transform.position));
            GameObject closestPropel = null;
            foreach (var item in _propels)
            {
                if (Vector2.Distance(-_body.linearVelocity, (Vector2)(item.transform.position - transform.position)) < distance)
                {
                    distance = Vector2.Distance(-_body.linearVelocity, (Vector2)(item.transform.position - transform.position));
                    closestPropel = item;
                }
                else
                {
                    item.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 1f, Mathf.Clamp(item.GetComponent<SpriteRenderer>().color.a - 0.05f * Time.fixedDeltaTime, 0.3f, 0.6f));
                }
            }
            if (closestPropel != null)
            {
                closestPropel.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 1f, Mathf.Clamp(closestPropel.GetComponent<SpriteRenderer>().color.a + 0.5f * Time.fixedDeltaTime, 0.3f, 0.6f));
            }
        }
        else
        {
            foreach (var item in _propels)
            {
                item.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 1f, Mathf.Clamp(item.GetComponent<SpriteRenderer>().color.a - 0.5f * Time.fixedDeltaTime, 0f, 1f));
            }
        }
    }

    public void ChangeShootState(bool state)
    {
        _isShooting = state;
    }

    public void ChangeDriftState(bool state)
    {
        _isDrifting = state;
        print(state);
    }

    public void UpdateAim()
    {
        if (_weapon._currentWeaponState == Weapon.CurrentState.Waiting)
        {
            if (_currentDevice == CurrentDevice.Gamepad || _currentDevice == CurrentDevice.Mobile)
            {
                switch (_weapon._currentWeapon)
                {
                    case Weapon.CurrentWeapon.Laser:
                        _weapon._laser.AimCheck(Color.red);
                        break;
                    case Weapon.CurrentWeapon.Rocket:
                        _weapon._rocket.AimCheck(GameManager.DirectionToAngle(_savedDirection), Color.red);
                        break;
                }
            }
            if (_currentDevice == CurrentDevice.Keyboard)
            {
                switch (_weapon._currentWeapon)
                {
                    case Weapon.CurrentWeapon.Laser:
                        _weapon._laser.AimCheck(Color.red);
                        break;
                    case Weapon.CurrentWeapon.Rocket:
                        _weapon._rocket.AimCheck(_worldMousePosition, Color.red);
                        break;
                }
            }
        }
    }

    public void Shoot()
    {
        _isShooting = false;
        if (_weapon._currentWeaponState == Weapon.CurrentState.Waiting)
        {
            if (_currentDevice == CurrentDevice.Gamepad || _currentDevice == CurrentDevice.Mobile)
            {
                switch (_weapon._currentWeapon)
                {
                    case Weapon.CurrentWeapon.Laser:
                        _weapon._laser.Shoot(Color.white);
                        _laserWeaponUI.GetComponentInChildren<Animator>().Play("cooldownUI");
                        _laserWeaponUI.GetComponentInChildren<Animator>().SetFloat("speed", 1 / _weapon._laser.cooldownDuration);
                        break;
                    case Weapon.CurrentWeapon.Rocket:
                        _weapon._rocket.Shoot(GameManager.DirectionToAngle(_savedDirection), Color.red);
                        _missileWeaponUI.GetComponentInChildren<Animator>().Play("cooldownUI");
                        _missileWeaponUI.GetComponentInChildren<Animator>().SetFloat("speed", 1 / _weapon._rocket.cooldownDuration);
                        break;
                }
            }
            if (_currentDevice == CurrentDevice.Keyboard)
            {
                switch (_weapon._currentWeapon)
                {
                    case Weapon.CurrentWeapon.Laser:
                        _weapon._laser.Shoot(Color.white);
                        _laserWeaponUI.GetComponentInChildren<Animator>().Play("cooldownUI");
                        _laserWeaponUI.GetComponentInChildren<Animator>().SetFloat("speed", 1 / _weapon._laser.cooldownDuration);
                        break;
                    case Weapon.CurrentWeapon.Rocket:
                        _weapon._rocket.Shoot(_worldMousePosition, Color.red);
                        _missileWeaponUI.GetComponentInChildren<Animator>().Play("cooldownUI");
                        _missileWeaponUI.GetComponentInChildren<Animator>().SetFloat("speed", 1 / _weapon._rocket.cooldownDuration);
                        break;
                }
            }
        }
    }

    public void WeaponChange(int value)
    {
        _weapon._line.enabled = false;
        _weapon._viseur.SetActive(false);
        if (_currentDevice == CurrentDevice.Gamepad && _changeWeapon.WasPerformedThisFrame())
        {
            _weapon._currentWeapon = (Weapon.CurrentWeapon)Mathf.Clamp((int)_weapon._currentWeapon + Mathf.Round(_changeWeapon.ReadValue<float>()), 1, 2);
        }
        else if (_currentDevice == CurrentDevice.Keyboard || _currentDevice == CurrentDevice.Mobile || !_changeWeapon.WasPerformedThisFrame())
        {
            _weapon._currentWeapon = (Weapon.CurrentWeapon)Mathf.Clamp(value, 1, 2);
        }
        switch (_weapon._currentWeapon)
        {
            case Weapon.CurrentWeapon.Laser:
                _laserWeaponUI.GetComponent<Image>().color = new Color(1f, 0, 0, 1f);
                _missileWeaponUI.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
                break;
            case Weapon.CurrentWeapon.Rocket:
                _laserWeaponUI.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
                _missileWeaponUI.GetComponent<Image>().color = new Color(1f, 0f, 0f, 1f);
                break;
        }
    }

    public void Dash(Vector2 input = default)
    {
        if (_canDash)
        {
            DashChange(-1);
            if (input != default)
            {
                _body.linearVelocity = input * _speed;
            }
            else
            {
                if (_currentDevice == CurrentDevice.Keyboard || _currentDevice == CurrentDevice.Mobile)
                {
                    _body.linearVelocity = (_worldMousePosition - (Vector2)transform.position).normalized * _speed;
                }
                else if (_currentDevice == CurrentDevice.Gamepad)
                {
                    _body.linearVelocity = _direction.ReadValue<Vector2>() * _speed;
                }
            }
            GameObject particle = Instantiate(GameManager._gameManager._dashParticle);
            particle.transform.position = transform.position;
            var main = particle.GetComponent<ParticleSystem>().main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMin * _speed / 2, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMax * _speed);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMin.r, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMin.g, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMin.b, Mathf.Clamp(_speed / _maxSpeed - 0.1f, 0, 1)), new Color(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMax.r, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMax.g, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMax.b, Mathf.Clamp(_speed / _maxSpeed + 0.1f, 0, 1)));
            var emission = particle.GetComponent<ParticleSystem>().emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().emission.rateOverTime.constant * _speed / _maxSpeed);
        }
    }

    public void Dash()
    {
        if (_canDash)
        {
            DashChange(-1);
            if ( _currentDevice == CurrentDevice.Mobile)
            {
                _body.linearVelocity = _savedDirection * _speed;
            }
            if (_currentDevice == CurrentDevice.Keyboard)
            {
                _body.linearVelocity = (_worldMousePosition - (Vector2)transform.position).normalized * _speed;
            }
            else if (_currentDevice == CurrentDevice.Gamepad)
            {
                _body.linearVelocity = _direction.ReadValue<Vector2>() * _speed;
            }
            GameObject particle = Instantiate(GameManager._gameManager._dashParticle);
            particle.transform.position = transform.position;
            var main = particle.GetComponent<ParticleSystem>().main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMin * _speed / 2, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMax * _speed);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMin.r, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMin.g, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMin.b, Mathf.Clamp(_speed / _maxSpeed - 0.1f, 0, 1)), new Color(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMax.r, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMax.g, GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().main.startColor.colorMax.b, Mathf.Clamp(_speed / _maxSpeed + 0.1f, 0, 1)));
            var emission = particle.GetComponent<ParticleSystem>().emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(GameManager._gameManager._dashParticle.GetComponent<ParticleSystem>().emission.rateOverTime.constant * _speed / _maxSpeed);
        }
    }

    public void HealthChange(int amout)
    {
        _health = Mathf.Clamp(_health + amout, 0, _maxHealth);
        List<Image> images = new List<Image>();
        _healthCountUI.GetComponentsInChildren<Image>(images);
        images.Remove(_healthCountUI.GetComponent<Image>());
        if (Mathf.Sign(amout) == 1)
        {
            GaugeChangeUI(images, amout);
        }
        else
        {
            images.Reverse();
            GaugeChangeUI(images, amout);
            _anim.Play("damageTaken");
        }
        if (_health == 0)
        {
            StartCoroutine(IDie());
        }
    }

    public void DashChange(int amout)
    {
        _dashCount = Mathf.Clamp(_dashCount + amout, 0, _maxDash);
        List<Image> images = new List<Image>();
        _dashCountUI.GetComponentsInChildren<Image>(images);
        images.Remove(_dashCountUI.GetComponent<Image>());
        if (Mathf.Sign(amout) == 1)
        {
            GaugeChangeUI(images, amout);
        }
        else
        {
            images.Reverse();
            GaugeChangeUI(images, amout);
        }
        if (_dashCount == 0 && _canDash)
        {
            _canDash = false;
        }
        else if (_dashCount != 0 && !_canDash)
        {
            _canDash = true;
        }
    }

    public void GaugeChangeUI(List<Image> list, int amout)
    {
        int i = Mathf.Abs(amout);
        int goalValue = Mathf.Clamp(amout, 0, 1);
        foreach (var item in list)
        {
            if (item.color.a == Mathf.Abs(goalValue - 1) && i > 0)
            {
                Color color = new Color(item.color.r, item.color.g, item.color.b, goalValue);
                item.color = color;
                i--;
            }
        }
    }

    public IEnumerator IDie(float slowMotionDuration = 1.5f)
    {
        _isDiing = true;
        UIChangeColor(Color.red);
        foreach (var item in _propels)
        {
            item.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 1f, 0f);
        }
        _isDrifting = true;
        _body.linearDamping = 1f;
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(slowMotionDuration);
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(1f);
        GameManager._gameManager.SceneChange("Menu");
    }

    public void UIChangeColor(Color color, GameObject UI = null)
    {
        if (UI == null)
        {
            _healthCountUI.GetComponent<Image>().color = color;
            _dashCountUI.GetComponent<Image>().color = color;
            _speedUI.gameObject.transform.parent.gameObject.GetComponent<Image>().color = color;
            _waveUI.gameObject.transform.parent.gameObject.GetComponent<Image>().color = color;
            _laserWeaponUI.transform.parent.gameObject.GetComponent<Image>().color = color;
        }
        else
        {
            UI.GetComponent<Image>().color = color;
        }
    }
}
