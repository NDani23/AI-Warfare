using Google.Protobuf.WellKnownTypes;
using System;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public enum GUIMode
{
    Commander,
    Tank,
    Heli
}

public class GUIManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image AimPointerImage;
    [SerializeField] private UnityEngine.UI.Image RedState;
    [SerializeField] private UnityEngine.UI.Image YellowState;
    [SerializeField] private UnityEngine.UI.Text TimerText;
    [SerializeField] private UnityEngine.UI.Image RedTeamPoints;
    [SerializeField] private UnityEngine.UI.Image YellowTeamPoints;
    [SerializeField] private UnityEngine.UI.Image DeadPanel;
    [SerializeField] private UnityEngine.UI.Image VictoryPanel;
    [SerializeField] private UnityEngine.UI.Image DefeatPanel;
    [SerializeField] private UnityEngine.UI.Image PausePanel;
    [SerializeField] private UnityEngine.UI.Image TiePanel;
    [SerializeField] private UnityEngine.UI.Text RespawnCooldownText;
    [SerializeField] private EnvController env;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraController GameCamera;
    [SerializeField] private TankHUD tankHUD;
    [SerializeField] private HeliHUD heliHUD;

    private GUIMode _GUIMode = GUIMode.Commander;

    private VehicleManager? playerVehicle;

    public VehicleManager PlayerVehicle
    {
        get { return playerVehicle; }
    }

    public UnityEvent PausedEvent;

    float playerRespawnCooldown = 0.0f;
    float endRoundTimer = 0.0f;

    private Vector2 CursorHotspot;
    void Start()
    {
        //player.DiedEvent.AddListener(PlayerDiedHandler);
        //player.RespawnEvent.AddListener(PlayerRespawnHandler);
        env.RedWonEvent.AddListener(RedWonHandler);
        env.YellowWonEvent.AddListener(YellowWonHandler);
        env.TieEvent.AddListener(TieHandler);
        gameManager.ControlModeChangedEvent.AddListener(handleControlModeChanged);
        PausedEvent.AddListener(PauseGameHandler);

        playerVehicle = null;
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


        //AimPointerImage.transform.position = player.GetScreenSpaceAimPos();

        TimeSpan timeSpan = TimeSpan.FromSeconds(env.getRemainingTime());
        TimerText.text = timeSpan.ToString(@"mm\:ss");
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
        //inputController.gameObject.SetActive(false);
    }

    public void ContinueGameHandler()
    {
        PausePanel.gameObject.SetActive(false);
        //inputController.gameObject.SetActive(true);
    }

    private void handleControlModeChanged(bool isPlayerControlled)
    {
        //if (isPlayerControlled)
        //{
        //    if (_GUIMode == GUIMode.Heli)
        //        heliHUD.enableControlHUD();
        //    else if (_GUIMode == GUIMode.Tank)
        //        tankHUD.enableControlHUD();
        //}
        //else
        //{
        //    if (_GUIMode == GUIMode.Heli)
        //        heliHUD.disableControlHUD();
        //    else if (_GUIMode == GUIMode.Tank)
        //        tankHUD.disableControlHUD();
        //}
    }

    public void SwitchGUIMode(VehicleManager vehicleInFocus)
    {

        if (vehicleInFocus == null)
        {
            _GUIMode = GUIMode.Commander;
            tankHUD.gameObject.SetActive(false);
            heliHUD.gameObject.SetActive(false);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (vehicleInFocus.VehicleType == VehicleType.Tank)
        {
            _GUIMode = GUIMode.Tank;
            tankHUD.gameObject.SetActive(true);
            heliHUD.gameObject.SetActive(false);
            tankHUD.vehicle = (TankManager)vehicleInFocus;
        }
        else if (vehicleInFocus.VehicleType == VehicleType.Heli)
        {
            _GUIMode = GUIMode.Heli;
            tankHUD.gameObject.SetActive(false);
            heliHUD.gameObject.SetActive(true);
            heliHUD.Agent = (HeliManager)vehicleInFocus;
        }
    }
}
