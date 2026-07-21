using System.Collections;
using UnityEngine;

/// A single cereal piece on the board.
/// Animates itself toward a target position (coroutine-based tweening).
public class CerealPiece : MonoBehaviour
{
    public int X;
    public int Y;
    public int Type;

    public bool Moving { get; private set; }

    SpriteRenderer sr;

    public SpriteRenderer Renderer
    {
        get
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            return sr;
        }
    }

    public void SetGridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void MoveTo(Vector3 target, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(target, duration));
    }

    IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        Moving = true;
        Vector3 start = transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            // Ease-out quad: start fast, arrive softly
            p = 1f - (1f - p) * (1f - p);
            transform.position = Vector3.LerpUnclamped(start, target, p);
            yield return null;
        }
        transform.position = target;
        Moving = false;
    }

    /// Quick "pop", then scale down to zero — played before the piece is destroyed.
    public IEnumerator ClearRoutine()
    {
        Moving = true;
        Vector3 baseScale = transform.localScale;
        float t = 0f;
        const float duration = 0.22f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float scale = p < 0.3f
                ? Mathf.Lerp(1f, 1.25f, p / 0.3f)          // puff up first
                : Mathf.Lerp(1.25f, 0f, (p - 0.3f) / 0.7f); // then vanish
            transform.localScale = baseScale * scale;
            yield return null;
        }
        Moving = false;
    }

    /// Small shake for invalid moves.
    public IEnumerator ShakeRoutine()
    {
        Moving = true;
        Vector3 origin = transform.position;
        float t = 0f;
        const float duration = 0.25f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damp = 1f - Mathf.Clamp01(t / duration);
            transform.position = origin + Vector3.right * (Mathf.Sin(t * 60f) * 0.05f * damp);
            yield return null;
        }
        transform.position = origin;
        Moving = false;
    }
}
