using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleFootsteps : MonoBehaviour
{
    [Header("Footstep Audio")]
    public AudioClip[] footstepClips;
    public float stepInterval = 0.45f;
    public float volume = 0.7f;

    [Header("Movement Check")]
    public CharacterController characterController;
    public float minMoveSpeed = 0.1f;

    [SerializeField]private AudioSource audioSource;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (characterController == null || footstepClips.Length == 0)
            return;

        bool isMoving = characterController.velocity.magnitude > minMoveSpeed;
        bool isGrounded = characterController.isGrounded;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        int index = Random.Range(0, footstepClips.Length);
        audioSource.PlayOneShot(footstepClips[index], volume);
    }
}