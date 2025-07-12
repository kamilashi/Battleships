using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Flocking : MonoBehaviour
{
    [Header("Setup")]
    public int NumOfBoids;
    public GameObject BoidPrefab;

    public float SeparationDistance;
    public float CohesionDistance;
    public float AlignmentDistance;

    public float SeparationWeight;
    public float CohesionWeight;
    public float AlignmentWeight;

    public Vector2 MaxRandomOffset;

    public Vector3 BoidLocalForward;
    public float BoidSpawnHeight;

    public Camera MainCamera;

    [Header("Debug view")]
    public List<Boid> Boids = new List<Boid>();


    // Start is called before the first frame update
    void Awake()
    {
/*
        for (int i = 0; i < NumOfBoids; i++)
        {
            Boid newBoid = Instantiate(BoidPrefab).GetComponent<Boid>();
            Vector3 offset = new Vector3(Random.Range(0, MaxRandomOffset.x), 0.0f, Random.Range(0, MaxRandomOffset.y));
            newBoid.transform.Translate(offset);
            newBoid.Flocking = this;
            Boids.Add(newBoid);
        }*/

        Debug.Assert(MainCamera != null, "Please, pass the correct camera into the script");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
