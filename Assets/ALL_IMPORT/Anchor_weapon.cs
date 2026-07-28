using UnityEngine;

public class Anchor_weapon: MonoBehaviour
{
    Vector3 localPos;
    Quaternion localRot;

    void Start()
    {
        localPos = transform.localPosition;
        localRot = transform.localRotation;
    }

    void LateUpdate()
    {
        transform.localPosition = localPos;
        transform.localRotation = localRot;
    }
}
