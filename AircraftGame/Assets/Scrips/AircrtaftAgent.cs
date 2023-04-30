using Aircraft;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.MLAgents;
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

        public int NextCheckpointIndex { get; set; }

        //Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;

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
        }


        /// <summary>
        /// Read action inputs from vectorAction 
        /// </summary>
        /// <param name="vectorAction"></param>
        public override void OnActionReceived(float[] vectorAction)
        {
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
    }

}



