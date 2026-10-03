using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using JetBrains.Annotations;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.iOS;

public class GameManager : MonoBehaviour
{
    [Header("General")]
    public static GameManager _gameManager;
    public enum GameState {InMenu = 0, InWaveGame = 1, InPause = 2}
    public enum CurrentDevice {Gamepad, Keyboard, Mobile, Unknow}
    public GameState _gameState;
    public CurrentDevice _currentDevice;
    public Texture2D _cursorSprite;
    public GameObject _asteroidPrefab;
    public GameObject _borderPrefab;
    public List<GameObject> _enemiesPrefabs = new List<GameObject>();
    public List<GameObject> _enemiesInScene = new List<GameObject>();

    [Header("Menu")]
    public GameObject firstSelected;
    public TMP_Text _versionDisplay;
    public GameObject _irisOfEye;

    [Header("WaveMode")]
    public WaveManager _waveManager;
    public bool inWave = true;

    [Header("Particles")]
    public GameObject _explosionParticle;
    public GameObject _hitParticle;
    public GameObject _dashParticle;
    public GameObject _spawnParticle;

    [Serializable]
    public class WaveManager
    {
        public GameManager script;
        public int _waveNumber;
        public float _waveChangeDuration, _enemyNumberCoef;
        public GameObject _border, _asteroidGroup;
        public List<GameObject> _enemiesToSpawn = new List<GameObject>();

        public IEnumerator IWaveChange()
        {
            script.inWave = false;
            _waveNumber++;
            _enemiesToSpawn.Clear();
            for (int restDifficulty = _waveNumber; restDifficulty > 0;)
            {
                GameObject enemyToAdd = script._enemiesPrefabs[0];
                foreach(var item in script._enemiesPrefabs)
                {
                    if (item.GetComponent<Enemy>()._difficulty <= Mathf.CeilToInt(restDifficulty * _enemyNumberCoef) && item.GetComponent<Enemy>()._difficulty > enemyToAdd.GetComponent<Enemy>()._difficulty)
                    {
                        enemyToAdd = item;
                    }
                }
                restDifficulty -= enemyToAdd.GetComponent<Enemy>()._difficulty;
                _enemiesToSpawn.Add(enemyToAdd);
            }
            yield return new WaitForSeconds(_waveChangeDuration);
            Player._player.HealthChange(1);
            Player._player._waveUI.text = "Wave " + _waveNumber;
            foreach(var item in _enemiesToSpawn)
            {
                SearchSpawnEnemy(item, 7.5f);
            }
        }

        public void SearchSpawnEnemy(GameObject enemy, float distance = 5f)
        {
            bool canSpawn = true;
            for (int i = 1; i < Mathf.RoundToInt(this._border.transform.localScale.x / 2 - Mathf.Max(enemy.transform.localScale.x, enemy.transform.localScale.y) - 2.5f); i++)
            {
                canSpawn = true;
                Vector2 spawnPos = Random.insideUnitCircle * i;
                if (Vector2.Distance(spawnPos, Player._player.gameObject.transform.position) < distance)
                {
                    canSpawn = false;
                }
                if (script._enemiesInScene.Count > 0)
                {
                    foreach (var item in script._enemiesInScene)
                    {
                        if (Vector2.Distance(spawnPos, item.transform.position) < distance)
                        {
                            canSpawn = false;
                            break;
                        }
                    }
                }
                if (canSpawn)
                {
                    script.StartCoroutine(ISpawnEnemy(enemy, spawnPos));
                    break;
                }
            }
            if (!canSpawn)
            {
                this._border.transform.localScale += Vector3.one * 5f;
                this.SearchSpawnEnemy(enemy, distance);
            }
        }

        public IEnumerator ISpawnEnemy(GameObject enemy, Vector2 pos)
        {
            GameObject particle = Instantiate(script._spawnParticle);
            particle.transform.position = pos;
            particle.transform.localScale = Vector3.one * (enemy.transform.localScale.x + enemy.transform.localScale.y) * 0.5f;
            GameObject enemyInstance = Instantiate(enemy);
            enemyInstance.SetActive(false);
            enemyInstance.transform.position = pos;
            script._enemiesInScene.Add(enemyInstance);
            yield return new WaitForSeconds(2f);
            Destroy(particle.GetComponent<PointEffector2D>());
            Destroy(particle.GetComponent<Collider2D>());
            particle.GetComponentInChildren<ParticleSystemForceField>().gravity = -3f;
            particle.GetComponentInChildren<ParticleSystemForceField>().gravityFocus = 0f;
            particle.GetComponentInChildren<ParticleSystemForceField>().endRange = 0.5f;
            particle.GetComponentInChildren<ParticleSystemForceField>().drag = 0f;
            enemyInstance.SetActive(true);
            script.inWave = true;
        }

