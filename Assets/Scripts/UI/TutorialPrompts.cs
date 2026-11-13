using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class TutorialPrompts : MonoBehaviour
{
    private enum Completion
    {
        Time,
        Walk,
        LandJump,
        CrossGap,
        ReachExit
    }

    [Serializable]
    private class TutorialLine
    {
        [Tooltip("Edit the wording and appearance on this scene TextMeshPro object. Disable its GameObject to skip it.")]
        public TextMeshPro text = null;
        public Completion completion = Completion.Time;
        public float target = 0f;
        public float minimumDisplayTime = 0.7f;
        [Tooltip("Keep this line visible until the following line completes, then fade both together.")]
        public bool keepVisibleWithNext = false;

        [Tooltip("Optionally wait for the player to land on this block before showing the line.")]
        public Transform waitForGroundedBlock = null;
        public UnityEvent onShown = new UnityEvent();
        public UnityEvent onCompleted = new UnityEvent();
    }

    private class HeldPrompt
    {
        public TextMeshPro text;
        public Color color;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayableDirector transitionIn;
    [SerializeField] private TutorialLine[] lines;

    [Header("Animation")]
    [SerializeField] private float charactersPerSecond = 35f;
    [SerializeField] private float letterFadeDuration = 0.18f;
    [SerializeField] private float fadeDuration = 0.35f;

    private GroundDetection groundDetection;
    private Vector3 previousPlayerPosition;
    private float distanceWalked;
    private float jumpStartHeight;
    private bool wasGrounded;
    private bool roseDuringJump;
    private bool landedJump;
    private bool reachedExit;
    private bool initialized;
    private TextMeshPro floatingText;
    private Color32[][] authoredVertexColors;
    private float revealElapsed;
    private float lineAlpha = 1f;
    private float lineStartedAt;
    private bool trackingProgress;
    private List<HeldPrompt> heldPrompts = new List<HeldPrompt>();

    private void Start()
    {
        if (player == null || lines == null || lines.Length == 0)
        {
            Debug.LogError("Tutorial prompts need a player and scene text lines.", this);
            enabled = false;
            return;
        }

        groundDetection = player.GetComponentInChildren<GroundDetection>();
        if (groundDetection == null)
        {
            Debug.LogError("Tutorial prompts need the player's GroundDetection.", this);
            enabled = false;
            return;
        }

        previousPlayerPosition = player.position;
        wasGrounded = groundDetection.IsGrounded();
        jumpStartHeight = Vector3.Dot(player.position, player.up);

        initialized = true;
        BeginSequence();
    }

    private void OnEnable()
    {
        if (initialized)
            BeginSequence();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        trackingProgress = false;
        RestoreCurrentText();
        ClearHeldPrompts();

        // Hide the presentation when this component or its parent is switched off.
        if (lines == null)
            return;

        foreach (TutorialLine line in lines)
        {
            if (line.text != null)
                line.text.renderer.enabled = false;
        }
    }

    private void BeginSequence()
    {
        trackingProgress = false;

        foreach (TutorialLine line in lines)
        {
            if (line.text != null)
                line.text.renderer.enabled = false;
        }

        StartCoroutine(WaitForTransition());
    }

    private IEnumerator WaitForTransition()
    {
        // Allow the director's Play On Awake initialization to finish first.
        yield return null;

        if (transitionIn != null)
        {
            while (transitionIn.state == PlayState.Playing &&
                   transitionIn.time < transitionIn.duration)
                yield return null;
        }

        // Re-enabling the tutorial starts a fresh sequence.
        previousPlayerPosition = player.position;
        distanceWalked = 0f;
        wasGrounded = groundDetection.IsGrounded();
        jumpStartHeight = Vector3.Dot(player.position, player.up);
        roseDuringJump = false;
        landedJump = false;
        reachedExit = false;

        trackingProgress = true;
        yield return ShowLines();
        trackingProgress = false;
    }

    private void Update()
    {
        if (!trackingProgress || groundDetection == null)
            return;

        // Track actual horizontal travel and a completed, rising jump.
        Vector3 movement = player.position - previousPlayerPosition;
        distanceWalked += Vector3.ProjectOnPlane(movement, player.up).magnitude;
        previousPlayerPosition = player.position;

        bool grounded = groundDetection.IsGrounded();
        if (wasGrounded && !grounded)
        {
            jumpStartHeight = Vector3.Dot(player.position, player.up);
            roseDuringJump = false;
        }

        if (!grounded && Vector3.Dot(player.position, player.up) > jumpStartHeight + 0.3f)
            roseDuringJump = true;

        if (!wasGrounded && grounded && roseDuringJump)
            landedJump = true;

        wasGrounded = grounded;
    }

    public void OnExitReached()
    {
        reachedExit = true;
    }

    private IEnumerator ShowLines()
    {
        foreach (TutorialLine line in lines)
        {
            // Keep the authored text, transform, font, and GameObject enabled state.
            if (line.text == null || !line.text.isActiveAndEnabled)
            {
                ClearHeldPrompts();
                continue;
            }

            // Some instructions belong to a particular landing, not just a sequence position.
            while (line.text.isActiveAndEnabled && line.waitForGroundedBlock != null &&
                   (!player.IsChildOf(line.waitForGroundedBlock) || !groundDetection.IsGrounded()))
                yield return null;

            if (!line.text.isActiveAndEnabled)
            {
                ClearHeldPrompts();
                continue;
            }

            // A grouped instruction should wait for a landing after it starts.
            if (line.keepVisibleWithNext)
                landedJump = false;

            floatingText = line.text;
            floatingText.maxVisibleCharacters = int.MaxValue;
            floatingText.ForceMeshUpdate();

            // Keep authored vertex colors, including rich text and gradients.
            TMP_TextInfo info = floatingText.textInfo;
            authoredVertexColors = new Color32[info.meshInfo.Length][];
            for (int i = 0; i < info.meshInfo.Length; i++)
                authoredVertexColors[i] = (Color32[])info.meshInfo[i].colors32.Clone();

            revealElapsed = 0f;
            lineAlpha = 1f;
            lineStartedAt = Time.time;
            floatingText.OnPreRenderText += ApplyLetterFade;
            UpdateLetterFade();
            floatingText.renderer.enabled = true;
            line.onShown?.Invoke();
            int characterCount = floatingText.textInfo.characterCount;

            float elapsed = 0f;
            float typeDuration = Mathf.Max(0, characterCount - 1) / Mathf.Max(1f, charactersPerSecond)
                + Mathf.Max(0.01f, letterFadeDuration);

            // Stagger each letter's own fade instead of fading the whole line.
            while (elapsed < typeDuration)
            {
                if (!floatingText.isActiveAndEnabled)
                    break;

                if (line.completion == Completion.LandJump && landedJump)
                    break;

                elapsed += Time.deltaTime;
                revealElapsed = elapsed;
                UpdateLetterFade();
                yield return null;
            }

            revealElapsed = float.PositiveInfinity;
            UpdateLetterFade();

            float shownAt = Time.time;
            while (floatingText.isActiveAndEnabled &&
                   !(line.completion == Completion.LandJump && landedJump) &&
                   (Time.time - shownAt < line.minimumDisplayTime || !IsComplete(line)))
                yield return null;

            if (floatingText.isActiveAndEnabled && IsComplete(line))
                line.onCompleted?.Invoke();

            if (line.keepVisibleWithNext && floatingText.isActiveAndEnabled)
            {
                // Leave the completed line in place while the next one types.
                heldPrompts.Add(new HeldPrompt { text = floatingText, color = floatingText.color });
                RestoreCurrentText();
                continue;
            }

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                lineAlpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                UpdateLetterFade();
                SetHeldPromptsAlpha(lineAlpha);
                yield return null;
            }

            floatingText.renderer.enabled = false;
            RestoreCurrentText();
            ClearHeldPrompts();
        }

        ClearHeldPrompts();
    }

    private bool IsComplete(TutorialLine line)
    {
        switch (line.completion)
        {
            case Completion.Time:
                return Time.time - lineStartedAt >= line.target;
            case Completion.Walk:
                return distanceWalked >= line.target;
            case Completion.LandJump:
                return landedJump;
            case Completion.CrossGap:
                return player.position.x >= line.target && groundDetection.IsGrounded();
            case Completion.ReachExit:
                return reachedExit;
            default:
                return false;
        }
    }

    private void UpdateLetterFade()
    {
        ApplyLetterFade(floatingText.textInfo);
        floatingText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void ApplyLetterFade(TMP_TextInfo info)
    {
        if (authoredVertexColors == null)
            return;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible)
                continue;

            int material = character.materialReferenceIndex;
            int vertex = character.vertexIndex;
            if (material >= authoredVertexColors.Length ||
                vertex + 3 >= authoredVertexColors[material].Length)
                continue;

            float startsAt = i / Mathf.Max(1f, charactersPerSecond);
            float alpha = Mathf.Clamp01((revealElapsed - startsAt) / Mathf.Max(0.01f, letterFadeDuration))
                * lineAlpha;

            for (int corner = 0; corner < 4; corner++)
            {
                Color32 color = authoredVertexColors[material][vertex + corner];
                color.a = (byte)Mathf.RoundToInt(color.a * alpha);
                info.meshInfo[material].colors32[vertex + corner] = color;
            }
        }
    }

    private void RestoreCurrentText()
    {
        if (floatingText == null)
            return;

        floatingText.OnPreRenderText -= ApplyLetterFade;
        revealElapsed = float.PositiveInfinity;
        lineAlpha = 1f;

        if (authoredVertexColors != null)
            UpdateLetterFade();

        authoredVertexColors = null;
        floatingText = null;
    }

    private void SetHeldPromptsAlpha(float alpha)
    {
        foreach (HeldPrompt prompt in heldPrompts)
        {
            if (prompt.text == null)
                continue;

            Color color = prompt.color;
            color.a *= alpha;
            prompt.text.color = color;
        }
    }

    private void ClearHeldPrompts()
    {
        foreach (HeldPrompt prompt in heldPrompts)
        {
            if (prompt.text == null)
                continue;

            prompt.text.renderer.enabled = false;
            prompt.text.color = prompt.color;
        }

        heldPrompts.Clear();
    }

}
