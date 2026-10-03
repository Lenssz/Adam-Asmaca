using UnityEngine;

// UV paths and their baked per-pixel drawing order share the same timeline.
[CreateAssetMenu(menuName = "Pencil Stickman/Stroke profile")]
public class PencilStrokeProfile : ScriptableObject
{
    [System.Serializable]
    public class Stroke
    {
        public float start;
        public float end;
        public Vector2[] points;
        public float[] distances;

        public Vector2 Evaluate(float fraction)
        {
            if (points == null || points.Length == 0) return Vector2.zero;
            for (int i = 1; i < points.Length; i++)
                if (fraction <= distances[i])
                    return Vector2.Lerp(points[i - 1], points[i], Mathf.InverseLerp(distances[i - 1], distances[i], fraction));
            return points[points.Length - 1];
        }
    }

    public Texture2D orderMask;
    public float duration = 0.5f;
    public Stroke[] strokes;

    public Vector2 Evaluate(float progress, out bool penDown)
        => Evaluate(progress, out penDown, out _);

    public Vector2 Evaluate(float progress, out bool penDown, out float lift)
    {
        penDown = false;
        lift = 0;
        if (strokes == null || strokes.Length == 0) return Vector2.zero;
        for (int i = 0; i < strokes.Length; i++)
        {
            Stroke stroke = strokes[i];
            if (progress < stroke.start)
            {
                Vector2 previous = i == 0 ? stroke.points[0] : strokes[i - 1].Evaluate(1);
                float previousEnd = i == 0 ? 0 : strokes[i - 1].end;
                float travel = Mathf.InverseLerp(previousEnd, stroke.start, progress);
                lift = .018f * Mathf.Sin(travel * Mathf.PI);
                return Vector2.Lerp(previous, stroke.points[0], Mathf.SmoothStep(0, 1, travel));
            }
            if (progress <= stroke.end)
            {
                penDown = true;
                return stroke.Evaluate(Mathf.InverseLerp(stroke.start, stroke.end, progress));
            }
        }
        return strokes[strokes.Length - 1].Evaluate(1);
    }
}
