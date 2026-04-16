using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject CameraObj;
    const string INPUT_MOUSE_X = "Mouse X";
    const string INPUT_MOUSE_Y = "Mouse Y";
    public bool UseOperation = true;
    [Header("目标")]
    public Transform Target;

    [Header("切换第三人称视角")]
    public bool IsFollow = true;

    [Header("旋转速度")]
    [Range(0.01f , 1f)]
    public float orbitSpeed = 0.5f;

    [Header("仰视角限制")]
    [Range(0.01f , 89.9f)]
    public float EulerLimitUp = 50f;
    [Header("俯视角限制")]
    [Range(0.01f , 89.9f)]
    public float EulerLimitDown = 5f;

    [Header("距离目标距离")]
    public float distance = 5f;

    [Header("镜头碰撞反应时间")]
    public float CameraCollideResponseSpeed = 0.1f;

    [Header("镜头偏移量")]
    public Vector3 Offset = Vector3.up + Vector3.right;

    void Start()
    {
        if (Target)
            distance = Vector3.Distance(transform.position , Target.position);
    }
    public void SetTarget(Transform Target)
    {
        this.Target = Target;
    }
    public void SetRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }
    public Quaternion GetRotation()
    {
        return transform.rotation;
    }
    void LateUpdate()
    {
        if (!UseOperation|| Target == null)
        {
            return;
        }
        if (Input.GetMouseButton(1))
        {
            float rot_x = Input.GetAxis(INPUT_MOUSE_X);
            float rot_y = -Input.GetAxis(INPUT_MOUSE_Y);
            CameraRotate(rot_x * orbitSpeed * 4 , rot_y * orbitSpeed * 4);
        }
        if (IsFollow)
        {
            CameraUpdateLocation();
            Vector3 pos = transform.localRotation * ( Vector3.forward * -distance ) + Target.position;
            transform.position = pos;//等于位置
        }
        else
        {
            transform.position = Target.position;
            transform.GetChild(0).position = transform.position;
        }
    }
    public void CameraRotate(float rot_x , float rot_y)
    {
        if (Target == null)
        {
            return;
        }
        Vector3 eulerRotation = transform.localRotation.eulerAngles;
        eulerRotation.x += rot_y * orbitSpeed;
        eulerRotation.y += rot_x * orbitSpeed;
        eulerRotation.z = 0f;
        #region Limit
        if (eulerRotation.x > 90 - EulerLimitDown && eulerRotation.x < 200)
        {
            eulerRotation.x = 90 - EulerLimitDown;
        }
        //270--360
        if (eulerRotation.x > 200 && eulerRotation.x < 270 + EulerLimitUp)
        {
            eulerRotation.x = 270 + EulerLimitUp;
        }
        #endregion
        transform.localRotation = Quaternion.Euler(eulerRotation);
        if (IsFollow)
            transform.position = transform.localRotation * ( Vector3.forward * -distance ) + Target.position;
        else
        {
            transform.position = Target.position;
        }
    }

    /// <summary>
    /// 碰到物体后刷新摄像机位置
    /// </summary>
    void CameraUpdateLocation()
    {
        Vector3 CameraLocation = transform.position;
        CameraLocation += transform.right * Offset.x;
        CameraLocation += transform.up * Offset.y;
        CameraObj.transform.position = CameraLocation;
        Target.LookAt(CameraObj.transform);

        RaycastHit Hit;
        Vector3 Ray = Target.forward;

        if (Physics.Raycast(Target.position , Ray , out Hit , distance + 1))
        {
            Vector3 cameraLocation = Hit.point + CameraObj.transform.forward * Offset.z;
            CameraObj.transform.position = cameraLocation;
        }
    }
}
