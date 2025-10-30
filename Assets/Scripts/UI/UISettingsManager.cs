using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using DG.Tweening;
using TMPro;
using System.Collections;
using System;

public class UISettingsManager : MonoBehaviour
{
    [Header("Settings Panels")]
    public GameObject audioPanel;
    public GameObject displayPanel;
    public GameObject controlsPanel;

    [Header("Audio")]
    public AudioMixer masterMixer;
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterAmountText;
    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicAmountText;
    public Slider ambientVolumeSlider;
    public TextMeshProUGUI ambientAmountText;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxAmountText;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public Toggle cameraBobToggle;
    public Toggle cameraSmoothToggle;
    public Toggle jumpScareToggle;

    [Header("Controls")]
    public Slider lookSensitivitySlider;
    public TextMeshProUGUI lookSensitivityAmountText;

    [Header("UI Feedback")]
    public TextMeshProUGUI savedText;

    [Header("UI Settings")]
    [SerializeField] private GameObject settingsPanel;

    private Animator animator;
    private bool isPanelOpen = false;
    private bool isAnimating = false;

    // ปรับให้ตรงกับชื่อใน Animator ของคุณ
    private const string OpenStateName = "Open_Setting_ui_anim";
    private const string CloseStateName = "Close_Setting_ui_anim";

    public static Action OnSettingApplied;

