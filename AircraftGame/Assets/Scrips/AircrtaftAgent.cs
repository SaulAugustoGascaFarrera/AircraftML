using Aircraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


namespace Aircraft
{
    public class AircrtaftAgent : Agent
    {
        [Header("Movement Components")]
        public float thrust = 100000f;
        public float pitchSpeed = 100.0f;
        public float yawSpeed = 100.0f;
        public float rollSpeed = 100.0f;
        public float boostMultiplier = 2.0f;


        [Header("Explosion stuff")]
        [Tooltip("The aircraft mesh that will disappear on explosion")]
        public GameObject meshObject;


        [Tooltip("The game object of the explosion particle effect")]
        public GameObject explosionEffect;


        [Header("Training")]
        [Tooltip("Number of steps to time out after in training")]
        public int stepTimeout = 300;

        public int NextCheckpointIndex { get; set; }

        //Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;

        //when the next step timeout will be during training
        float nextStepTimeout;

        //whether the aircraft is frozen (intentionally not flying)
        private bool frozen = false;



        //Copntrols
        private float pitchChange = 0.0f;
        private float smoothPitchChange = 0.0f;
        private float maxPitchAngle = 45.0f;
        private float yawChange = 0.0f;
        private float smoothYawChange = 0.0f;
        private float rollChange = 0.0f;
        private float smoothRollChange = 0.0f;
        private float maxRollAngle = 45.0f;
        private bool boost;


        /// <summary>
        /// Called whern the agent is first initialized
        /// </summary>
        public override void Initialize()
        {
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponentInParent<TrailRenderer>();

            // Override the Max step set in the inspector
            // Max 5000 steps if training, infinite steps if racing
            MaxStep = area.trainingMode ? 5000 : 0;
        }


        /// <summary>
        /// Called whern a new episode begins
        /// </summary>
        public override void OnEpisodeBegin()
        {
            //Reset the velocity,position and orientation
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this,randomize: area.trainingMode);


            //update the step timeout if training
            if(area.trainingMode)
            {
                nextStepTimeout = StepCount + stepTimeout;
            }
        }


        /// <summary>
        /// Read action inputs from vectorAction 
        /// </summary>
        /// <param name="vectorAction"></param>
        public override void OnActionReceived(float[] vectorAction)
        {

            if (frozen) return;

            //Read values fir pitch and yaw
            pitchChange = vectorAction[0];//up or none
            if(pitchChange == 2)  pitchChange = -1; //none

            yawChange = vectorAction[1]; //turn right or none
            if(yawChange == 2) yawChange = -1; //turn left


            //Read value form boost
            boost = vectorAction[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;


            ProcessMovement();

            if (area.trainingMode) 
            {
                //small negative reward every step
                AddReward(-1 / MaxStep);

                //Make sure we havent run out of time if training
                if(StepCount > nextStepTimeout) 
                {

                    AddReward(0.5f);
                    EndEpisode();
                
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();

                if(localCheckpointDir.magnitude < Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius",0)) 
                {
                    GotCheckpoint();
                }


            }

            
        }
        /// <summary>
        /// Collect observations used by agent to make decisions
        /// </summary>
        /// <param name="sensor">The vector sensor</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            //Observe aircraft velocity (1 vector3 = 3 values)
            sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity));

            //where is the next checkpoint? (1 vector3 = 3 values)
            sensor.AddObservation(VectorToNextCheckpoint());

            //Orientation of the next checkpoint (1 vector3 = 3 values)
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
            sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));


            //Total observations = 3 + 3 + 3 = 9

        }

        /// <summary>
        /// In this project, we only expect Heuristic to be used on AircraftPlayer
        /// </summary>
        /// <param name="actionsOut">Empty array</param>
        public override void Heuristic(float[] actionsOut)
        {
            Debug.LogError("Heuristic() was called on " + gameObject.name + " Make sure only the aircarftplayer is set to Behavior type: Heuristic only.");
        }

        /// <summary>
        /// Prevent the agent from moving and taking actions
        /// </summary>
        public void FreezeAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        /// <summary>
        /// Resume agent movement amnd actions
        /// </summary>
        public void ThawAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
            
        }

        /// <summary>
        /// Get a vector to next checkpoint the agent need to fly through
        /// </summary>
        /// <returns>A local space vector</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;

            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);

            return localCheckpointDir;
        }


        /// <summary>
        /// Called when the agent flies through the correct checkpoint
        /// </summary>
        private void GotCheckpoint()
        {
            //NETX CHECKPOINT REACHED, UPDATE
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if(area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = StepCount + stepTimeout;
            }
        }


        /// <summary>
        /// Calculate and apply movement
        /// </summary>
        private void ProcessMovement() 
        { 
        
            //Calculate boost
            float boostModifier = boost ? boostMultiplier : 1.0f;

            //apply forward thrust
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);


            //Get current rotation 
            Vector3 currentRotation = transform.rotation.eulerAngles;

            //Calculate the roll angle
            float rollAngle = currentRotation.z > 180.0f ? currentRotation.z - 360.0f : currentRotation.z;

            if(yawChange == 0f)
            {
                //Not turning; smoothly roll toward center 
                rollChange = -rollAngle / maxRollAngle;

            }
            else
            {
                //Turning; roll in opposite direction in turn
                rollChange = -yawChange;
            }


            //Calculate smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange,pitchChange,2.0f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2.0f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2.0f * Time.fixedDeltaTime);


            //Calculate new pitch,yaw and roll. Clamp pitch and roll
            float pitch = currentRotation.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 180.0f) pitch -= 360.0f;
            pitch = Mathf.Clamp(pitch,-maxPitchAngle, maxPitchAngle);


            float yaw = currentRotation.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            float roll = currentRotation.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;
            if (roll > 180.0f) roll -= 360.0f;
            roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

            //set new rotation 
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);

        }

        /// <summary>
        /// React to entering a trigger
        /// </summary>
        /// <param name="other">the collider entered</param>
        private void OnTriggerEnter(Collider other)
        {
            if(other.transform.CompareTag("checkpoint") && other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }


        /// <summary>
        /// React to collisions
        /// </summary>
        /// <param name="collision">Collision info</param>
        private void OnCollisionEnter(Collision collision)
        {
            if(!collision.transform.CompareTag("agent"))
            {
                //We hit something that wasnt another agent
                if(area.trainingMode)
                {
                    AddReward(-1f);
                    EndEpisode();
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }

        /// <summary>
        /// Resets the aircraft to the most recent complete checkpoint
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExplosionReset()
        {
            FreezeAgent();

            //Disable aircraft mesh object, enable explosion
            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            //Disable explosion, re enable aircraft mesh
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);

            //Reset position
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);
            
            ThawAgent();
        }
    }

}



