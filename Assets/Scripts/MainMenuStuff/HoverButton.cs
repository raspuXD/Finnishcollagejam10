using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button button;
    Vector3 originalScale;
    Vector3 targetScale;
    bool hovering;

    [SerializeField] float desiredScale = 1.25f;
    [SerializeField] float scaleSpeed = 10f;

    public TMP_Text buttonText;
    public bool wantTheTextToBeCool;

    void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        targetScale = originalScale * desiredScale;

        if (wantTheTextToBeCool)
            buttonText = GetComponentInChildren<TMP_Text>();

        button.onClick.AddListener(ResetScale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;

        if (wantTheTextToBeCool)
            buttonText.fontStyle |= FontStyles.Underline;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;

        if (wantTheTextToBeCool)
            buttonText.fontStyle &= ~FontStyles.Underline;
    }

    void ResetScale()
    {
        transform.localScale = originalScale;
        hovering = false;
    }

    void Update()
    {
        Vector3 target = hovering ? targetScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * scaleSpeed);
    }
}
