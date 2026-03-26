using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MagnetUI : MonoBehaviour
{
    [Header("References")]
    public MagnetController magnet;

    [Header("Icons")]
    public Image magnetIcon;
    public Sprite attractIcon;
    public Sprite repelIcon;
    public Sprite offIcon;

    [Header("Optional Label")]
    public TextMeshProUGUI polarityLabel;

    [Header("Feel")]
    public float iconScalePunch = 1.2f;
    public float iconScaleSpeed = 8f;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private MagnetController.Enabled lastEnabled;
    private MagnetPolarity lastPolarity;

    void Start()
    {
        if (magnetIcon != null)
        {
            baseScale = magnetIcon.transform.localScale;
            targetScale = baseScale;
        }

        // Force first update
        lastEnabled = MagnetController.Enabled.ON; 
        lastPolarity = magnet.currentPolarity == MagnetPolarity.Attract 
                       ? MagnetPolarity.Repel 
                       : MagnetPolarity.Attract;

        UpdateUI();
    }

    void Update()
    {
        if (magnet == null || magnetIcon == null) return;

        // Detect state change
        if (magnet.currentEnabled != lastEnabled || magnet.currentPolarity != lastPolarity)
        {
            UpdateUI();
            targetScale = baseScale * iconScalePunch; // punch on change
        }

        // Smooth scale back
        magnetIcon.transform.localScale = Vector3.Lerp(
            magnetIcon.transform.localScale,
            targetScale,
            iconScaleSpeed * Time.deltaTime
        );

        // Settle back to base after punch
        targetScale = Vector3.Lerp(targetScale, baseScale, iconScaleSpeed * Time.deltaTime);

        lastEnabled  = magnet.currentEnabled;
        lastPolarity = magnet.currentPolarity;
    }

    void UpdateUI()
    {
        if (magnet.currentEnabled == MagnetController.Enabled.OFF)
        {
            magnetIcon.sprite = offIcon;
            magnetIcon.color  = Color.gray;

            if (polarityLabel != null)
                polarityLabel.text = "OFF";

            return;
        }

        if (magnet.currentPolarity == MagnetPolarity.Attract)
        {
            magnetIcon.sprite = attractIcon;
            magnetIcon.color  = Color.white;

            if (polarityLabel != null)
                polarityLabel.text = "ATTRACT";
        }
        else
        {
            magnetIcon.sprite = repelIcon;
            magnetIcon.color  = Color.white;

            if (polarityLabel != null)
                polarityLabel.text = "REPEL";
        }
    }
}