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


        public List<AircrtaftAgent> AircraftAgents { get; private set; }

        public List<GameObject> Checkpoints { get; private set; }


        private void Awake()
        {
            if(AircraftAgents == null) FindAircraftAgents();
        }

        

        private void Start()
        {
           if(Checkpoints == null) CreateCheckpoints();
        }


        


        /// <summary>
        /// Find all aircraft agents in the area
        /// </summary>
        private void FindAircraftAgents()
        {
            //Find all aircraft agents in this area

            AircraftAgents = transform.GetComponentsInChildren<AircrtaftAgent>().ToList();

            Debug.Assert(AircraftAgents.Count > 0, "No aircraftagents found");
        }





        /// <summary>
        /// Create the checkpoints
        /// </summary>
        private void CreateCheckpoints()
        {
            //Create chgeckpoints along the race path
            Debug.Assert(racePath != null, "Race path was not set");
            Checkpoints = new List<GameObject>();
            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);

            for (int i = 0; i < numCheckpoints; i++)
            {
                //Instantiate either a checkpoint or finish line checkpoint
                GameObject checkpoint;

                if (i == numCheckpoints - 1)
                {
                    checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                }
                else
                {
                    checkpoint = Instantiate<GameObject>(checkpointPrefab);
                }


                //Set the parent
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);


                //Add checkpoint to the list
                Checkpoints.Add(checkpoint);
            }
        }

        //<summary>
        /// Reset the position of an agent using its current NextCheckpointIndex,unless
        /// randomize is true, then will pick a new random checkpoint
        /// <param name="agent">The agent to reset</param>
        /// <param name="randomize">If true, will pick a new NextCheckpointIndex before reset</param>
        //</summary>

        void ResetAgentPosition(AircrtaftAgent agent,bool randomize = false) 
        {

            if (AircraftAgents == null) FindAircraftAgents();

            if (Checkpoints == null) CreateCheckpoints();

            if (randomize)
            {
                //Pick a new NextCheckpoint at random
                agent.NextCheckpointIndex = Random.Range(0,Checkpoints.Count);
            }

            //set start position to the previous position
            int previousCheckpointIndex = agent.NextCheckpointIndex - 1;

            if(previousCheckpointIndex == -1) previousCheckpointIndex = Checkpoints.Count - 1;

            float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

            //Convert the position on the race path to a position in 3d space
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);

            //Get the orientation at that position on the race path
            Quaternion orientation = racePath.EvaluateOrientation(startPosition);

            //Calculate horizontal offset so that agents are spread out 
            Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2.0f) * Random.Range(9f,10f);


            //Set the aircraft position and rotation
            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;
        }

    }
}

