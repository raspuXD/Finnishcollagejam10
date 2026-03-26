using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

//basic volume controller so it saves float to audio mixer so user can change the volume of the sfx's and music's
public class VolumeControl : MonoBehaviour
{
    [SerializeField] string volumeParameter = "MasterVolume";
    AudioMixer audioMixer;
    [SerializeField] Slider theSlider;
    [SerializeField] TMP_Text theText;
    [SerializeField] bool isMusic, isSFX;
    [SerializeField] float theNoSavedValue = .2f;

    private void Awake()
    {
        theSlider.onValueChanged.AddListener(HandleSlider);
    }

    private void Start()
    {
        audioMixer = AudioManager.Instance.audioMixer;
        theSlider.value = PlayerPrefs.GetFloat(volumeParameter, theNoSavedValue);
        HandleSlider(theSlider.value);
    }

    void HandleSlider(float value)
    {
        audioMixer.SetFloat(volumeParameter, Mathf.Log10(value) * 30);
        float holeroVolume = theSlider.value * 100f;
        if(isMusic)
        {
            theText.text = "MUSIC VOLUME:" + holeroVolume.ToString("F0") + "%";
        }
        else if(isSFX)
        {
            theText.text = "SFX VOLUME:" + holeroVolume.ToString("F0") + "%";
        }
        else
        {
            theText.text = "MASTER VOLUME:" + holeroVolume.ToString("F0") + "%";
        }
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat(volumeParameter, theSlider.value);
        PlayerPrefs.Save();
    }
}
