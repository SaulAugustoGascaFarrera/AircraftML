using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class AircraftPlayer : AircraftAgent
    {
        [Header("Input Bindings")]
        public InputAction pitchInput;
        public InputAction yawInput;
        public InputAction boostInput;
        public InputAction pauseInput;

        public override void Initialize()
        {
            base.Initialize();

            pitchInput.Enable();
            yawInput.Enable(); 
            boostInput.Enable();
            pauseInput.Enable();
        }

        /// <summary>
        /// Read player input and converts it to a vector action array
        /// </summary>
        /// <param name="actionsOut">An array of floats for OnActionReceived to use</param>
        public override void Heuristic(float[] actionsOut)
        {
            //Pitch 1 == up, 0 == none, -1 == down
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());

            //Yaw 1 == right,0 == none,-1 == left
            float yawValue = Mathf.Round((yawInput.ReadValue<float>()));

            //Boost 1 == boost,0 == no boost
            float boostValue = Mathf.Round(boostInput.ReadValue<float>());

            //convert -1 (down) to discrete value 2
            if(pitchValue == -1) pitchValue = 2f;

            //convert -1 (left) to discrete value 2
            if (yawValue == -1) yawValue = 2f;

            actionsOut[0] = pitchValue;
            actionsOut[1] = yawValue;
            actionsOut[2] = boostValue;
        }

        private void OnDestroy() 
        { 
            pitchInput.Disable();
            yawInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
        }
    }
}

