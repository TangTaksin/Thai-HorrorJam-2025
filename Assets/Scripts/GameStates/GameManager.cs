using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    GameState lastState;

    bool _haveflashlight;
    public bool haveflashlight 
    { 
        get 
        { 
            return _haveflashlight; 
        }
        set 
        {
            if (value != _haveflashlight)
            {
                _haveflashlight = value;
                OnVariableUpdate?.Invoke();
            }
        } 
    }

    bool _haveSprayCan;
    public bool haveSprayCan
    {
        get { return _haveSprayCan; }
        set 
        {
            if (value != _haveSprayCan)
            {
                _haveSprayCan = value;
                OnVariableUpdate?.Invoke();
            }
        }
    }

    int _gasCount;
    public int gasCount
    {
        get { return _gasCount; }
        set
        {
            if (value != gasCount)
            {
                gasCount = value;
                OnVariableUpdate?.Invoke();
            }
        }
    }

    public static Action<GameState> OnStateChanged;
    public static Action OnVariableUpdate;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    private void Update()
    {

    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        if (CurrentState == GameState.Playing || CurrentState == GameState.MainMenu)
        {
            lastState = CurrentState;
        }
        CurrentState = newState;

        switch (CurrentState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                AudioListener.pause = false;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Start or resume gameplay
                break;

            case GameState.Inspect:
                Time.timeScale = 0f;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                AudioListener.pause = true;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Show game over screen
                break;
        }

        OnStateChanged?.Invoke(CurrentState);
    }

    public void ChangeStateToPlaying()
    {
        ChangeState(GameState.Playing);
    }

    public void ExitPause()
    {
        ChangeState(lastState);
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    Inspect,
    Paused,
    GameOver
}
