using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class test_Toggle : MonoBehaviour
{

    Toggle autoBtnToggle;//========================≤‚ ‘
    public CameraController cameraControls;
    // Start is called before the first frame update
    void Start()
    {
        if (!autoBtnToggle)
        {
            autoBtnToggle = GetComponent<Toggle> ();
        }
        autoBtnToggle.onValueChanged.AddListener (( bool value ) => OnToggleClick (autoBtnToggle, value));//========================

    }

    public void OnToggleClick( Toggle toggle, bool value )
    {
        cameraControls. IsFollow = !value;
    }
}
