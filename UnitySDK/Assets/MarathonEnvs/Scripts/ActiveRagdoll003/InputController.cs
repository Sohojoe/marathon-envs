using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class InputController : MonoBehaviour
{
    [Header("Options")]
    public float MaxVelocity;

    [Header("User or Mock input states")]
    public Vector2 MovementVector; // User-input desired horizontal center of mass velocity.
    public Vector2 CameraRotation; // User-input desired rotation for camera.
    public bool Jump; // User wants to jump
    public bool Backflip; // User wants to backflip

    [Header("Read only (or debug)")]
    public Vector2 DesiredHorizontalVelocity; // MovementVector * Max Velovity
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    public bool UseHumanInput;

    float _delayUntilNextAction;

    // Start is called before the first frame update
    void Awake()
    {
        UseHumanInput = !Academy.Instance.IsCommunicatorOn;
    }

    // Update is called once per frame
    void Update()
    {
        DoUpdate();
    }
    void DoUpdate()
    {
        if (UseHumanInput)
            GetHumanInput();
        else
            GetMockInput();
        if (MovementVector.magnitude > 0f)
            HorizontalDirection = new Vector3(MovementVector.normalized.x, 0f, MovementVector.normalized.y);
        DesiredHorizontalVelocity = MovementVector * MaxVelocity;
    }
    public void OnReset()
    {
        _delayUntilNextAction = -1f;
        DoUpdate();
    }
    void GetHumanInput()
    {
        MovementVector = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        CameraRotation = Vector2.zero;
        Jump = Input.GetKey(KeyCode.Space); //Input.GetButtonDown("Fire1");
        Backflip = Input.GetKey(KeyCode.B);
    }
    void GetMockInput()
    {
        _delayUntilNextAction -= Time.deltaTime;
        if (_delayUntilNextAction > 0)
            return;
        if (ChooseBackflip())
        {
            Backflip = true;
            _delayUntilNextAction = 2f;
            return;
        }
        Backflip = false;
        Jump = false;
        float direction = UnityEngine.Random.Range(0f, 360f);
        float power = UnityEngine.Random.value;
        MovementVector = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
        MovementVector *= power;
        Jump = ChooseJump();
        _delayUntilNextAction = 2f;
    }
    bool ChooseBackflip()
    {
        var rnd = UnityEngine.Random.Range(0, 10);
        return rnd == 0;
    }
    bool ChooseJump()
    {
        var rnd = UnityEngine.Random.Range(0, 5);
        return rnd == 0;
    }

}
