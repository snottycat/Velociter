using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    public LineRenderer _line;
    public Animation _anim;
    public LayerMask _shootInteract;
    public GameObject _viseur, _propels, _hull;
    public Sprite _fixedHull;

    [Header("Parents")]
    public GameObject _mainUser;

    [Header("Weapon States")]
    public CurrentMode _currentMode;
    public CurrentWeapon _currentWeapon;
    public CurrentState _currentWeaponState;
    public Gun _gun;
    public Laser _laser;
    public Rocket _rocket;
    public enum CurrentMode {Fixed, Drone, AssaultDrone}
    public enum CurrentWeapon {Gun = 1,Laser = 2, Rocket = 3}
    public enum CurrentState {Shooting, Aiming, Waiting, Cooldown, Disabled}

    [Header("Drone Mode")]
    public float _orbitDistance;
    public float _orbitSpeed;
    public GameObject _currentTarget;
    public Sprite _droneHull;

    [Serializable]
    public class Gun
    {
        public Weapon script;
        public GameObject prefabBullet;
        public Color bulletColor;
        public int damages, bulletCount;
        public float speed, lifetime, bulletSize,cooldownDuration;
        public Vector2 spawnOffset;
        public CurrentState currentState;

        public void Shoot(Color color)
        {
            GameObject bullet = Instantiate(prefabBullet);
            bullet.transform.position = script.gameObject.transform.TransformPoint(spawnOffset);
            bullet.transform.rotation = script.gameObject.transform.rotation;
            bullet.transform.localScale = Vector3.one * bulletSize;
            bullet.GetComponent<SpriteRenderer>().color = color;
            bullet.GetComponent<Bullet>()._launcher = script;
            script.StartCoroutine(this.ICooldown(this.cooldownDuration));
        }

        public IEnumerator ICooldown(float duration)
        {
            currentState = CurrentState.Cooldown;
            yield return new WaitForSeconds(duration);
            currentState = CurrentState.Waiting;
        }
    }

    [Serializable]
    public class Laser
    {
        public Weapon script;
        public int damages;
        public float range;
        public float cooldownDuration, aimingDuration, shootingDuration;
        public bool equiped;
        public Color lastShotColor;
        public CurrentState currentState;

        public void Shoot(Color color)
        {
            if (currentState != CurrentState.Cooldown && currentState != CurrentState.Disabled)
            {
                this.lastShotColor = color;
                RaycastHit2D hit = AimCheck(color);
                switch (hit.transform?.gameObject.layer)
                {
                    case 6:
                        Player._player.HealthChange(-damages);;
                        break;
                    case 7:
                        hit.transform?.gameObject.GetComponent<EnnemiHealth>()?.HealthChange(-damages);
                        break;
                    case 8:
                        if (hit.transform?.gameObject.GetComponent<EnnemiHealth>()?._health <= damages)
                        {
                            hit.transform?.gameObject.GetComponent<Missile>()?.Explode();
                        }
                        hit.transform?.gameObject.GetComponent<EnnemiHealth>()?.HealthChange(-damages);
                        break;
                }
                if (hit.transform != null)
                {
                    GameObject particle = Instantiate(GameManager._gameManager._hitParticle);
                    particle.transform.position = hit.point;
                    var main = particle.GetComponent<ParticleSystem>().main;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(GameManager._gameManager._hitParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMin * this.damages, GameManager._gameManager._hitParticle.GetComponent<ParticleSystem>().main.startSpeed.constantMax * this.damages);
                    var emission = particle.GetComponent<ParticleSystem>().emission;
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(GameManager._gameManager._hitParticle.GetComponent<ParticleSystem>().emission.rateOverTime.constant * this.damages);
                }
                script._anim.Play("LaserShooting");
                script.StartCoroutine(this.ICooldown(this.cooldownDuration));
            }
            else
            {
                script._line.enabled = false;
            }
        }

        public RaycastHit2D AimCheck(Color color)
        {
            Vector2 direction = (Vector2)script.gameObject.transform.up;
            RaycastHit2D hit = Physics2D.Raycast((Vector2)script.gameObject.transform.position, direction, range, script._shootInteract);
            if (currentState != CurrentState.Cooldown && currentState != CurrentState.Disabled)
            {
                script._line.widthMultiplier = 0.5f;
                script._line.enabled = true;
            }
            script._line.material.SetColor("_Color", color);
            script._line.material.SetColor("_EmissionColor", color);
            if (hit)
            {
                script._line.SetPosition(1, hit.point);
            }
            else
            {
                script._line.SetPosition(1, direction * range + (Vector2)script.gameObject.transform.position);
            } 
            return hit;
        }

        public IEnumerator IAim(float duration)
        {
            currentState = CurrentState.Aiming;
            yield return new WaitForSeconds(duration);
            script.StartCoroutine(IShoot(shootingDuration));
        }

        public IEnumerator IShoot(float duration)
        {
            currentState = CurrentState.Shooting;
            yield return new WaitForSeconds(duration);
            Shoot(Color.white);
        }

        public IEnumerator ICooldown(float duration)
        {
            currentState = CurrentState.Cooldown;
            script._line.enabled = false;
            yield return new WaitForSeconds(duration);
            currentState = CurrentState.Waiting;
        }
    }

    [Serializable]
    public class Rocket
    {
        public Weapon script;
        public GameObject prefabMissile;
        public Color missileColor;
        public int damages;
        public float lifetime, acceleration, maxSpeed, rotationSpeed, explosionRadius, explosionForce, cooldownDuration, precision;
        public bool showViseur, equiped;
        public Vector2 spawnOffset;
        public CurrentState currentState;

        public void Shoot(Vector2 pos, Color color)
        {
            if (currentState == CurrentState.Waiting)
            {
                GameObject missile = Instantiate(prefabMissile);
                missile.transform.position = script.gameObject.transform.TransformPoint(spawnOffset);
                missile.transform.rotation = script.gameObject.transform.rotation;
                missile.GetComponent<Missile>()._launcher = script;
                missile.GetComponent<Missile>()._target = AimCheck(pos, color);
                script.StartCoroutine(this.ICooldown(this.cooldownDuration));
            }
        }

        public void Shoot(float angle, Color color)
        {
            if (currentState == CurrentState.Waiting)
            {
                GameObject missile = Instantiate(prefabMissile);
                missile.transform.position = script.gameObject.transform.TransformPoint(spawnOffset);
                missile.transform.rotation = script.gameObject.transform.rotation;
                missile.GetComponent<Missile>()._launcher = script;
                missile.GetComponent<Missile>()._target = AimCheck(angle, color);
                script.StartCoroutine(this.ICooldown(this.cooldownDuration));
            }
        }

        public void Shoot(GameObject target)
        {
            if (currentState == CurrentState.Waiting)
            {
                GameObject missile = Instantiate(prefabMissile);
                missile.transform.position = script.gameObject.transform.TransformPoint(spawnOffset);
                missile.transform.rotation = script.gameObject.transform.rotation;
                missile.GetComponent<Missile>()._launcher = script;
                missile.GetComponent<Missile>()._target = target;
                script.StartCoroutine(this.ICooldown(this.cooldownDuration));
            }
        }

        public GameObject AimCheck(Vector2 pos, Color color)
        {
            List<GameObject> enemiesInSceneCopy = new List<GameObject>(GameManager._gameManager._enemiesInScene);
            if (enemiesInSceneCopy.Count > 0 && currentState == CurrentState.Waiting)
            {
                float distance = Vector2.Distance(pos, (Vector2)enemiesInSceneCopy[0].transform.position);
                GameObject closestEnemy = null;
                foreach (var item in enemiesInSceneCopy)
                {
                    if (Vector2.Distance(pos, (Vector2)item.transform.position) < distance || closestEnemy == null)
                    {
                        distance = Vector2.Distance(pos, (Vector2)item.transform.position);
                        closestEnemy = item;
                    }
                }
                script._viseur.GetComponent<SpriteRenderer>().color = color;
                script._viseur.SetActive(this.showViseur);
                script._viseur.transform.position = closestEnemy.transform.position;
                script._viseur.transform.localScale = new Vector3(Mathf.Max(closestEnemy.transform.localScale.x, closestEnemy.transform.localScale.y), Mathf.Max(closestEnemy.transform.localScale.x, closestEnemy.transform.localScale.y), 1f) * 2f;
                return closestEnemy;
            }
            else
            {
                script._viseur.SetActive(false);
                return null;
            }
        }

        public GameObject AimCheck(float angle, Color color)
        {
            List<GameObject> enemiesInSceneCopy = new List<GameObject>(GameManager._gameManager._enemiesInScene);
            if (enemiesInSceneCopy.Count > 0 && currentState == CurrentState.Waiting)
            {
                float distance = Mathf.Abs(angle - Vector2.Angle(Vector2.right, (Vector2)enemiesInSceneCopy[0].transform.position - (Vector2)script.gameObject.transform.position));
                GameObject closestEnemy = null;
                foreach (var item in enemiesInSceneCopy)
                {
                    if (Mathf.Abs(angle - Vector2.Angle(Vector2.right, (Vector2)item.transform.position - (Vector2)script.gameObject.transform.position)) < distance || closestEnemy == null)
                    {
                        distance = Mathf.Abs(angle - Vector2.Angle(Vector2.right, (Vector2)item.transform.position - (Vector2)script.gameObject.transform.position));
                        closestEnemy = item;
                    }
                }
                script._viseur.GetComponent<SpriteRenderer>().color = color;
                script._viseur.SetActive(this.showViseur);
                script._viseur.transform.position = closestEnemy.transform.position;
                script._viseur.transform.localScale = new Vector3(Mathf.Max(closestEnemy.transform.localScale.x, closestEnemy.transform.localScale.y), Mathf.Max(closestEnemy.transform.localScale.x, closestEnemy.transform.localScale.y), 1f) * 2f;
                return closestEnemy;
            }
            else
            {
                script._viseur.SetActive(false);
                return null;
            }
        }

        public IEnumerator ICooldown(float duration)
        {
            currentState = CurrentState.Cooldown;
            if (script._viseur != null)
            {
                script._viseur.SetActive(false);
            }
            yield return new WaitForSeconds(duration);
            currentState = CurrentState.Waiting;
        }
    }

    void Awake()
    {
        switch (_currentMode)
        {
            case CurrentMode.Fixed:
                ChangeHull(_currentMode);
                break;
            case CurrentMode.Drone:
                ChangeHull(_currentMode);
                transform.position = _mainUser.transform.position + Vector3.up * _orbitDistance;
                break;
            case CurrentMode.AssaultDrone:
                ChangeHull(_currentMode);
                transform.position = _mainUser.transform.position + Vector3.up * _orbitDistance;
                break;
        }
    }

    void Update()
    {
        _line.SetPosition(0, transform.position);
    }

    void FixedUpdate()
    {
        switch (_currentWeapon)
        {
            case CurrentWeapon.Laser:
                _laser.equiped = true;
                _rocket.equiped = false;
                _currentWeaponState = _laser.currentState;
                if (_currentMode == CurrentMode.Drone)
                {
                    switch (_currentWeaponState)
                    {
                        case CurrentState.Cooldown:
                            DroneOrbit(_currentTarget);
                            break;
                        case CurrentState.Waiting:
                            DroneOrbit(_currentTarget);
                            StartCoroutine(_laser.IAim(_laser.aimingDuration));
                            break;
                        case CurrentState.Aiming:
                            if (_currentTarget.TryGetComponent<Rigidbody2D>(out Rigidbody2D body))
                            {
                                DroneOrbit(((Vector2)_currentTarget.transform.position + body.linearVelocity * _laser.shootingDuration - (Vector2)_mainUser.transform.position).normalized);
                            }
                            else
                            {
                                DroneOrbit(_currentTarget);
                            }
                            _laser.AimCheck(Color.red);
                            break;
                        case CurrentState.Shooting:
                            _laser.AimCheck(Color.yellow);
                            break;
                    }
                }
                else if (_currentMode == CurrentMode.Fixed && _currentWeaponState == CurrentState.Cooldown)
                {
                    if (_anim.IsPlaying("LaserShooting"))
                    {
                        _laser.AimCheck(_laser.lastShotColor);
                    }
                }
                break;
            case CurrentWeapon.Rocket:
                _laser.equiped = false;
                _rocket.equiped = true;
                _currentWeaponState = _rocket.currentState;
                if (_currentMode == CurrentMode.Drone)
                {
                    DroneOrbit(_currentTarget);
                    if (_currentWeaponState == CurrentState.Waiting)
                    {
                        _rocket.Shoot(_currentTarget);
                    }
                }
                break;
        }
    }

    public void DroneOrbit(Vector2 target)
    {
        if (_currentMode == CurrentMode.Drone || _currentMode == CurrentMode.AssaultDrone)
        {
            float angleTarget = GameManager.DirectionToAngle(target);
            float angleWeapon = GameManager.DirectionToAngle((Vector2)(transform.position - _mainUser.transform.position).normalized);
            transform.RotateAround(_mainUser.transform.position, Vector3.forward, Mathf.DeltaAngle(angleWeapon, angleTarget) * Time.fixedDeltaTime * _orbitSpeed);
        }
    }

    public void DroneOrbit(GameObject target)
    {
        if (_currentMode == CurrentMode.Drone || _currentMode == CurrentMode.AssaultDrone)
        {
            float angleTarget = GameManager.DirectionToAngle((Vector2)(target.transform.position - _mainUser.transform.position).normalized);
            float angleWeapon = GameManager.DirectionToAngle((Vector2)(transform.position - _mainUser.transform.position).normalized);
            transform.RotateAround(_mainUser.transform.position, Vector3.forward, Mathf.DeltaAngle(angleWeapon, angleTarget) * Time.fixedDeltaTime * _orbitSpeed);
        }
    }

    public void ChangeHull(CurrentMode mode)
    {
        switch (mode)
        {
            case CurrentMode.Fixed:
                _hull.GetComponent<SpriteRenderer>().sprite = _fixedHull;
                _hull.transform.localPosition = Vector3.up * -0.65F;
                _hull.transform.localScale = Vector3.one * 2.75f;
                _propels.SetActive(false);
                break;
            case CurrentMode.Drone:
                _hull.GetComponent<SpriteRenderer>().sprite = _droneHull;
                _hull.transform.localPosition = Vector3.up * -0.4f;
                _hull.transform.localScale = Vector3.one * 1.75f;
                _propels.SetActive(true);
                break;
            case CurrentMode.AssaultDrone:
                _hull.GetComponent<SpriteRenderer>().sprite = _droneHull;
                _hull.transform.localPosition = Vector3.up * -0.4f;
                _hull.transform.localScale = Vector3.one * 1.75f;
                _propels.SetActive(true);
                break;
        }
    }
}