        public WaveManager(GameManager script, int waveNumber, float waveChangeDuration, float enemyNumberCoef)
        {
            this.script = script;
            this._waveNumber = waveNumber;
            this._waveChangeDuration = waveChangeDuration;
            this._enemyNumberCoef = enemyNumberCoef;
        }
    }

    public void Awake()
    {
        if (_gameManager == null)
        {
            _gameManager = this;
            _gameState = GameState.InMenu;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(_gameManager.gameObject);
            _gameManager = this;
            _gameState = GameState.InMenu;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    void FixedUpdate()
    {
        if (_gameState == GameState.InWaveGame && _enemiesInScene.Count <= 0 && inWave)
        {
            StartCoroutine(_waveManager.IWaveChange());
        }
    }

    void OnEnable()
    {
        InputSystem.onEvent += DeviceTest;
    }

    public void OnDisable()
    {
        _gameManager = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        InputSystem.onEvent -= DeviceTest;
    }

    public void DeviceTest(InputEventPtr eventPtr, InputDevice device)
    {
        switch (device)
        {
            case Gamepad:
                _currentDevice = CurrentDevice.Gamepad;
                break;
            case Keyboard:
                _currentDevice = CurrentDevice.Keyboard;
                break;
            case Mouse:
                _currentDevice = CurrentDevice.Keyboard;
                break;
            case Touchscreen:
                _currentDevice = CurrentDevice.Mobile;
                break;
        }
        print($"le device est {_currentDevice}");
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Menu":
                _gameState = GameState.InMenu;
                if (firstSelected != null && _currentDevice == CurrentDevice.Gamepad)
                {
                    EventSystem.current.SetSelectedGameObject(firstSelected);
                }
                _versionDisplay.text = Application.version;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
            case "WaveGame":
                _gameState = GameState.InWaveGame;
                _waveManager = new WaveManager(this, _waveManager._waveNumber, _waveManager._waveChangeDuration, _waveManager._enemyNumberCoef);
                _waveManager._border = Instantiate(_borderPrefab);
                _waveManager._border.transform.position = Vector2.zero;
                _waveManager._border.transform.localScale = new Vector3(50f, 50f, 50f);
                _waveManager._asteroidGroup = new GameObject("AsteroidGroup");
                _waveManager._asteroidGroup.transform.position = Vector2.zero;
                _waveManager._asteroidGroup.transform.localScale = Vector3.one;
                for (int i = 0; i < Random.Range(5, 21); i++)
                {
                    GameObject asteroid = Instantiate(_asteroidPrefab);
                    asteroid.transform.position = new Vector3(Random.Range(-75f, 75f), Random.Range(-75f, 75f), Random.Range(200f, 30f));
                    asteroid.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                    asteroid.transform.localScale = Vector3.one * Random.Range(10f, 30f);
                    float randomColor = Random.Range(0.25f, 0.3f);
                    asteroid.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", new Color(randomColor, randomColor, randomColor, 1f));
                    asteroid.transform.SetParent(_waveManager._asteroidGroup.transform);
                }
                if (_currentDevice == CurrentDevice.Keyboard)
                {
                    Cursor.SetCursor(_cursorSprite, new Vector2(_cursorSprite.width / 2, _cursorSprite.height / 2), CursorMode.Auto);
                }
                StartCoroutine(_waveManager.IWaveChange());
                break;
        }
    }

    public bool IsOnUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void CameraShake(int duration, float magnitude)
    {
        IEnumerator ICameraShake()
        {
            for (int i = 0; i < duration; i++)
            {
                Camera.main.transform.position += (Vector3)Random.insideUnitCircle * magnitude;
                yield return new WaitForSeconds(0.05f);
            }
        }
        StartCoroutine(ICameraShake());
    }

    public void GameStateChange(GameState state)
    {
        _gameState = state;
    }

    public void GameStateChange(int state)
    {
        _gameState = (GameState)state;
    }

    public void SceneChange(string scene)
    {
        SceneManager.LoadScene(scene);
        StopAllCoroutines();
        _enemiesInScene.Clear();
    }

    public static float DirectionToAngle(Vector2 direction)
    {
        if (Mathf.Sign(direction.x) == 1)
        {
            return -Vector2.Angle(Vector2.up, direction);
        }
        else
        {
            return Vector2.Angle(Vector2.up, direction);
        }
        //return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
    }

    public void OpenLink(string link)
    {
        Application.OpenURL(link);
    }

    public void ExitGame()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            Application.Quit();
        }
    }
}
