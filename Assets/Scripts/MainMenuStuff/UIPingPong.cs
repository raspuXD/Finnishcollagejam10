using UnityEngine;

public class UIPingPong : MonoBehaviour
{
    [SerializeField] float startSize = 1f;
    [SerializeField] float maxSize = 1.25f;
    [SerializeField] float minSize = 0.75f;
    [SerializeField] float scaleSpeed = 1f;
    [SerializeField] bool eased;

    Vector3 baseScale;

    void Awake()
    {
        baseScale = Vector3.one * startSize;
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * scaleSpeed, 1f);

        if (eased)
            t = Mathf.SmoothStep(0f, 1f, t);

        float size = Mathf.Lerp(minSize, maxSize, t);
        transform.localScale = baseScale * size;
    }
}
