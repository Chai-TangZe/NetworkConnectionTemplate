using UnityEngine;

[ExecuteAlways]
public class ScaleObjectToFitBoxSyx : MonoBehaviour
{
    public bool update = false;
    public Vector3 boxSize = new Vector3(5, 5, 5);  // 手动设定的Box初始大小
    public Color boxColor = Color.green;  // Box的颜色


    private void OnDrawGizmos()
    {
        // 绘制Box的线框
        Gizmos.color = boxColor;
        Gizmos.DrawWireCube(transform.position, boxSize);
        if (update)
        {
            update = false;
            ScaleTargetObject(targetObject);
        }
    }
    public Transform targetObject;
    Vector3 scale = Vector3.one;
    // 将目标物体等比例缩放到Box内部
    public void ScaleTargetObject(Transform targetObject)
    {
        scale = targetObject.localScale;
        targetObject.rotation = transform.rotation;
        this.targetObject = targetObject;
        Vector3 finalBoxSize = boxSize;
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("目标物体或其子物体上没有Renderer组件！");
            return;
        }

        // 获取所有Renderer的包围盒
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponent<ParticleSystem>())
            {
                continue;
            }
            combinedBounds.Encapsulate(renderer.bounds);
        }

        // 计算目标物体的当前尺寸
        Vector3 targetSize = combinedBounds.size;

        // 计算缩放比例
        float scaleFactor = Mathf.Min(finalBoxSize.x / targetSize.x, finalBoxSize.y / targetSize.y, finalBoxSize.z / targetSize.z);
        if (targetObject.localScale == Vector3.zero)
        {
            targetObject.localScale = Vector3.one;
        }
        // 应用缩放比例到目标物体的整体Transform
        targetObject.localScale *= scaleFactor;

        // 计算包围盒中心相对于目标物体原点的偏移量
        Vector3 boundsCenterOffset = combinedBounds.center - targetObject.position;

        // 将目标物体移动到Box的中心
        Vector3 newPosition = transform.position - boundsCenterOffset * scaleFactor;
        targetObject.position = newPosition;
    }

    public void ReductionSize()
    {
        if (targetObject)
            targetObject.localScale = scale;
    }
}
