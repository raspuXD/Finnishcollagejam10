using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutomaticUISound : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        if(button != null )
        {
            button.onClick.AddListener(() => AudioManager.Instance.PlaySFX("UIButton"));
        }

        Toggle toggle = GetComponent<Toggle>();
        if( toggle != null )
        {
            toggle.onValueChanged.AddListener((isOn) => AudioManager.Instance.PlaySFX("UIButton"));
        }

        TMP_Dropdown dropdown = GetComponent<TMP_Dropdown>();
        if( dropdown != null )
        {
            dropdown.onValueChanged.AddListener((value) => AudioManager.Instance.PlaySFX("UIButton"));
        }
    }
}