using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAgents
{
    public class SpawnableEnv: MonoBehaviour
    {
        [Space()]
        [Tooltip("How much padding bettween spawned environments as a multiple of the envionment size (i.e. 1 = a gap of one envionment.")]
        public float paddingBetweenEnvs;
        [Space()]
        public Bounds bounds;
        [Space()]
        [Tooltip("Creates a unique scene and physics scene for this envionment")]
        public bool CreateUniquePhysicsScene;

        Scene _spawnedScene;
        PhysicsScene _spawnedPhysicsScene;

        [Header("Artanim Agents")]

        [SerializeField]
        bool _useTerrain;


        //if you do not use terrain, then you must give a gameObject which is the parent of all the bounding boxes that form the floor
        //[SerializeField]
        GameObject _groundRoot = null;


        [SerializeField]
        string _groundRootName;


        //to fit with how pathfinding works, we only activate these agents once everything is set up
        [SerializeField]
        GameObject[] inactiveAgents;

        bool _agentsEnabled = false;

        [SerializeField]
        bool _updatebounds = false;


        private void Update()
        {
            
        }

        private void FixedUpdate()
        {
            if (! _agentsEnabled)
                EnableInactiveAgents();

            if (CreateUniquePhysicsScene)
                _spawnedPhysicsScene.Simulate(Time.fixedDeltaTime);
        }

        public void UpdateBounds()
        {

            if (_updatebounds) { 
                bounds.size = Vector3.zero; // reset
                Collider[] colliders = GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    bounds.Encapsulate(col.bounds);
                }

                if (_useTerrain)
                {
                    TerrainCollider[] terrainColliders = GetComponentsInChildren<TerrainCollider>();
                    foreach (TerrainCollider col in terrainColliders)
                    {
                        var b = new Bounds();
                        b.center = col.transform.position + (col.terrainData.size / 2);
                        b.size = col.terrainData.size;
                        bounds.Encapsulate(b);
                    }

                }
                else
                {//we assume we are using normal bounding boxes
                    if (_groundRoot == null) {
                        _groundRoot = GameObject.Find(_groundRootName);
                
                    }

                    if (_groundRoot != null)
                    {
                        BoxCollider[] boxColliders = _groundRoot.transform.GetComponentsInChildren<BoxCollider>();
                        foreach (BoxCollider col in boxColliders)
                        {
                            var b = new Bounds();
                            b.center = col.transform.position + (col.size / 2);
                            b.size = col.size;
                            bounds.Encapsulate(b);
                        }
                    }

                }

            }




        }



        //JL TODO fix this DIRTY hack

        void EnableInactiveAgents()
        {

            Debug.Log("Looking for my agents" );

            //a fix to go around the problem of having agents that do not know their path
            //artanim_ai.Agent[] agents = GameObject.FindObjectsOfType<artanim_ai.Agent>();


            //does not work since inactive:
            //artanim_ai.Agent[] agents =  gameObject.GetComponentsInChildren<artanim_ai.Agent>();
            //artanim_ai.Agent[] agents = transform.GetComponentsInChildren<artanim_ai.Agent>();


            //foreach (artanim_ai.Agent a in agents)
            //{
            //    a.enabled = false;
            //}

            //foreach (artanim_ai.Agent a in agents)
            //{

            //    Debug.Log("I am activating agent " + a.name);
            //    a.enabled = true;
            //}

            foreach (GameObject o in inactiveAgents)
                o.SetActive( true);


            _agentsEnabled = true;
        }



        public bool IsPointWithinBoundsInWorldSpace(Vector3 point)
        {
            var boundsInWorldSpace = new Bounds(
                bounds.center + transform.position,
                bounds.size
            );
            bool isInBounds = boundsInWorldSpace.Contains(point);
            return isInBounds;
        }

        public void SetSceneAndPhysicsScene(Scene spawnedScene, PhysicsScene spawnedPhysicsScene)
        {
            _spawnedScene = spawnedScene;
            _spawnedPhysicsScene = spawnedPhysicsScene;
        }
        public PhysicsScene GetPhysicsScene()
        {
            return _spawnedPhysicsScene != null ? _spawnedPhysicsScene : Physics.defaultPhysicsScene;
        }

        /*
        public static void TriggerPhysicsStep()
        {
            var uniquePhysicsEnvs = FindObjectsOfType<SpawnableEnv>()
                .Where(x=>x.CreateUniquePhysicsScene)
                .ToList();
            foreach (var env in uniquePhysicsEnvs)
            {
                env._spawnedPhysicsScene.Simulate(Time.fixedDeltaTime);
            }
        }*/


    }
}