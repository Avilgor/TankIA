using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
        public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.
        public bool player;
        public TankShooting shootManager;

        private string m_MovementAxisName;          // The name of the input axis for moving forward and back.            
        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_MovementInputValue;         // The current value of the movement input.
        private float m_TurnInputValue;             // The current value of the turn input.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particles systems used by the Tanks
        
        private Vector3 destinationPoint;
        private bool gotPoint;
        private NavMeshPath path;
        private int pathIndex = 0;
        private Vector3 direction;
        private float turnAngle;
        List<GameObject> targets;
        private Transform currentTarget;
        private Transform seekTarget;
        private bool move;
        private bool forward;
        private Vector3 rayOffset;
        private float xColPoint;

        private void Awake()
        {
            path = new NavMeshPath();
            m_Rigidbody = GetComponent<Rigidbody>();
            targets = new List<GameObject>();
            xColPoint = 0.0f;          
        }


        private void OnEnable()
        {
            // When the tank is turned on, make sure it's not kinematic.
            m_Rigidbody.isKinematic = false;
            gotPoint = false;
            StopAllCoroutines();
            // Also reset the input values.
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            turnAngle = 0f;
            seekTarget = null;
            targets.Clear();
            move = true;
            forward = true;

            // We grab all the Particle systems child of that Tank to be able to Stop/Play them on Deactivate/Activate
            // It is needed because we move the Tank when spawning it, and if the Particle System is playing while we do that
            // it "think" it move from (0,0,0) to the spawn point, creating a huge trail of smoke
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }


        private void OnDisable()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;

            // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }


        private void Start()
        {
            // The axes names are based on player number.
            m_MovementAxisName = "Vertical" + m_PlayerNumber;

            // Store the original pitch of the audio source.
            m_OriginalPitch = m_MovementAudio.pitch;

            rayOffset.x = 0;
            rayOffset.z = 0;
            rayOffset.y = 0.5f;
        }


        private void Update()
        {
            // Store the value of both input axes.
            m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
            if (Input.GetKey(KeyCode.D)) m_TurnInputValue = 1;
            else if (Input.GetKey(KeyCode.A)) m_TurnInputValue = -1;
            else m_TurnInputValue = 0;

            EngineAudio();
        }


        private void EngineAudio()
        {
            // If there is no input (the tank is stationary)...
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                // ... and if the audio source is currently playing the driving clip...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... change the clip to idling and play it.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... change the clip to driving and play.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }


        private void FixedUpdate()
        {
            if (Time.timeScale > 0)
            {
                Move();
                Turn();
            }
        }


        private void Move()
        {
            if (player)
            {
                // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
                Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

                // Apply this movement to the rigidbody's position.
                m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
            }
            else
            {
                if (!gotPoint)
                {
                    StopAllCoroutines();
                    if (currentTarget != null)
                    {
                        Debug.Log("Shooting");
                        destinationPoint = RandomPointNavMesh(currentTarget.position,10.0f);
                        NavMesh.CalculatePath(transform.position, destinationPoint, NavMesh.AllAreas, path);
                        pathIndex = 0;
                        if (path.status == NavMeshPathStatus.PathInvalid) gotPoint = false;
                        //else StartCoroutine(RecalculatePath());
                        
                    }
                    else if (seekTarget != null && Vector3.Distance(transform.position,seekTarget.position) < 30)
                    {
                        Debug.Log("Seeking");
                        destinationPoint = seekTarget.position;
                        NavMesh.CalculatePath(transform.position, destinationPoint, NavMesh.AllAreas, path);
                        pathIndex = 0;
                        if (path.status == NavMeshPathStatus.PathInvalid) gotPoint = false;
                        //else StartCoroutine(RecalculatePath());                      
                    }
                    else
                    {
                        Debug.Log("Wandering");
                        destinationPoint = RandomPointNavMesh(transform.position);
                        NavMesh.CalculatePath(transform.position, destinationPoint, NavMesh.AllAreas, path);
                        pathIndex = 0;
                        if (path.status == NavMeshPathStatus.PathInvalid) gotPoint = false;
                        else StartCoroutine(RecalculatePath());                           
                    }
                }
                else if(move)
                {
                    if (forward)
                    {
                        if (Physics.Raycast(transform.position + rayOffset, transform.forward, 2.0f, 1 << LayerMask.NameToLayer("Ground"))) StartCoroutine(StepBack());//forward = false;
                    }
                    //else forward = true;
                    Debug.DrawRay(transform.position + rayOffset, transform.forward * 2.0f, Color.white);
                    if (forward)
                    {
                        Vector3 move = transform.forward * m_Speed * Time.deltaTime;
                        m_Rigidbody.MovePosition(m_Rigidbody.position + move);
                    }
                    else
                    {
                        Vector3 move = -transform.forward * m_Speed * Time.deltaTime;
                        m_Rigidbody.MovePosition(m_Rigidbody.position + move);
                    }

                    if (Vector3.Distance(path.corners[pathIndex], transform.position) <= 2f)
                    {
                        if (pathIndex < path.corners.Length - 1) pathIndex++;
                        else gotPoint = false;
                    }
                }

                if(path.corners.Length > 0)
                    for (int i = 0; i < path.corners.Length - 1; i++) Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
            }
        }

        private void Turn()
        {
            if (player)
            {
                // Determine the number of degrees to be turned based on the input, speed and time between frames.
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

                // Make this into a rotation in the y axis.
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

                // Apply this rotation to the rigidbody's rotation.
                m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
            }
            else
            {
                /*if (!move)
                {
                    Debug.Log("Pos"+transform.position.z);
                    if (xColPoint < transform.position.z) //Turn Left
                    {
                        Quaternion turnRotation = Quaternion.Euler(0f, -m_TurnSpeed * Time.deltaTime, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }
                    else //Turn Right
                    {
                        Quaternion turnRotation = Quaternion.Euler(0f, m_TurnSpeed * Time.deltaTime, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }
                }
                else
                {*/
                turnAngle = GetNewAngle();
                //if (forward) //Turn forward
                //{
                    if (turnAngle > 2.0f) //Turn Left
                    {
                        Quaternion turnRotation = Quaternion.Euler(0f, -m_TurnSpeed * Time.deltaTime, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }
                    else if (turnAngle < -2.0f) //Turn Right
                    {
                        Quaternion turnRotation = Quaternion.Euler(0f, m_TurnSpeed * Time.deltaTime, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }
                /*}
                else //Turn backwards
                {
                    if (turnAngle > 2.0f) //Turn Right
                    {
                        Quaternion turnRotation = Quaternion.Euler(0f, m_TurnSpeed * Time.deltaTime, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }
                    else if (turnAngle < -2.0f) //Turn Left
                    {
                        Quaternion turnRotation = Quaternion.Euler(0f, -m_TurnSpeed * Time.deltaTime, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }
                }*/
                //}
                //else turnAngle = GetNewAngle();
            }
        }

        private float GetNewAngle()
        {
            if (path.corners.Length > 0)
            {
                direction = (path.corners[pathIndex] - transform.position);
                float angle = Vector3.SignedAngle(direction, transform.forward, Vector3.up);
                if (angle > 1.0f || angle < -1.0f) return angle;
                else return 0;
            }
            else return 0;
        }

        private Vector3 RandomPointNavMesh(Vector3 center, float range = 100.0f)
        {
            Vector3 point = Vector3.zero;
            gotPoint = false;
            NavMeshHit hit;
            do
            {
                Vector3 randomPoint = center + Random.insideUnitSphere * range;
                if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
                {
                    point = hit.position;
                    gotPoint = true;
                }
            } while (!gotPoint);

            return point;
        }

        private void SelectTarget()
        {
            if (targets.Count > 0)
            {
                if (targets.Count > 1)
                {
                    float min = 0;
                    float d;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        d = Vector3.Distance(transform.position, targets[0].transform.position);
                        if (min == 0)
                        {
                            min = d;
                            currentTarget = targets[i].transform;
                            shootManager.target = targets[i];
                        }
                        else if (d < min)
                        {
                            min = d;
                            currentTarget = targets[i].transform;
                            shootManager.target = targets[i];
                        }
                    }
                }
                else
                {
                    currentTarget = targets[0].transform;
                    shootManager.target = targets[0];
                }
            }
            else 
            {
                shootManager.target = null;
                currentTarget = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!player && other.gameObject != gameObject)
            {
                if (other.CompareTag("Player"))
                {
                    targets.Add(other.gameObject);
                    seekTarget = null;
                    SelectTarget();
                    gotPoint = false;
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!player && other.gameObject != gameObject)
            {
                if (other.CompareTag("Player"))
                {
                    SelectTarget();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!player && other.gameObject != gameObject)
            {
                if (other.CompareTag("Player"))
                {
                    if (targets.Count > 1)
                    {
                        for (int i = 0; i < targets.Count; i++)
                        {
                            if (targets[i] == other.gameObject)
                            {
                                targets.Remove(other.gameObject);
                                break;
                            }
                        }
                    }
                    else
                    {
                        seekTarget = other.gameObject.transform;
                        targets.Remove(other.gameObject);
                    }
                    SelectTarget();
                }
            }
        }

        private IEnumerator StepBack()
        {
            forward = false;
            yield return new WaitForSeconds(0.5f);
            forward = true;
            gotPoint = false;
        }

        /*private IEnumerator LockMove()
        {
            Debug.Log("Moving locked");
            move = false;
            yield return new WaitForSeconds(3.0f);
            move = true;
        }*/

        private IEnumerator RecalculatePath()
        {
            yield return new WaitForSeconds(5.0f);
            if (gotPoint)
            {
                NavMesh.CalculatePath(transform.position, destinationPoint, NavMesh.AllAreas, path);
                pathIndex = 0;
            }
            StartCoroutine(RecalculatePath());
        }
    }
}