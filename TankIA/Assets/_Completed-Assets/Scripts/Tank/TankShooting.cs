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
        public int maxShells = 5;

        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.
        private float cd = 2.0f;
        public GameObject target;
        private float angle;
        private int currentShells;


        private void OnEnable()
        {
            m_CurrentLaunchForce = 30.0f;
            m_Fired = false;
            target = null;
            angle = 0;
            turret.transform.rotation = transform.rotation;
            currentShells = maxShells;
        }


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
                        //if (Physics.Raycast(turret.transform.position, target.transform.position,20.0f)) //Enemy in sight
                        //{
                           // Debug.DrawLine(turret.transform.position, target.transform.position, Color.green);
                            Vector3 direction = (target.transform.position - turret.transform.position);
                            angle = Vector3.SignedAngle(direction, turret.transform.forward, Vector3.up);
                        //}
                        /*else //Not in sight
                        {
                            Vector3 direction = (gameObject.transform.position - turret.transform.position);
                            angle = Vector3.SignedAngle(direction, turret.transform.forward, Vector3.up);
                        }*/

                        if (angle > 5.0f)
                        {
                            turret.transform.Rotate(0, -turretTurnSpeed * (Time.deltaTime * 50), 0, Space.Self);
                        }
                        else if (angle < -5.0f)
                        {
                            turret.transform.Rotate(0, turretTurnSpeed * (Time.deltaTime * 50), 0, Space.Self);
                        }

                        if (angle < 10 && angle > -10 && !m_Fired && currentShells > 0)
                        {
                            RaycastHit hit;
                            Physics.Raycast(turret.transform.position, target.transform.position,out hit,20.0f);
                            if (hit.collider) //Enemy in sight
                            {
                                currentShells -= 1;
                                if (currentShells <= 0)
                                {
                                    gameObject.SendMessage("GoForAmmo");
                                }
                                Fire();
                            }                                                                                                              
                        }
                    }
                }              
            }
        }

        public void OnGotAmmo()
        {
            currentShells = maxShells;
        }

        private void Fire()
        {
            m_Fired = true;
            float distance = Vector3.Distance(m_FireTransform.position, target.transform.position);

            float tan1;//,tan2;
            float temp1;//,temp2;
            float x, y;
            x = distance;//target.transform.position.x - transform.position.x;
            y = target.transform.position.y - (transform.position.y + 0.05f);
            temp1 /*= temp2*/ = m_CurrentLaunchForce * m_CurrentLaunchForce;
            float root = Mathf.Sqrt(Mathf.Pow(m_CurrentLaunchForce,4) - (9.81f * (9.81f*(x*x) + (2 * m_CurrentLaunchForce * m_CurrentLaunchForce * y))));
            temp1 += root;
            //temp2 -= root;
            tan1 = temp1 / (9.81f*x);
            //tan2 = temp2 / (9.81f * x);
            //Debug.Log("Tan 1 :" + tan1);
            //Debug.Log("Tan 2 :" + tan2);
            //float angle1, angle2;
            //angle1 = Mathf.Atan(tan1);
            //angle2 = Mathf.Atan(tan2);
            //Debug.Log("ShootAngle :" + angle1);
            //Debug.Log("Angle 2 :" + angle2);
            //Debug.Log("Distance: " + distance);
            float shotAngle = Mathf.Atan(tan1);
            Debug.Log("ShootAngle :" + shotAngle);
            /*float shotAngle;
            if (distance / 2 > m_CurrentLaunchForce) //objective out of range
            {
                shotAngle = 45.0f;
            }
            else
            {
                shotAngle = Mathf.Rad2Deg * Mathf.Acos((distance / 2) / m_CurrentLaunchForce); //Calculate shoot angle
                if (shotAngle > 45) shotAngle = 90.0f - shotAngle; //Select shortest way

                //Calculate height angle
                if (m_FireTransform.position.y > target.transform.position.y)
                {
                    float a = m_FireTransform.position.y - target.transform.position.y;
                    float fixAngle = Mathf.Rad2Deg * Mathf.Asin(a / distance); //Calculate fix angle
                    shotAngle -= fixAngle;
                    //Debug.Log("Fix angle: " + fixAngle);
                }
                else
                {
                    float a = target.transform.position.y - m_FireTransform.position.y;
                    float fixAngle = Mathf.Rad2Deg * Mathf.Asin(a / distance); //Calculate fix angle
                    shotAngle += fixAngle;
                    //Debug.Log("Fix angle: " + fixAngle);
                }
            }
            Debug.Log("Shot angle: "+ shotAngle);*/
            Debug.DrawLine(m_FireTransform.position,target.transform.position,Color.magenta,3.0f);
            
            GameObject shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);
            Vector3 v = shellInstance.transform.rotation.eulerAngles;
            shellInstance.transform.rotation = Quaternion.Euler(-shotAngle, v.y, v.z);

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.GetComponent<Rigidbody>().velocity = m_CurrentLaunchForce * shellInstance.transform.forward.normalized;

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