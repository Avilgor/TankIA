using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public GameObject m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public bool player;
        public GameObject turret;
        public float turretTurnSpeed = 360.0f;

        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.
        private float cd = 2.0f;
        public GameObject target;
        private float angle;


        private void OnEnable()
        {
            m_CurrentLaunchForce = 30.0f;
            m_Fired = false;
            target = null;
            angle = 0;
            turret.transform.rotation = transform.rotation;
        }


        /*private void Start ()
        {
           
        }*/


        private void Update()
        {
            if (Time.timeScale > 0)
            {
                if (player)
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        if (m_Fired == false) Fire();
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        turret.transform.Rotate(0, -turretTurnSpeed * (Time.deltaTime * 50), 0, Space.Self);
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        turret.transform.Rotate(0, turretTurnSpeed * (Time.deltaTime * 50), 0, Space.Self);
                    }
                }
                else
                {
                    if (target != null)
                    {
                        if (Physics.Raycast(turret.transform.position, target.transform.position)) //Enemy in sight
                        {
                            Debug.DrawLine(turret.transform.position, target.transform.position, Color.green);
                            Vector3 direction = (target.transform.position - turret.transform.position);
                            angle = Vector3.SignedAngle(direction, turret.transform.forward, Vector3.up);
                        }
                        else //Not in sight
                        {
                            Vector3 direction = (gameObject.transform.position - turret.transform.position);
                            angle = Vector3.SignedAngle(direction, turret.transform.forward, Vector3.up);
                        }

                        if (angle > 5.0f)
                        {
                            turret.transform.Rotate(0, -turretTurnSpeed * (Time.deltaTime * 50), 0, Space.Self);
                        }
                        else if (angle < -5.0f)
                        {
                            turret.transform.Rotate(0, turretTurnSpeed * (Time.deltaTime * 50), 0, Space.Self);
                        }

                        if (angle < 10 && angle > -10 && !m_Fired) Fire();
                    }
                }              
            }
        }


        private void Fire()
        {
            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

            // Create an instance of the shell and store a reference to it's rigidbody.
            GameObject shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.GetComponent<Rigidbody>().velocity = m_CurrentLaunchForce * m_FireTransform.forward.normalized;

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
            
            StartCoroutine(ChargeShell());
        }        

        private IEnumerator ChargeShell()
        {
            yield return new WaitForSeconds(cd);
            m_Fired = false;
        }
    }
}