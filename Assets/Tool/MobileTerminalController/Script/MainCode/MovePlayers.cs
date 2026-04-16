using UnityEngine;
using System.Collections;

public class MovePlayers : MonoBehaviour
{
    private CharacterController character;
    public float moveSpeed = 5f;
    public VJHandler jsMovement;
    public Transform Camera;
    //private Vector3 direction;
    private void Start()
    {
        character = this.GetComponent<CharacterController> ();
    }
    void Update()
    {
        if (jsMovement.InputDirection.magnitude != 0)
            character.Move (transform.right * moveSpeed * jsMovement.InputDirection.x + transform.forward * moveSpeed * jsMovement.InputDirection.y);
        //if (jsMovement.InputDirection.magnitude != 0)
        //{
            transform.eulerAngles = new Vector3 (transform.eulerAngles.x, Camera.eulerAngles.y, transform.eulerAngles.z);
        //    GetComponent<Rigidbody> ().velocity = transform.right * moveSpeed * jsMovement.InputDirection.x+ transform.forward * moveSpeed * jsMovement.InputDirection.y;
        //}
    }
}