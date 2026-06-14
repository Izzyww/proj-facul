using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform m_Target;
    [SerializeField] private Vector3 m_Offset = new Vector3(0f, 1f, -10f);

    public void SetTarget(Transform target)
    {
        m_Target = target;
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (m_Target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                m_Target = player.transform;
            }
            else
            {
                return;
            }
        }

        SnapToTarget();
    }

    private void SnapToTarget()
    {
        Vector3 desiredPosition = m_Target.position + m_Offset;
        desiredPosition.z = m_Offset.z;
        transform.position = desiredPosition;
    }
}
