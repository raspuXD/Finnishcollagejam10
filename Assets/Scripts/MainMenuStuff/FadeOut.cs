using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class FadeOut : MonoBehaviour
{
    public Sprite fadeSprite;
    public float fadeSpeed = 1f;
    public float waitAfterFade = 0f;
    public float delayBeforeStart = 0f;
    public UnityEvent onFadeComplete;
    public bool hasToBeCalled = true;
    public bool wannaDestoryAfterWaited = true;
    Canvas canvas;
    Image fadeImage;
    bool isFading = false;
    bool isWaiting = false;
    bool isDelaying = false;
    float delayTimer = 0f;
    float waitTimer = 0f;

    public GameObject instantiatedCanvas;

    void Start()
    {
        CreateCanvasAndImage();
        if (!hasToBeCalled)
        {
            if (delayBeforeStart > 0f)
            {
                isDelaying = true;
                delayTimer = delayBeforeStart;
            }
            else
            {
                StartFadeOut();
            }
        }
    }

    void Update()
    {
        if (isDelaying)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 1f);
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                isDelaying = false;
                StartFadeOut();
            }
            return;
        }

        if (isFading)
        {
            Color color = fadeImage.color;
            color.a -= fadeSpeed * Time.deltaTime;
            color.a = Mathf.Clamp01(color.a);
            fadeImage.color = color;

            if (color.a <= 0f)
            {
                isFading = false;
                isWaiting = true;
                waitTimer = waitAfterFade;
            }
        }
        else if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                if (wannaDestoryAfterWaited)
                {
                    Destroy(instantiatedCanvas);
                }
                onFadeComplete.Invoke();
            }
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

    public void StartFadeOut()
    {
        if(instantiatedCanvas == null)
        {
            CreateCanvasAndImage();
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        fadeImage.raycastTarget = true;
        isFading = true;
    }
}
