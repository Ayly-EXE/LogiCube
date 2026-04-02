using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SlimeController : MonoBehaviour
{
    [Header("Split")]
    [Min(0)] public int generation = 0;
    [Min(1)] public int maxGenerations = 3;
    [Range(0.3f, 0.95f)] public float childScaleMultiplier = 0.75f;
    [Min(0.05f)] public float childSpawnOffset = 0.45f;
    [Min(0f)] public float childUpOffset = 0.2f;
    [Min(0f)] public float splitImpulse = 2.2f;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 2f;
    [Min(0.05f)] public float minDirectionChangeDelay = 0.8f;
    [Min(0.1f)] public float maxDirectionChangeDelay = 2f;
    [Range(0f, 1f)] public float directionJitter = 0.45f;

    [Header("Jump")]
    [Min(0f)] public float jumpVelocity = 4.5f;
    [Min(0.1f)] public float minJumpDelay = 0.9f;
    [Min(0.1f)] public float maxJumpDelay = 2.1f;
    [Range(0f, 0.6f)] public float forwardJumpBias = 0.18f;

    [Header("Hit")]
    [Min(0f)] public float hitCooldown = 0.08f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private float nextDirectionChangeAt;
    private float nextJumpAt;
    private float nextHitAllowedAt;
    private float groundedUntilTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (maxDirectionChangeDelay < minDirectionChangeDelay)
            maxDirectionChangeDelay = minDirectionChangeDelay;
        if (maxJumpDelay < minJumpDelay)
            maxJumpDelay = minJumpDelay;

        PickNewDirection(force: true);
        ScheduleNextDirectionChange();
        ScheduleNextJump();
    }

    void Update()
    {
        if (Time.time >= nextDirectionChangeAt)
        {
            PickNewDirection(force: false);
            ScheduleNextDirectionChange();
        }

        if (Time.time >= nextJumpAt && IsGrounded())
        {
            Jump();
            ScheduleNextJump();
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        Vector3 desired = moveDirection * moveSpeed;
        velocity.x = desired.x;
        velocity.z = desired.z;
        rb.velocity = velocity;
    }

    public void TakeHit()
    {
        if (Time.time < nextHitAllowedAt)
            return;

        nextHitAllowedAt = Time.time + hitCooldown;

        if (generation >= maxGenerations)
        {
            Destroy(gameObject);
            return;
        }

        SpawnChildren();
        Destroy(gameObject);
    }

    private void SpawnChildren()
    {
        int childGeneration = generation + 1;
        Vector3 childScale = transform.localScale * childScaleMultiplier;

        Vector3 forward = moveDirection.sqrMagnitude > 0.001f ? moveDirection : transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        if (side.sqrMagnitude < 0.001f)
            side = Vector3.right;

        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 spawnPos = transform.position + side * (i * childSpawnOffset) + Vector3.up * childUpOffset;
            GameObject childObject = Instantiate(gameObject, spawnPos, Quaternion.identity);

            SlimeController child = childObject.GetComponent<SlimeController>();
            if (child != null)
            {
                child.generation = childGeneration;
                child.transform.localScale = childScale;
                child.moveSpeed = moveSpeed * 1.08f;
                child.nextHitAllowedAt = Time.time + 0.12f;
                child.PickNewDirection(force: true);
                child.ScheduleNextJump();
            }

            if (childObject.TryGetComponent<Rigidbody>(out Rigidbody childRb))
            {
                childRb.velocity = Vector3.zero;
                Vector3 impulseBasis = child != null ? child.moveDirection : forward;
                Vector3 impulseDir = (impulseBasis + Vector3.up * 0.2f).normalized;
                childRb.AddForce(impulseDir * splitImpulse, ForceMode.VelocityChange);
            }
        }
    }

    private void PickNewDirection(bool force)
    {
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        if (randomDir.sqrMagnitude < 0.04f)
            randomDir = transform.forward;
        randomDir.Normalize();

        if (force || moveDirection.sqrMagnitude < 0.001f)
        {
            moveDirection = randomDir;
            return;
        }

        moveDirection = Vector3.Lerp(moveDirection, randomDir, directionJitter).normalized;
    }

    private void ScheduleNextDirectionChange()
    {
        float delay = Random.Range(minDirectionChangeDelay, maxDirectionChangeDelay);
        nextDirectionChangeAt = Time.time + Mathf.Max(0.05f, delay);
    }

    private void ScheduleNextJump()
    {
        float delay = Random.Range(minJumpDelay, maxJumpDelay);
        nextJumpAt = Time.time + Mathf.Max(0.05f, delay);
    }

    private void Jump()
    {
        Vector3 velocity = rb.velocity;
        if (velocity.y < 0f)
            velocity.y = 0f;
        rb.velocity = velocity;

        Vector3 jumpDirection = (Vector3.up + moveDirection * forwardJumpBias).normalized;
        rb.AddForce(jumpDirection * jumpVelocity, ForceMode.VelocityChange);
    }

    private bool IsGrounded()
    {
        return Time.time <= groundedUntilTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        UpdateGroundedState(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        UpdateGroundedState(collision);
    }

    private void UpdateGroundedState(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.35f)
            {
                groundedUntilTime = Time.time + 0.12f;
                return;
            }
        }
    }
}
