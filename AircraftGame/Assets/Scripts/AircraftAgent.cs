using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {
        [Header("Movemnt Parameters")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;
        public int NextCheckpointIndex { get; set; }

        //Component to keep track of
        AircraftArea area;
        new Rigidbody rigidbody;
        TrailRenderer trail;

        //Control
        float pitchChange = 0f;
        float smoothPitchChange = 0f;
        float maxPitchAngle = 45f;
        float yawChange = 0f;
        float smoothYawChange = 0f;
        float rollChange = 0f;
        float smoothRollChange = 0f;
        float maxRollAngle = 45f;
        bool boost;

        /// <summary>
        /// Called when the agent is first initialize
        /// </summary>
        public override void Initialize()
        {
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
        }

        /// <summary>
        /// Read actions inputs from vectorAction
        /// </summary>
        /// <param name="vectorAction">The chosen actions</param>
        public override void OnActionReceived(float[] vectorAction)
        {
            //Read Values for  pitch and yaw
            pitchChange = vectorAction[0]; //up or none
            if (pitchChange == 2) pitchChange = -1; //down 

            yawChange = vectorAction[1]; //turn right or none
            if(yawChange == 2) yawChange = -1; //turn left

            //Read value for boost and enable / disable trial render
            boost = vectorAction[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            ProcessMovement();
        }

        /// <summary>
        /// Calculate and apply movement
        /// </summary>
        private void ProcessMovement()
        {
            //Calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;

            //Apply forward thrust
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);

            //Get the current rotation
            Vector3 curRot = transform.rotation.eulerAngles;

            //Calculate the roll angle between(-180 and 180)
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;

            if(yawChange == 0)
            {
                //Nor turning: smoothly roll toward center
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                //Turning: roll in opposite direction of turn 
                rollChange = -yawChange;
            }

            // Calculate  smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange,2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);


            //Calculate new Pitch,Yaw and Roll. Clamp pitch amd roll.
            float pitch = curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch,-maxPitchAngle, maxPitchAngle);

            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
            

            float roll = curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;
            if (roll > 180) roll -= 360f;
            roll = Mathf.Clamp(roll,-maxRollAngle, maxRollAngle);

            //set teh new rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }
    }
}

