using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerController : MonoBehaviour
{
    void FixedUpdate()
    {
        isPushingBarrier = (pushTimer > 0f);
        if (pushTimer > 0f) pushTimer -= Time.fixedDeltaTime;

        if (isDashing) return;

        moveInput = moveAction.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude < 0.05f)
        {
            moveInput = Vector2.zero;
        }

        Vector3 moveDirection = new(moveInput.x, 0, moveInput.y);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        bool isTryingToSprint = sprintAction.IsPressed() && moveDirection.sqrMagnitude > 0.1f;
        float currentSpeed = speed;
        bool isSprinting = false;

        if (exhaustionTimer > 0)
        {
            exhaustionTimer -= Time.fixedDeltaTime;
        }
        
        if (regenTimer > 0)
        {
            regenTimer -= Time.fixedDeltaTime;
        }

        bool isAttacking = animator.GetBool(IsAttackingHash);
        if (isAttacking)
        {
            currentSpeed *= 0.5f;
            animator.SetBool(IsPushingHash, false);
        }
        else if (isPushingBarrier)
        {
            currentSpeed *= pushSpeedMultiplier;
            animator.SetBool(IsPushingHash, true);
        }
        else
        {
            animator.SetBool(IsPushingHash, false);

            if (isTryingToSprint && currentEnergy > 0 && exhaustionTimer <= 0)
            {
                isSprinting = true;
                currentSpeed *= sprintSpeedMultiplier;
                currentEnergy -= sprintEnergyCost * Time.fixedDeltaTime;
                
                regenTimer = regenDelay;

                if (currentEnergy <= 0)
                {
                    currentEnergy = 0;
                    exhaustionTimer = exhaustionDelay;
                }
            }
            else if (!isTryingToSprint && exhaustionTimer <= 0 && regenTimer <= 0)
            {
                if (currentEnergy < maxEnergy)
                {
                    currentEnergy += energyRegenRate * Time.fixedDeltaTime;
                    if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
                }
            }
        }

        Vector3 newVelocity = new(moveDirection.x * currentSpeed, playerRb.linearVelocity.y, moveDirection.z * currentSpeed);
        playerRb.linearVelocity = newVelocity;

        Vector3 localMove = transform.InverseTransformDirection(moveDirection);
        animator.SetFloat(HorizontalHash, localMove.x, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat(VerticalHash, localMove.z, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat(MoveSpeedHash, moveDirection.magnitude, 0.15f, Time.fixedDeltaTime);
        
        bool isMoving = moveDirection.sqrMagnitude > 0.1f;
        animator.SetBool(IsSprintingHash, isSprinting && isMoving);

        if (animator.GetBool(IsAttackingHash))
        {
            HandleLook();
        }
        else if (isMoving)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 15f);
        }
    }

    void HandleLook()
    {
        bool isMouseMoving = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f;

        bool gamepadRightStickActive = Gamepad.current != null &&
            Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f;

        if (gamepadRightStickActive && !isMouseMoving)
        {
            Vector2 stickInput = Gamepad.current.rightStick.ReadValue();
            Vector3 lookDir = new(stickInput.x, 0, stickInput.y);
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 25f);
            }
        }
        else
        {
            RotateTowardsMouse();
        }
    }

    public void RotateTowardsMouse()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 pointToLook = ray.GetPoint(rayDistance);
            Vector3 targetDirection = new(pointToLook.x, transform.position.y, pointToLook.z);
            transform.LookAt(targetDirection);
        }
    }

    void OnDash(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive || !canDash || isDashing) return;
        
        if (currentEnergy < dashEnergyCost) return;

        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;
        
        currentEnergy -= dashEnergyCost;
        if (currentEnergy <= 0)
        {
            currentEnergy = 0;
            exhaustionTimer = exhaustionDelay;
        }
        else
        {
            regenTimer = regenDelay;
        }

        Vector2 currentInput = ReadCurrentMovementInput();
        Vector3 dashDirection;

        if (currentInput.sqrMagnitude > 0.1f)
        {
            dashDirection = new Vector3(currentInput.x, 0, currentInput.y).normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }

        Vector3 localDir = transform.InverseTransformDirection(dashDirection);

        string dashStateName;
        if (Mathf.Abs(localDir.z) >= Mathf.Abs(localDir.x))
            dashStateName = localDir.z >= 0 ? "DashForward" : "DashBack";
        else
            dashStateName = localDir.x >= 0 ? "DashRight" : "DashLeft";

        int attackLayerIndex = animator.GetLayerIndex("Attack Layer");
        if (attackLayerIndex >= 0)
            animator.SetLayerWeight(attackLayerIndex, 0f);

        animator.SetBool(IsDashingHash, true);

        yield return null;
        animator.Play(dashStateName, 0, 0f);

        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null)
            playerCollider.enabled = false;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            playerRb.linearVelocity = new Vector3(
                dashDirection.x * dashSpeed,
                playerRb.linearVelocity.y,
                dashDirection.z * dashSpeed
            );
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        if (playerCollider != null)
            playerCollider.enabled = true;
        animator.SetBool(IsDashingHash, false);

        if (attackLayerIndex >= 0)
            animator.SetLayerWeight(attackLayerIndex, 1f);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    Vector2 ReadCurrentMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.1f)
                return stick;
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
        }

        if (input.sqrMagnitude > 1f) input.Normalize();
        return input;
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Barrier"))
        {
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 dirToBarrier = (collision.transform.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, dirToBarrier) > 0.5f)
                {
                    pushTimer = 0.15f;
                }
            }
        }
    }
}