    void Start()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("[UISettingsManager] settingsPanel is not assigned!");
            enabled = false;
            return;
        }

        animator = settingsPanel.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[UISettingsManager] Animator component not found on settingsPanel!");
            enabled = false;
            return;
        }

        if (savedText != null)
        {
            savedText.alpha = 0;
            savedText.gameObject.SetActive(false);
        }

        // Listener อัปเดตค่าแบบเรียลไทม์
        masterVolumeSlider.onValueChanged.AddListener((v) => UpdateAmountText(masterAmountText, v));
        musicVolumeSlider.onValueChanged.AddListener((v) => UpdateAmountText(musicAmountText, v));
        ambientVolumeSlider.onValueChanged.AddListener((v) => UpdateAmountText(ambientAmountText, v));
        sfxVolumeSlider.onValueChanged.AddListener((v) => UpdateAmountText(sfxAmountText, v));
        lookSensitivitySlider.onValueChanged.AddListener((v) => UpdateAmountText(lookSensitivityAmountText, v));

        LoadSettings();

        // อัปเดตค่าเริ่มต้น
        UpdateAmountText(masterAmountText, masterVolumeSlider.value);
        UpdateAmountText(musicAmountText, musicVolumeSlider.value);
        UpdateAmountText(ambientAmountText, ambientVolumeSlider.value);
        UpdateAmountText(sfxAmountText, sfxVolumeSlider.value);
        UpdateAmountText(lookSensitivityAmountText, lookSensitivitySlider.value);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !isAnimating)
        {
            ToggleSettingsPanel();
        }
    }

    public void ToggleSettingsPanel()
    {
        if (isAnimating) return;
        isAnimating = true;

        isPanelOpen = !isPanelOpen;

        if (isPanelOpen)
            OpenSettingsPanel();
        else
            CloseSettingsPanel();
    }

    public void OpenSettingsPanel()
    {
        // if (settingsPanel == null || animator == null) return;

        // Debug.Log("Opening Settings Panel...");

        // // 🟢 ทำให้อนิเมชันเล่นแม้ Time.timeScale = 0
        // settingsPanel.SetActive(true);
        // animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        // animator.SetTrigger("Open");
        // GameManager.Instance?.ChangeState(GameState.Paused);
        if (settingsPanel == null || animator == null)
        {
            isAnimating = false; // ป้องกันการค้าง
            return;
        }

        // เรียกใช้ Coroutine แทน
        StartCoroutine(OpenAnimationCoroutine());
    }

    public void CloseSettingsPanel()
    {
        // if (animator == null || settingsPanel == null) return;

        // Debug.Log("Closing Settings Panel...");

        // // 🟢 ทำให้อนิเมชันเล่นแม้ Time.timeScale = 0
        // animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        // animator.SetTrigger("Close");
        // GameManager.Instance?.ExitPause();
        // GameManager.Instance?.ChangeState(GameState.Playing);
        if (animator == null || settingsPanel == null)
        {
            isAnimating = false; // ป้องกันการค้าง
            return;
        }

        // เรียกใช้ Coroutine แทน
        StartCoroutine(CloseAnimationCoroutine());
    }

    private IEnumerator OpenAnimationCoroutine()
    {
        Debug.Log("Opening Settings Panel...");

        settingsPanel.SetActive(true);
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetTrigger("Open");
        GameManager.Instance?.ChangeState(GameState.Paused);

        int layer = 0;
        yield return null;

        Debug.Log("Waiting for Open state...");
        yield return new WaitUntil(() =>
        {
            var cur = animator.GetCurrentAnimatorStateInfo(layer);
            var next = animator.GetNextAnimatorStateInfo(layer);
            return cur.IsName(OpenStateName) || next.IsName(OpenStateName);
        });
        Debug.Log("...Found Open state!");

        Debug.Log("Waiting for transition to end...");
        yield return new WaitWhile(() => animator.IsInTransition(layer));
        Debug.Log("...Transition ended!");

        // --- 🔴 นี่คือส่วนที่แก้ไข 🔴 ---
        Debug.Log("Waiting for animation to finish...");
        // เราจะรอตราบใดที่ State ยังเป็น "Open" และเวลา animation ยังไม่ถึง 1 (ยังเล่นไม่จบ)
        while (animator.GetCurrentAnimatorStateInfo(layer).IsName(OpenStateName) &&
          animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1.0f)
        {
            // รอเฟรมถัดไป
            yield return null;
        }
        Debug.Log("...Animation finished or state changed!");
        // -------------------------------

        isAnimating = false; // ปลดล็อคให้กดปุ่มได้
        isPanelOpen = true;
        Debug.Log("✅ Open animation finished");
    }

    private IEnumerator CloseAnimationCoroutine()
    {
        Debug.Log("Closing Settings Panel...");

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetTrigger("Close");

        // --- 🔴 นี่คือจุดที่แก้ไข 🔴 ---
        // เราต้องเรียกทั้งสองคำสั่งนี้ (เหมือนที่โค้ดเดิมของคุณเคยทำ)
        GameManager.Instance?.ExitPause();
        GameManager.Instance?.ChangeState(GameState.Playing);
        // --------------------------------

        int layer = 0;
        yield return null;

        Debug.Log("Waiting for Close state...");
        yield return new WaitUntil(() =>
        {
            var cur = animator.GetCurrentAnimatorStateInfo(layer);
            var next = animator.GetNextAnimatorStateInfo(layer);
            return cur.IsName(CloseStateName) || next.IsName(CloseStateName);
        });
        Debug.Log("...Found Close state!");

        Debug.Log("Waiting for transition to end...");
        yield return new WaitWhile(() => animator.IsInTransition(layer));
        Debug.Log("...Transition ended!");

        Debug.Log("Waiting for animation to finish...");
        while (animator.GetCurrentAnimatorStateInfo(layer).IsName(CloseStateName) &&
               animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1.0f)
        {
            yield return null;
        }
        Debug.Log("...Animation finished or state changed!");

        settingsPanel.SetActive(false);
        isPanelOpen = false;
        isAnimating = false; // ปลดล็อคให้กดปุ่มได้

        Debug.Log("✅ Close animation finished and panel deactivated");
    }

    // private IEnumerator WaitForOpenAnimation()
    // {
    //     int layer = 0;
    //     yield return null;

    //     yield return new WaitUntil(() =>
    //     {
    //         var cur = animator.GetCurrentAnimatorStateInfo(layer);
    //         var next = animator.GetNextAnimatorStateInfo(layer);
    //         return cur.IsName(OpenStateName) || next.IsName(OpenStateName);
    //     });

    //     yield return new WaitWhile(() => animator.IsInTransition(layer));
    //     yield return new WaitWhile(() => animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f);

    //     isAnimating = false;
    //     isPanelOpen = true;
    //     Debug.Log("✅ Open animation finished");
    //     GameManager.Instance.ChangeState(GameState.Paused);
    // }

    // private IEnumerator DeactivatePanelAfterAnimation()
    // {
    //     int layer = 0;
    //     yield return null;

    //     yield return new WaitUntil(() =>
    //     {
    //         var cur = animator.GetCurrentAnimatorStateInfo(layer);
    //         var next = animator.GetNextAnimatorStateInfo(layer);
    //         return cur.IsName(CloseStateName) || next.IsName(CloseStateName);
    //     });

    //     yield return new WaitWhile(() => animator.IsInTransition(layer));
    //     yield return new WaitWhile(() => animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f);

    //     settingsPanel.SetActive(false);
    //     animator.enabled = false;
    //     animator.SetBool("IsOpen", false);
    //     isPanelOpen = false;
    //     isAnimating = false;

    //     Debug.Log("✅ Close animation finished and panel deactivated");
    //     GameManager.Instance?.ExitPause();
    // }

    // ================== SETTINGS LOGIC ====================

    public void ShowAudioPanel() { audioPanel.SetActive(true); displayPanel.SetActive(false); controlsPanel.SetActive(false); }
    public void ShowDisplayPanel() { audioPanel.SetActive(false); displayPanel.SetActive(true); controlsPanel.SetActive(false); }
    public void ShowControlsPanel() { audioPanel.SetActive(false); displayPanel.SetActive(false); controlsPanel.SetActive(true); }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        bool isFullscreen = fullscreenToggle.isOn;
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("CameraBob", cameraBobToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("CameraSmooth", cameraSmoothToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("JumpScare", jumpScareToggle.isOn ? 1 : 0);

        PlayerPrefs.SetFloat("LookSensitivity", lookSensitivitySlider.value);
        PlayerPrefs.Save();

        Screen.fullScreen = isFullscreen;

        Debug.Log("✅ Settings saved via Apply button.");
        ShowSavedMessage();

        OnSettingApplied?.Invoke();
    }

    private void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 50f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 50f);
        float ambientVol = PlayerPrefs.GetFloat("AmbientVolume", 50f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 50f);

        masterVolumeSlider.value = masterVol;
        musicVolumeSlider.value = musicVol;
        ambientVolumeSlider.value = ambientVol;
        sfxVolumeSlider.value = sfxVol;

        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetAmbientVolume(ambientVol);
        SetSFXVolume(sfxVol);

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;
        fullscreenToggle.isOn = isFullscreen;
        Screen.fullScreen = isFullscreen;

        cameraBobToggle.isOn = PlayerPrefs.GetInt("CameraBob", 1) == 1;
        cameraSmoothToggle.isOn = PlayerPrefs.GetInt("CameraSmooth", 1) == 1;
        jumpScareToggle.isOn = PlayerPrefs.GetInt("JumpScare", 1) == 1;

        lookSensitivitySlider.value = PlayerPrefs.GetFloat("LookSensitivity", 0.5f);

        Debug.Log("🔄 Settings loaded from PlayerPrefs.");

        OnSettingApplied?.Invoke();
    }

    public void SetMasterVolume(float volume)
    {
        float db = (volume <= 0) ? -80f : Mathf.Log10(volume / 100f) * 20;
        masterMixer.SetFloat("MasterVolume", db);
    }

    public void SetMusicVolume(float volume)
    {
        float db = (volume <= 0) ? -80f : Mathf.Log10(volume / 100f) * 20;
        masterMixer.SetFloat("MusicVolume", db);
    }

    public void SetAmbientVolume(float volume)
    {
        float db = (volume <= 0) ? -80f : Mathf.Log10(volume / 100f) * 20;
        masterMixer.SetFloat("AmbientVolume", db);
    }

    public void SetSFXVolume(float volume)
    {
        float db = (volume <= 0) ? -80f : Mathf.Log10(volume / 100f) * 20;
        masterMixer.SetFloat("SFXVolume", db);
    }

    public void SetDefaults()
    {
        masterVolumeSlider.value = 50;
        musicVolumeSlider.value = 50;
        ambientVolumeSlider.value = 50;
        sfxVolumeSlider.value = 50;

        fullscreenToggle.isOn = false;
        cameraBobToggle.isOn = true;
        cameraSmoothToggle.isOn = true;
        jumpScareToggle.isOn = false;

        lookSensitivitySlider.value = 0.5f;

        SetMasterVolume(50);
        SetMusicVolume(50);
        SetAmbientVolume(50);
        SetSFXVolume(50);

        Screen.fullScreen = false;

        Debug.Log("🔁 Settings reset to default values.");
    }

    private void ShowSavedMessage()
    {
        if (savedText == null) return;

        savedText.gameObject.SetActive(true);
        savedText.DOKill();

        Color c = savedText.color;
        c.a = 0;
        savedText.color = c;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);
        seq.Append(DOTween.To(() => savedText.color.a, x =>
        {
            Color col = savedText.color;
            col.a = x;
            savedText.color = col;
        }, 1f, 0.3f))
           .AppendInterval(1.2f)
           .Append(DOTween.To(() => savedText.color.a, x =>
           {
               Color col = savedText.color;
               col.a = x;
               savedText.color = col;
           }, 0f, 0.8f))
           .OnComplete(() => savedText.gameObject.SetActive(false));
    }

    private void UpdateAmountText(TextMeshProUGUI textElement, float value)
    {
        if (textElement == null) return;

        if (textElement == lookSensitivityAmountText)
            textElement.text = value.ToString("F2");
        else
            textElement.text = Mathf.RoundToInt(value).ToString();
    }
}
