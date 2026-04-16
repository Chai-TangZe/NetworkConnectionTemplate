using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    bool isJump = false;
    public bool UseOperation = true;
    [Header("Joystick Controller")]
    public VJHandler jsMovement;

    [Header("Player Camera")]
    public Transform Camera;

    [Header("Move Speed")]
    public float MoveSpeed;

    /// <summary>
    /// Input movement values.
    /// </summary>
    private float horizontal;
    private float vertical;
    private float ctrlValue = 0.5f;
    private float handHeight = 0;
    [Header("Gravity")]
    public float Gravity = 10f;
    float GravityMax = 1000;
    float gravity;
    [Header("Jump Speed")]
    public float JumpSpeed = 15f;
    [Header("Player")]
    public CharacterController PlayerController;
    private Transform head;
    Vector3 Player_Move;

    public Transform Head { get {
            if (head==null)
            {
                head = PlayerController.transform.GetChild(0).transform;
            }
            return head;
        } }
    private void Start()
    {
        handHeight = Head.position.y - PlayerController.transform.position.y;
    }
    public void OnJump()
    {
        if (PlayerController.isGrounded)
            isJump = true;
    }
    public void SetPosition(Vector3 position)
    {
        point = position;
        PlayerController.enabled = false;
        PlayerController.transform.position = position;
        StartCoroutine(Delay());
        // PlayerController.Move teleport has blocking issues
    }
    Vector3 point = Vector3.zero;
    bool isMove = false;
    IEnumerator Delay()
    {
        isMove = true;
        yield return new WaitForSeconds(1);
        PlayerController.enabled = true;
        isMove = false;
    }
    public Vector3 GetPosition()
    {
        return PlayerController.transform.position;
    }
    // Keep state across frames; do not compute in a local scope each call.
    void Update()
    {
        if (!UseOperation|| Camera==null)
        {
            return;
        }
        if (isMove)
        {
            PlayerController.transform.position = point;
        }
        gravity = Gravity;
        PlayerController.transform.eulerAngles = new Vector3(PlayerController.transform.eulerAngles.x , Camera.eulerAngles.y , PlayerController.transform.eulerAngles.z);
        // Check if CharacterController is grounded
        if (PlayerController.isGrounded)
        {
            gravity = GravityMax;
            moveQuantity(out horizontal , out vertical);
            Player_Move = ( PlayerController.transform.forward * vertical + PlayerController.transform.right * horizontal ) * MoveSpeed;

            // Check jump input
            if (isJump)
            {
                isJump = false;
                gravity = Gravity;
                // Add upward velocity on jump
                Player_Move.y = Player_Move.y + JumpSpeed;
            }
        }
        else
        {
            moveQuantity(out horizontal , out vertical);
            Player_Move += ( PlayerController.transform.forward * vertical + PlayerController.transform.right * horizontal ) * Time.deltaTime * MoveSpeed * 2.5f;
        }

        // Apply gravity
        Player_Move.y = Player_Move.y - gravity * Time.deltaTime;

        PlayerController.Move(Player_Move * Time.deltaTime);
    }
    bool isCtrl = false;
    /// <summary>
    /// Process input values.
    /// </summary>
    /// <param name="horizontal"></param>
    /// <param name="vertical"></param>
    void moveQuantity(out float horizontal , out float vertical)
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
            OnJump();
        if (Input.GetKeyDown(KeyCode.LeftControl))
            isCtrl = !isCtrl;
        Head.position =
                new Vector3(Head.position.x ,
                isCtrl ? PlayerController.transform.position.y + ctrlValue : PlayerController.transform.position.y + handHeight
                , Head.position.z);
        if (jsMovement)
        {
            if (jsMovement.InputDirection.magnitude != 0)
            {
                horizontal = jsMovement.InputDirection.x;
                vertical = jsMovement.InputDirection.y;
            }
        }
    }
}
