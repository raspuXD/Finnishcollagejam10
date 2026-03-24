using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class FadeIn : MonoBehaviour
{
    public Sprite fadeSprite;
    public float fadeSpeed = 1f;
    public float waitAfterFade = 0f;
    public UnityEvent onFadeComplete;
    public bool hasToBeCalled = true;
    public bool wannaDestoryAfterWaited = true;

    Canvas canvas;
    Image fadeImage;
    bool isFading = false;
    bool isWaiting = false;
    float waitTimer = 0f;

    public GameObject instantiatedCanvas;

    void Start()
    {
        if (!hasToBeCalled)
        {
            StartFadeIn();
        }
    }

    void Update()
    {
        if (isFading)
        {
            Color color = fadeImage.color;
            color.a += fadeSpeed * Time.deltaTime;
            color.a = Mathf.Clamp01(color.a);
            fadeImage.color = color;

            if (color.a >= 1f)
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
                if(wannaDestoryAfterWaited)
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

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void StartFadeIn()
    {
        if(!isFading)
        {
            CreateCanvasAndImage();
            isFading = true;
        }
    }
}
