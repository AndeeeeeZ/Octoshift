using System;
using TMPro;
using UnityEngine;

// Shared by world-space tutorial prompts and the camera-control checklist.
public class TutorialLetterFade : IDisposable
{
    private readonly TMP_Text text;
    private readonly float charactersPerSecond;
    private readonly float letterFadeDuration;
    private readonly Color32[][] authoredVertexColors;

    private float elapsed;
    private float alpha = 1f;

    public TutorialLetterFade(TMP_Text text, float charactersPerSecond, float letterFadeDuration)
    {
        this.text = text;
        this.charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        this.letterFadeDuration = Mathf.Max(0.01f, letterFadeDuration);

        text.maxVisibleCharacters = int.MaxValue;
        text.ForceMeshUpdate();

        // Preserve authored colors, including rich text, sprites, and gradients.
        TMP_TextInfo info = text.textInfo;
        authoredVertexColors = new Color32[info.meshInfo.Length][];
        for (int i = 0; i < info.meshInfo.Length; i++)
            authoredVertexColors[i] = (Color32[])info.meshInfo[i].colors32.Clone();

        text.OnPreRenderText += Apply;
        SetProgress(0f);
    }

    public void SetProgress(float revealElapsed, float lineAlpha = 1f)
    {
        elapsed = revealElapsed;
        alpha = lineAlpha;

        Apply(text.textInfo);
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void Apply(TMP_TextInfo info)
    {
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

            float startsAt = i / charactersPerSecond;
            float letterAlpha = Mathf.Clamp01((elapsed - startsAt) / letterFadeDuration) * alpha;

            for (int corner = 0; corner < 4; corner++)
            {
                Color32 color = authoredVertexColors[material][vertex + corner];
                color.a = (byte)Mathf.RoundToInt(color.a * letterAlpha);
                info.meshInfo[material].colors32[vertex + corner] = color;
            }
        }
    }

    public void Dispose()
    {
        if (text == null)
            return;

        text.OnPreRenderText -= Apply;
        SetProgress(float.PositiveInfinity);
    }
}
