using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GUIManager : MonoBehaviour
{
    [SerializeField] private Texture2D aimCursor;
    [SerializeField] private TankAgent player;
    [SerializeField] private UnityEngine.UI.Image AimPointerImage;
    [SerializeField] private UnityEngine.UI.Image CooldownForeground;
    [SerializeField] private UnityEngine.UI.Image RedState;
    [SerializeField] private UnityEngine.UI.Image YellowState;
    [SerializeField] private UnityEngine.UI.Text HealthText;
    [SerializeField] private UnityEngine.UI.Text TimerText;
    [SerializeField] private UnityEngine.UI.Image HealthForeground;
    [SerializeField] private UnityEngine.UI.Image RedTeamPoints;
    [SerializeField] private UnityEngine.UI.Image YellowTeamPoints;
    [SerializeField] private UnityEngine.UI.Image DeadPanel;
    [SerializeField] private UnityEngine.UI.Image VictoryPanel;
    [SerializeField] private UnityEngine.UI.Image DefeatPanel;
    [SerializeField] private UnityEngine.UI.Image PausePanel;
    [SerializeField] private UnityEngine.UI.Image TiePanel;
    [SerializeField] private UnityEngine.UI.Text RespawnCooldownText;
    [SerializeField] private EnvController env;
    [SerializeField] private InputController inputController;

    public UnityEvent PausedEvent;

    float playerRespawnCooldown = 0.0f;
    float endRoundTimer = 0.0f;

    private Vector2 CursorHotspot;
    void Start()
    {
        CursorHotspot = new Vector2(aimCursor.width / 2.0f, aimCursor.height / 2.0f);
        Cursor.SetCursor(aimCursor, CursorHotspot, CursorMode.Auto);
        player.DiedEvent.AddListener(PlayerDiedHandler);
        player.RespawnEvent.AddListener(PlayerRespawnHandler);
        env.RedWonEvent.AddListener(RedWonHandler);
        env.YellowWonEvent.AddListener(YellowWonHandler);
        env.TieEvent.AddListener(TieHandler);
        PausedEvent.AddListener(PauseGameHandler);
    }

    void Update()
    {
        if (playerRespawnCooldown != 0.0f)
        {
            playerRespawnCooldown = Mathf.Max(0.0f, playerRespawnCooldown - Time.deltaTime);
            RespawnCooldownText.text = ((int)playerRespawnCooldown).ToString();
        }

        if(endRoundTimer != 0.0f)
        {
            endRoundTimer = Mathf.Max(0.0f, endRoundTimer - Time.deltaTime);
            if (endRoundTimer == 0.0f) EndGameHandler();
        }


        AimPointerImage.transform.position = player.GetScreenSpaceAimPos();
        int playerHealth = (int)player.getHealth();
        HealthText.text = playerHealth.ToString() + "%";
        if (HealthForeground != null) HealthForeground.fillAmount = player.getHealth() / 100;
        TimeSpan timeSpan = TimeSpan.FromSeconds(env.getRemainingTime());
        TimerText.text = timeSpan.ToString(@"mm\:ss");
        if(playerRespawnCooldown == 0.0f)
            CooldownForeground.fillAmount = player.getCooldown() / 3.0f;
        RedTeamPoints.fillAmount = env.RedTeamPoints / 100.0f;
        YellowTeamPoints.fillAmount = env.YellowTeamPoints / 100.0f;

        if (env.getStateNum() > 0)
        {
            YellowState.fillAmount = env.getStateNum() / 10.0f;
            RedState.fillAmount = 0;
        }
        else if(env.getStateNum() < 0)
        {
            RedState.fillAmount = Mathf.Abs(env.getStateNum()) / 10.0f;
            YellowState.fillAmount = 0;
        }
        else
        {
            YellowState.fillAmount = 0;
            RedState.fillAmount = 0;
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            PausedEvent.Invoke();
        }
    }

    void PlayerDiedHandler()
    {
        DeadPanel.gameObject.SetActive(true);
        playerRespawnCooldown = EnvController.RespawnCooldown;
        CooldownForeground.fillAmount = 1.0f;
    }

    void PlayerRespawnHandler()
    {
        DeadPanel.gameObject.SetActive(false);
    }

    void RedWonHandler()
    {
        endRoundTimer = 10.0f;
        DefeatPanel.gameObject.SetActive(true);
    }

    void YellowWonHandler()
    {
        endRoundTimer = 10.0f;
        VictoryPanel.gameObject.SetActive(true);
    }

    void TieHandler()
    {
        endRoundTimer = 10.0f;
        TiePanel.gameObject.SetActive(true);
    }

    public void EndGameHandler()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }

    void PauseGameHandler()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        PausePanel.gameObject.SetActive(true);
        inputController.gameObject.SetActive(false);
    }

    public void ContinueGameHandler()
    {
        Cursor.SetCursor(aimCursor, CursorHotspot, CursorMode.Auto);
        PausePanel.gameObject.SetActive(false);
        inputController.gameObject.SetActive(true);
    }
}
