using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    GameState lastState;

    public static Action<GameState> OnStateChanged;

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
        ChangeState(GameState.Playing);
    }

    private void Update()
    {
        
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        lastState = CurrentState;
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

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Start or resume gameplay
                break;

            case GameState.Inspect:
                Time.timeScale = 0f;

                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;

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
