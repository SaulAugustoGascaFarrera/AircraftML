using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;



namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {
        [Tooltip("The path the race will take")]
        public CinemachineSmoothPath racePath;

        [Tooltip("The prefa to use for checkpoints")]
        public GameObject checkpointPrefabs;

        [Tooltip("The prefab to use for the start/end checkpoints")]
        public GameObject finishCheckpointPrefab;

        [Tooltip("If true, enable trainig mode")]
        public bool trainingMode;


        public List<AircrtaftAgent> AircrtaftAgents { get; private set; }

        public List<GameObject> Checkpoints { get; private set; }
    }
}

