using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DownAnyKey : MonoBehaviour
{
    Button button;
    Text inputtext;
    public KeyCode key;
    [HideInInspector]
    public bool IsChangeKey = false; //是否改键位
    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ButtonClicked);

        inputtext = GetComponentInChildren<Text>();
        inputtext.text = key.ToString(); 

        buttonColor = button.image.color;
    }
    // Update is called once per frame
    void Update()
    {
        if (IsChangeKey)
            getKeyDownCode();
    }
    void ButtonClicked()
    {
        IsChangeKey = true;
        ButtonSelect();
    }
    void ButtonSelect()
    {
        button.image.color = Color.black;
    }
    Color buttonColor;
    void ButtonUnSelect()
    {
        button.image.color = buttonColor;
    }
    public KeyCode getKeyDownCode()
    {
        if (Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode) && keyCode != KeyCode.Mouse0)
                {
                    IsChangeKey = false;
                    ButtonUnSelect();
                    if (keyCode == KeyCode.Escape)
                        return key;
                    key = keyCode;
                    inputtext.text = keyCode.ToString();
                    return keyCode;
                }
            }
            RecoverState();
        }
        return KeyCode.None;
    }
    public void SetKeyValue(int mKey)
    {
        RecoverState(); 
        this.key = (KeyCode)mKey;
        inputtext.text = key.ToString();
    }
    public void RecoverState()
    {
        IsChangeKey = false;
        ButtonUnSelect();
    }
}