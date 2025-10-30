using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using NaughtyAttributes; // Import NaughtyAttributes

public class TriggerDialogueWithDelay : MonoBehaviour
{
    [BoxGroup("Dialogue Settings")]
    [Required("Text UI must be assigned.")]
    public TextMeshProUGUI textUI;

    [BoxGroup("Dialogue Settings")]
    [ReorderableList] // Easier to manage dialogue lines
    public string[] dialogues;

    [BoxGroup("Dialogue Settings")]
    [MinValue(0.01f)] // Avoid division by zero or instant typing
    public float typingSpeed = 0.03f;

    [BoxGroup("Dialogue Settings")]
    [MinValue(0f)]
    public float delayBetweenLines = 1.5f;

    [BoxGroup("Dialogue Settings")]
    public bool playOnce = false;

    [BoxGroup("Animation Settings")]
    public bool useFadeAnimation = true;

    [BoxGroup("Animation Settings")]
    [ShowIf("useFadeAnimation")] // Only show this if useFadeAnimation is true
    [MinValue(0f)]
    public float fadeDuration = 0.3f;

    // --- Private & Debug Fields ---

    [Foldout("Debug State")]
    [SerializeField, ReadOnly] // Show private field in inspector for debugging
    private bool hasPlayed = false;
    
    [Foldout("Debug State")]
    [SerializeField, ReadOnly]
    private bool isTyping = false;

    private Coroutine dialogueCoroutine;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (useFadeAnimation && textUI != null)
        {
            canvasGroup = textUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = textUI.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartDialogue();
        }
    }

    /// <summary>
    /// Public method to start the dialogue sequence.
    /// </summary>
    public void StartDialogue()
    {
        // Don't restart if already typing
        if (isTyping)
            return;

        if (playOnce && hasPlayed)
            return;
        
        // Ensure textUI is active before starting
        if (textUI != null)
        {
            textUI.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("TextUI is not assigned!");
            return;
        }

        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = StartCoroutine(PlayDialogues());
    }

    IEnumerator PlayDialogues()
    {
        isTyping = true;

        for (int i = 0; i < dialogues.Length; i++)
        {
            // BUG FIX: Check if textUI or this component was destroyed
            if (textUI == null || this == null)
            {
                Debug.LogWarning("Dialogue target (TextUI or self) was destroyed. Halting dialogue.");
                isTyping = false;
                yield break;
            }

            yield return StartCoroutine(TypeText(dialogues[i]));

            // BUG FIX: Check again after typing, in case it was destroyed during
            if (this == null) 
            {
                isTyping = false;
                yield break;
            }

            yield return new WaitForSeconds(delayBetweenLines);
        }

        hasPlayed = true;

        // Fade out
        if (useFadeAnimation && canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeDuration);
        }

        // Wait for fade to finish before clearing text
        yield return new WaitForSeconds(fadeDuration);
        
        if (textUI != null) // Check again before clearing
            textUI.text = "";

        isTyping = false;

        // Reset for next time (if not playOnce)
        if (canvasGroup != null) 
            canvasGroup.alpha = 1f;
    }

    IEnumerator TypeText(string textToType)
    {
        // BUG FIX: Check if textUI is valid
        if (textUI == null)
        {
            Debug.LogWarning("TextUI was destroyed, cannot type.");
            yield break;
        }
        
        textUI.text = "";

        // Fade in
        if (useFadeAnimation && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration);
        }
        
        // Wait for fade-in only if using animation
        if(useFadeAnimation)
            yield return new WaitForSeconds(fadeDuration);

        // Type out the text
        foreach (char letter in textToType)
        {
            // BUG FIX: Check every letter in case textUI is destroyed mid-typing
            if (textUI == null)
                yield break;
                
            textUI.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void OnDestroy()
    {
        // Clean up DOTween tweens
        if (canvasGroup != null)
            DOTween.Kill(canvasGroup);
        
        // Stop coroutines to be safe (though they stop on destroy anyway)
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);
        isTyping = false;
    }

    // -------------------------------------------------------------------------
    // --- (Thai) วิธีแก้ปัญหา Script ถูกทำลายระหว่างเล่น Dialogue ---
    // A: Script ที่ถูกทำลาย (Destroy) จะหยุดทำงานทันที ไม่สามารถทำงานต่อได้
    // B: วิธีแก้คือ "อย่าเพิ่ง" Destroy(gameObject) โดยตรง
    // C: ให้ Script อื่นที่ต้องการทำลาย Object นี้ เรียกใช้เมธอด DestroySafely() นี้แทน
    //    เช่น: otherScript.GetComponent<TriggerDialogueWithDelay>().DestroySafely();
    // D: เมธอดนี้จะรอให้ Dialogue พูดจนจบก่อน (isTyping == false)
    //    แล้วจึงค่อยทำลาย GameObject ให้อัตโนมัติ
    // -------------------------------------------------------------------------

    /// <summary>
    /// Destroys this GameObject safely, waiting for dialogue to finish if it's playing.
    /// Call this from other scripts instead of Destroy(gameObject).
    /// </summary>
    public void DestroySafely()
    {
        StartCoroutine(DestroyWhenDone());
    }

    private IEnumerator DestroyWhenDone()
    {
        // Wait until the dialogue is no longer typing
        yield return new WaitUntil(() => !isTyping);
        
        // Now it's safe to destroy
        Destroy(gameObject);
    }
    
    // --- Editor Test Button ---

    #if UNITY_EDITOR
    [Button("Test Dialogue", EButtonEnableMode.Playmode)]
    private void TestDialogue()
    {
        // Ensure canvas group is set if we're testing in-editor
        if (canvasGroup == null && useFadeAnimation && textUI != null)
        {
            Awake();
        }
        
        StartDialogue();
    }
    #endif
}
