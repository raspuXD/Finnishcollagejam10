using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class FullFade : MonoBehaviour
{
    public Sprite fadeSprite;
    public float fadeSpeed = 1f;
    public float waitBeforeFadeOut = 0.5f;
    public float waitAfterFadeOut = 0f;
    public UnityEvent onFadeMiddle;
    public UnityEvent onFadeComplete;
    public bool hasToBeCalled = true;
    public bool wannaDestoryAfterWaited = true;
    Canvas canvas;
    Image fadeImage;
    enum FadeState { Idle, FadingIn, WaitingBeforeOut, FadingOut, WaitingAfterOut }
    FadeState fadeState = FadeState.Idle;
    float waitTimer = 0f;

    public GameObject instantiatedCanvas;

    void Start()
    {
        if (!hasToBeCalled)
        {
            StartFullFade();
        }
    }

    void Update()
    {
        switch (fadeState)
        {
            case FadeState.FadingIn:
                UpdateFadeIn();
                break;
            case FadeState.WaitingBeforeOut:
                UpdateWaitBeforeOut();
                break;
            case FadeState.FadingOut:
                UpdateFadeOut();
                break;
            case FadeState.WaitingAfterOut:
                UpdateWaitAfterOut();
                break;
        }
    }

    void UpdateFadeIn()
    {
        Color color = fadeImage.color;
        color.a += fadeSpeed * Time.deltaTime;
        color.a = Mathf.Clamp01(color.a);
        fadeImage.color = color;

        if (color.a >= 1f)
        {
            onFadeMiddle.Invoke();
            waitTimer = waitBeforeFadeOut;
            fadeState = FadeState.WaitingBeforeOut;
        }
    }

    void UpdateWaitBeforeOut()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            fadeState = FadeState.FadingOut;
        }
    }

    void UpdateFadeOut()
    {
        Color color = fadeImage.color;
        color.a -= fadeSpeed * Time.deltaTime;
        color.a = Mathf.Clamp01(color.a);
        fadeImage.color = color;

        if (color.a <= 0f)
        {
            waitTimer = waitAfterFadeOut;
            fadeState = FadeState.WaitingAfterOut;
        }
    }

    void UpdateWaitAfterOut()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            onFadeComplete.Invoke();
            if (wannaDestoryAfterWaited)
            {
                Destroy(instantiatedCanvas);
            }
            Destroy(instantiatedCanvas);
            fadeState = FadeState.Idle;
        }
    }

    void CreateCanvasAndImage()
    {
        instantiatedCanvas = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = instantiatedCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = instantiatedCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imageGO = new GameObject("FadeImage", typeof(Image));
        imageGO.transform.SetParent(instantiatedCanvas.transform, false);

        fadeImage = imageGO.GetComponent<Image>();
        fadeImage.sprite = fadeSprite;
        fadeImage.type = Image.Type.Simple;
        fadeImage.preserveAspect = false;
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void StartFullFade()
    {
        if (instantiatedCanvas == null)
        {
            CreateCanvasAndImage();
        }
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = true;
        fadeState = FadeState.FadingIn;
    }
}
