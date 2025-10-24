using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    [SerializeField] private GameObject pauseMenuUI;

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
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (CurrentState == GameState.Playing)
            {
                HandlePause();
            }
            else if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                pauseMenuUI.SetActive(false);
                Time.timeScale = 1f; // Resume the game
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;

        switch (CurrentState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;

                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                // Start or resume gameplay
                break;
            case GameState.Paused:
                HandlePause();
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                // Show game over screen
                break;
        }

    }

    private void HandlePause()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}
