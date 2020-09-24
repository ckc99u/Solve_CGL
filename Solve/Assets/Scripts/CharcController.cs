using System.Collections.Generic;
using UnityEngine;

public class CharcController : MonoBehaviour
{

    private enum ControlMode
    {
        Demo,
        Direct
    }
    private enum Direction
    {
        Left,
        Right,
        Up,
        Down,
        None
    }

    [SerializeField] private float m_moveSpeed = 2.0f;
    [SerializeField] private float m_jumpForce = 4.0f;
    [SerializeField] private float m_slideForce = 4.0f;//todo use in the future

    [SerializeField] private Animator m_animator;
    [SerializeField] private Rigidbody m_rigibody;

    [SerializeField] private ControlMode m_controlMode = ControlMode.Demo;

    private bool m_grounded;
    private bool m_OntheGround;
    private bool m_slided = false;

    private Direction m_direction = Direction.None;

    private Vector3 m_currentDir = Vector3.zero;
    private float m_moveSize = 1f;//TODO: initialize when game start;

    private readonly float m_interpolation = 10;
    private readonly float m_walkScale = 0.5f;

    private float m_jumpTimeStamp = 0.0f;
    private float m_JumpInterval = 0.25f;

    private float m_squatTimeStamp = 0.0f;
    private float m_squatInterval = 2.0f;

    private Vector2 m_StartTouch;
    private Vector2 m_EndTouch;

    private List<Collider> m_collisions = new List<Collider>();

    private void Awake()
    {
        if (!m_animator) gameObject.GetComponent<Animator>();
        if (!m_rigibody) gameObject.GetComponent<Rigidbody>();
    }
    /// <summary>
    /// game collision detection logic. TODO: detect hitting object
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        foreach (ContactPoint i in contactPoints)
        {
            if (Vector3.Dot(i.normal, Vector3.up) > 0.05f)
            {
                if (!m_collisions.Contains(collision.collider))
                {
                    m_collisions.Add(collision.collider);
                }
                m_OntheGround = true;
            }
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        bool validSufaceNormal = false;
        foreach (ContactPoint i in contactPoints)
        {

            if (Vector3.Dot(i.normal, Vector3.up) > .5f)
            {
                validSufaceNormal = true;
                break;
            }
        }
        if (validSufaceNormal)
        {
            m_OntheGround = true;
            if (!m_collisions.Contains(collision.collider))
                m_collisions.Add(collision.collider);
        }
        else
        {
            if (m_collisions.Contains(collision.collider))
                m_collisions.Remove(collision.collider);
            if (m_collisions.Count == 0) m_OntheGround = false;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (m_collisions.Contains(collision.collider))
            m_collisions.Remove(collision.collider);
        if (m_collisions.Count == 0) m_OntheGround = false;
    }

    private void Update()
    {
        EndlessMove();
        switch (m_controlMode)
        {
            case ControlMode.Demo:
                DemoUpdate();
                break;
            case ControlMode.Direct:
                DirectUpate();
                break;
            default:
                Debug.LogError("Unspported state");
                break;
        }
    }
    /// <summary>
    /// alpha-version: keyboard
    /// </summary>
    private void DemoUpdate()
    {

        EndlessMove();

        if (Input.GetKeyDown(KeyCode.A))
        {
            m_direction = Direction.Left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            m_direction = Direction.Right;
        }
        else return;


    }
    private void EndlessMove()
    {
        if (m_grounded)
        {
            transform.position += Camera.main.transform.forward * m_moveSpeed * Time.deltaTime;
            m_animator.SetFloat("MoveSpeed", 1);
        }
    }
    private void RightMoves()
    {
        Vector3 pos = Camera.main.transform.right * m_moveSize;
        if (m_grounded)
            transform.position += pos;//endless move forward

    }
    private void LeftMoves()
    {

        Vector3 pos = Camera.main.transform.right * -m_moveSize;
        if (m_grounded)
            transform.position += pos;//endless move forward
    }
    /// <summary>
    /// beta-version: touch screen
    /// </summary>
    private void DirectUpate()
    {

        if (Input.touchCount == 1)
        {
            //using fingerID to get touch
            if (Input.GetTouch(0).phase == TouchPhase.Began)
                m_StartTouch = Input.GetTouch(0).position;
            if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                m_EndTouch = Input.GetTouch(0).position;
                DetectSwipe();
            }
        }
        else return;
    }
    private void FixedUpdate()
    {
        m_animator.SetBool("Grounded", m_grounded);

        m_grounded = m_OntheGround;
        switch (m_direction)
        {
            case Direction.Left:
                LeftMoves();
                m_direction = Direction.None;
                break;
            case Direction.Right:
                RightMoves();
                m_direction = Direction.None;
                break;
            default:
                break;
        }
        JumpAndLanding();
        SlideAndUp();
    }
    private void JumpAndLanding()
    {
        bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_JumpInterval;
        if (jumpCooldownOver && m_grounded && (Input.GetKey(KeyCode.W) || m_direction == Direction.Up))
        {
            m_jumpTimeStamp = Time.time;
            m_rigibody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
            m_direction = Direction.None;
        }

        if (!m_grounded)
            m_animator.SetTrigger("Land");
        else
            m_animator.SetTrigger("Jump");
    }
    private void SlideAndUp()
    {
        bool squatCooldownOver = (Time.time - m_squatTimeStamp) >= m_squatInterval;

        if (m_grounded && (Input.GetKey(KeyCode.S)|| m_direction == Direction.Down))
        {
            m_squatTimeStamp = Time.time;

            // m_rigibody.AddForce(Vector3.forward * m_squatForce, ForceMode.Impulse);
            if (!squatCooldownOver)
            {
                m_animator.SetBool("Down", true);
                m_direction = Direction.None;
            }
        }
        else if (squatCooldownOver)
            m_animator.SetBool("Down", false);




    }

    /// <summary>
    /// Dot product to comput swiping direction and XY coordinate system. 
    /// </summary>
    private void DetectSwipe()
    {
        Vector2 Dir = (m_EndTouch - m_StartTouch).normalized;

        if (Vector2.Dot(Dir, Vector2.up) <= 1 && Vector2.Dot(Dir, Vector2.up) >= 0.5f)//up
        {
            Debug.Log("UP");
            m_direction = Direction.Up;
        }
        else if (Vector2.Dot(Dir, Vector2.down) <= 1 && Vector2.Dot(Dir, Vector2.down) >= 0.5)//down
        {
            Debug.Log("Down");
            m_direction = Direction.Down;
        }
        else if (Vector2.Dot(Dir, Vector2.left) <= 1 && Vector2.Dot(Dir, Vector2.left) >= 0.5)//left
        {
            Debug.Log("Left");
            m_direction = Direction.Left;
        }
        else if (Vector2.Dot(Dir, Vector2.right) <= 1 && Vector2.Dot(Dir, Vector2.right) >= 0.5)//right
        {
            Debug.Log("Right");
            m_direction = Direction.Right;
        }
        else return;

    }
}
