using UnityEngine;

public class PencilDrawEffect : MonoBehaviour
{
    public SpriteRenderer pencilTip;
    public AudioSource pencilSound;
    public AudioClip[] writingVariations;
    private int activeStroke = -1;
    private int variationSequence;
    [Range(0, 1)] public float soundVolume = 0.22f;
    private SpriteRenderer artwork;
    private PencilStrokeProfile profile;
    private MaterialPropertyBlock properties;
    private static readonly int Progress = Shader.PropertyToID("_DrawProgress");
    private static readonly int Mask = Shader.PropertyToID("_StrokeMask");

    public void Begin(SpriteRenderer renderer, PencilStrokeProfile strokeProfile)
    {
        Stop();
        artwork = renderer;
        profile = strokeProfile;
        activeStroke = -1;
        if (pencilSound != null && writingVariations != null && writingVariations.Length > 0)
            pencilSound.clip = writingVariations[variationSequence++ % writingVariations.Length];
        properties = properties ?? new MaterialPropertyBlock();
        artwork.GetPropertyBlock(properties);
        properties.SetTexture(Mask, profile.orderMask);
        properties.SetFloat(Progress, 0);
        artwork.SetPropertyBlock(properties);
        if (pencilSound != null && pencilSound.clip != null && Application.isPlaying)
        {
            pencilSound.volume = 0;
            pencilSound.loop = true;
            pencilSound.Play();
        }
        Tick(0, 0);
    }

    public void Tick(float progress, float deltaTime)
    {
        if (artwork == null || profile == null) return;
        properties.SetFloat(Progress, Mathf.Clamp01(progress));
        artwork.SetPropertyBlock(properties);
        Vector2 uv = profile.Evaluate(progress, out bool penDown, out float lift);
        if (pencilTip != null)
        {
            Sprite sprite = artwork.sprite;
            Vector2 local = (Vector2.Scale(uv, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
            Vector3 position = artwork.transform.TransformPoint(local);
            position.y += lift;
            pencilTip.transform.position = position;
            pencilTip.transform.rotation = Quaternion.Euler(0, 0, -25);
            pencilTip.sortingLayerID = artwork.sortingLayerID;
            pencilTip.sortingOrder = artwork.sortingOrder + 10;
            pencilTip.enabled = progress < 1;
        }
        if (pencilSound != null)
        {
            float envelope = 0;
            if (penDown)
                for (int index = 0; index < profile.strokes.Length; index++)
                {
                    var stroke = profile.strokes[index];
                    if (progress >= stroke.start && progress <= stroke.end)
                    {
                        if (activeStroke != index)
                        {
                            activeStroke = index;
                            if (writingVariations != null && writingVariations.Length > 0)
                            {
                                pencilSound.volume = 0;
                                pencilSound.clip = writingVariations[variationSequence++ % writingVariations.Length];
                                pencilSound.pitch = 0.985f + (variationSequence % 3) * 0.015f;
                                if (Application.isPlaying) pencilSound.Play();
                            }
                        }
                        envelope = Mathf.Clamp01(Mathf.Min(progress - stroke.start, stroke.end - progress) * profile.duration / 0.015f);
                        break;
                    }
                }
            pencilSound.volume = Mathf.MoveTowards(pencilSound.volume, soundVolume * envelope, deltaTime * soundVolume / 0.015f);
            if (!penDown) pencilSound.volume = 0;
        }
    }

    public void Finish()
    {
        if (artwork != null)
        {
            properties.SetFloat(Progress, 1);
            artwork.SetPropertyBlock(properties);
        }
        Stop();
    }

    public static void ShowComplete(SpriteRenderer renderer)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetFloat(Progress, 1);
        renderer.SetPropertyBlock(block);
    }

    public void Stop()
    {
        if (pencilTip != null) pencilTip.enabled = false;
        if (pencilSound != null) { pencilSound.Stop(); pencilSound.volume = 0; }
        artwork = null;
        profile = null;
    }

    void OnDisable() => Stop();
    void OnApplicationPause(bool paused)
    {
        if (pencilSound == null) return;
        if (paused) pencilSound.Pause();
        else if (artwork != null) pencilSound.UnPause();
    }
    void OnApplicationFocus(bool focused) => OnApplicationPause(!focused);
}
