using UnityEngine;

public class MagnetAnimation : MonoBehaviour
{
    public Animator animator;

    [Header("Target")]
    public Transform magnetTransform; // assign the object that should move

    [Header("Float Settings")]
    public float floatSpeed = 3f;

    [Header("Height Clamp")]
    public float minHeight = -0.2f;
    public float maxHeight = 0.2f;

    private Vector3 originalLocalPos;
    private float targetOffsetY;
    private bool isFloating;

    void Start()
    {
        if (magnetTransform == null)
            magnetTransform = transform;

        originalLocalPos = magnetTransform.localPosition;
        PickNewTarget();
    }

    void Update() 
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        bool shouldFloat = stateInfo.IsName("Base Layer.RepelIdle") || 
                           stateInfo.IsName("Base Layer.AttractIdle");

        if (shouldFloat)
        {
            FloatMotion();
            isFloating = true;
        }
        else if (isFloating)
        {
            ResetPosition();
            isFloating = false;
        }
    }

    void FloatMotion()
    {
        float currentY = magnetTransform.localPosition.y;
        float targetY = originalLocalPos.y + targetOffsetY;

        float newY = Mathf.MoveTowards(currentY, targetY, floatSpeed * Time.deltaTime);

        magnetTransform.localPosition = new Vector3(
            originalLocalPos.x,
            newY,
            originalLocalPos.z
        );

        if (Mathf.Abs(newY - targetY) < 0.01f)
        {
            PickNewTarget();
        }
    }

    void PickNewTarget()
    {
        targetOffsetY = Random.Range(minHeight, maxHeight);
    }

    void ResetPosition()
    {
        magnetTransform.localPosition = originalLocalPos;
    }
}