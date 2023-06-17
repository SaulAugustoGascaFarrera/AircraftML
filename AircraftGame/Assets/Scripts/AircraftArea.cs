using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {
        [Tooltip("The path the race will take")]
        public CinemachineSmoothPath racePath;

        [Tooltip("The prefa to use for checkpoints")]
        public GameObject checkpointPrefab;

        [Tooltip("The prefab to use for the start/end checkpoints")]
        public GameObject finishCheckpointPrefab;

        [Tooltip("If true, enable trainig mode")]
        public bool trainingMode;


        public List<AircraftAgent> AircraftAgents { get; private set; }

        public List<GameObject> Checkpoints { get; private set; }


        private void Awake()
        {
            //Find all aircraft agents in the area
            AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();

            Debug.Assert(AircraftAgents.Count > 0,"No aircraft agents not found");
        }


        private void Start()
        {
            //Create checkpoints along the race path
            Debug.Assert(racePath != null, "Race path was not set");

            Checkpoints = new List<GameObject>();

            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);

            for(int i=0;i < numCheckpoints;i++)
            {
                //Instantiate either a chckpoint or finish line checkpoint
                GameObject checkpoint;
                if(i == numCheckpoints - 1 )
                {
                    checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                }
                else
                {
                    checkpoint = Instantiate<GameObject>(checkpointPrefab);
                }

                //Set the parent ,position and rotation
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                //Add the checkpoint to the list
                Checkpoints.Add(checkpoint);
            }
        }
    }
}

