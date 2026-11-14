using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CameraControlsTutorial : MonoBehaviour
{
    private enum Objective
    {
        Orbit,
        Pan,
        Focus,
        Restart
    }

    [Header("References")]
    [SerializeField] private CameraOrbit cameraOrbit;
    [SerializeField] private CameraZoom cameraZoom;
    [SerializeField] private RestartHelper restartHelper;
    [SerializeField] private PlayableDirector transitionIn;
    [SerializeField] private PlayableDirector endMenu;
    [SerializeField] private CanvasGroup presentation;

    [Header("Scene Text")]
    [SerializeField] private TextMeshProUGUI introduction;
    [Tooltip("Orbit, pan, focus, and restart, in that order. Edit their wording and style in the scene.")]
    [SerializeField] private TextMeshProUGUI[] objectives;

    [Header("Animation")]
    [SerializeField] private float charactersPerSecond = 20f;
    [SerializeField] private float letterFadeDuration = 0.18f;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float introductionHoldDuration = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float completedAlpha = 0.3f;

    [Header("Objectives")]
    [Min(0.1f)]
    [SerializeField] private float orbitDegrees = 8f;
    [Min(0.01f)]
    [SerializeField] private float panDistance = 0.5f;

    // A one-use checkpoint for scene reloads, never a persistent save-game value.
    private static string resumeScenePath;
    private static int resumeCompletedCount;
    private static bool resumeIntroductionComplete;

    private TutorialLetterFade introductionFade;
    private TutorialLetterFade[] objectiveFades;
    private int completedCount;
    private int activeObjective = -1;
    private float controlProgress;
    private bool introductionComplete;
    private bool restarting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        ClearCheckpoint();

        // Also clear an interrupted restart if the player leaves for another scene.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != resumeScenePath)
            ClearCheckpoint();
    }

    private static void ClearCheckpoint()
    {
        resumeScenePath = null;
        resumeCompletedCount = 0;
        resumeIntroductionComplete = false;
    }

    private void Awake()
    {
        if (cameraOrbit == null || cameraZoom == null || restartHelper == null ||
            endMenu == null || presentation == null || introduction == null ||
            objectives == null || objectives.Length != 4 ||
            System.Array.Exists(objectives, text => text == null))
        {
            Debug.LogError("Camera tutorial needs its camera, restart, end menu, and five scene text references.", this);
            enabled = false;
            return;
        }

        if (resumeScenePath == gameObject.scene.path)
        {
            completedCount = Mathf.Clamp(resumeCompletedCount, 0, objectives.Length);
            introductionComplete = resumeIntroductionComplete;
        }

        ClearCheckpoint();

        presentation.alpha = 0f;
        SetCameraControls(false);
        cameraOrbit.SetPerpendicularFocus(!introductionComplete);
    }

    private void OnEnable()
    {
        cameraOrbit.OrbitPerformed += OnOrbit;
        cameraOrbit.PanPerformed += OnPan;
        cameraOrbit.FocusPerformed += OnFocus;
        restartHelper.OnRestartRequested.AddListener(OnRestartRequested);
    }

    private void Start()
    {
        introductionFade = new TutorialLetterFade(introduction, charactersPerSecond, letterFadeDuration);
        objectiveFades = new TutorialLetterFade[objectives.Length];
        for (int i = 0; i < objectives.Length; i++)
            objectiveFades[i] = new TutorialLetterFade(objectives[i], charactersPerSecond, letterFadeDuration);

        StartCoroutine(RunTutorial());
    }

    private void OnDisable()
    {
        if (cameraOrbit != null)
        {
            cameraOrbit.OrbitPerformed -= OnOrbit;
            cameraOrbit.PanPerformed -= OnPan;
            cameraOrbit.FocusPerformed -= OnFocus;
        }

        if (restartHelper != null)
            restartHelper.OnRestartRequested.RemoveListener(OnRestartRequested);

        StopAllCoroutines();
        introductionFade?.Dispose();
        if (objectiveFades != null)
        {
            foreach (TutorialLetterFade fade in objectiveFades)
                fade?.Dispose();
        }

        if (presentation != null)
            presentation.alpha = 0f;
    }

    private IEnumerator RunTutorial()
    {
        // StickToWall finds the starting block in LateUpdate, after Start.
        yield return null;
        cameraOrbit.FocusOnCurrentBlock(true);

        // Wait for the same transition-in used by Tutorial 1.
        while (transitionIn != null && transitionIn.state == PlayState.Playing &&
               transitionIn.time < transitionIn.duration)
            yield return null;

        presentation.alpha = 1f;

        if (!introductionComplete)
        {
            yield return Reveal(introduction, introductionFade);
            yield return new WaitForSeconds(introductionHoldDuration);

            // Reveal depth with the exact angle offset used by the F key.
            cameraOrbit.SetPerpendicularFocus(false);
            cameraOrbit.FocusOnCurrentBlock();
            while (cameraOrbit.IsFocusing)
                yield return null;

            yield return new WaitForSeconds(introductionHoldDuration);
            yield return Fade(introductionFade, 1f, 0f);
            introductionComplete = true;
        }

        yield return RestoreCompletedObjectives();

        SetCameraControls(true);

        while (completedCount < objectives.Length)
        {
            int index = completedCount;
            activeObjective = index;
            controlProgress = 0f;

            yield return Reveal(objectives[index], objectiveFades[index]);
            while (completedCount == index)
                yield return null;

            yield return Fade(objectiveFades[index], 1f, completedAlpha);
        }

        activeObjective = -1;
        endMenu.Play();
    }

    private IEnumerator RestoreCompletedObjectives()
    {
        if (completedCount == 0)
            yield break;

        // Restore the dimmed lines together, then type the next objective normally.
        presentation.alpha = 0f;
        for (int i = 0; i < completedCount; i++)
            objectiveFades[i].SetProgress(float.PositiveInfinity, completedAlpha);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            presentation.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        presentation.alpha = 1f;
    }

    private IEnumerator Reveal(TMP_Text text, TutorialLetterFade fade)
    {
        float duration = Mathf.Max(0, text.textInfo.characterCount - 1) / Mathf.Max(1f, charactersPerSecond)
            + Mathf.Max(0.01f, letterFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fade.SetProgress(elapsed);
            yield return null;
        }

        fade.SetProgress(float.PositiveInfinity);
    }

    private IEnumerator Fade(TutorialLetterFade fade, float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fade.SetProgress(float.PositiveInfinity, Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }

        fade.SetProgress(float.PositiveInfinity, to);
    }

    private void OnOrbit(float degrees)
    {
        if (activeObjective != (int)Objective.Orbit || restarting)
            return;

        controlProgress += degrees;
        if (controlProgress >= orbitDegrees)
            CompleteObjective();
    }

    private void OnPan(float distance)
    {
        if (activeObjective != (int)Objective.Pan || restarting)
            return;

        controlProgress += distance;
        if (controlProgress >= panDistance)
            CompleteObjective();
    }

    private void OnFocus()
    {
        if (activeObjective == (int)Objective.Focus && !restarting)
            CompleteObjective();
    }

    private void CompleteObjective()
    {
        completedCount++;
        activeObjective = -1;
    }

    private void OnRestartRequested()
    {
        if (activeObjective == (int)Objective.Restart && !restarting)
            CompleteObjective();

        PreserveProgress();
    }

    public void PreserveProgress()
    {
        if (restarting)
            return;

        resumeScenePath = gameObject.scene.path;
        resumeCompletedCount = completedCount;
        resumeIntroductionComplete = introductionComplete;

        restarting = true;
        StopAllCoroutines();
        SetCameraControls(false);
    }

    public void ResetProgress()
    {
        ClearCheckpoint();
        restarting = true;
        StopAllCoroutines();
        SetCameraControls(false);
    }

    private void SetCameraControls(bool value)
    {
        cameraOrbit.SetManualControlsEnabled(value);
        cameraZoom.enabled = value;
    }
}
