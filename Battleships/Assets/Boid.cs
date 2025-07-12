using Library;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("Setup")]
    public float MaxMoveSpeed = 3.0f;
    public float Acceleration = 3.0f; 
    public float RotationSpeed;
    public Vector3 DefaultDirection;

    [Header("Debug view")]
    public Flocking Flocking;

    public Vector3 TargetDirection = Vector3.zero;
    public Vector3 CurrentDirection = Vector3.zero;

    public float TargetSpeed = 0.0f;
    public float CurrentSpeed = 0.0f;

    void Start()
    {
        TargetSpeed = MaxMoveSpeed;
        DefaultDirection.Normalize();
    }

    // Update is called once per frame
    void Update()
    {
        List<Boid> cohesionNeighbors = new List<Boid>();
        List<Boid> alignmentNeighbors = new List<Boid>();
        List<Boid> separationNeighbors = new List<Boid>();

        for (int i = 0; i < Flocking.NumOfBoids; i++)
        {
            if (Flocking.Boids[i] == this)
            {
                continue;
            }

            if (isInRange(transform.position, Flocking.Boids[i].transform.position, Flocking.CohesionDistance))
            {
                cohesionNeighbors.Add(Flocking.Boids[i]);
            }

            if (isInRange(transform.position, Flocking.Boids[i].transform.position, Flocking.AlignmentDistance))
            {
                alignmentNeighbors.Add(Flocking.Boids[i]);
            }

            if (isInRange(transform.position, Flocking.Boids[i].transform.position, Flocking.SeparationDistance))
            {
                separationNeighbors.Add(Flocking.Boids[i]);
            }
        }

        Vector3 cPosition = Vector3.zero;
        for (int i = 0; i < cohesionNeighbors.Count; i++) 
        {
            cPosition += (Flocking.Boids[i].transform.position);
        }
        cPosition /= cohesionNeighbors.Count + 1;

        Vector3 cDirection = cPosition - transform.position;
        cDirection.Normalize();

        Vector3 aDirection = Vector3.zero;
        for (int i = 0; i < alignmentNeighbors.Count; i++) 
        {
            aDirection += (Flocking.Boids[i].CurrentDirection);
        }
        aDirection /= alignmentNeighbors.Count + 1;
        aDirection.Normalize();

        Vector3 sDirection = Vector3.zero;
        for (int i = 0; i < separationNeighbors.Count; i++)
        {
            sDirection += (Flocking.Boids[i].transform.position - transform.position);
        }
        sDirection /= separationNeighbors.Count + 1;
        sDirection.Normalize();


        Vector3 targetPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
        targetPos.y = this.transform.position.y;
        Helpers.Draw3DCross(targetPos, 0.5f);
        TargetDirection = targetPos -= transform.position;

        float stopDistance = 2.0f;
        TargetSpeed = TargetDirection.sqrMagnitude <= stopDistance * stopDistance ? 0.0f : MaxMoveSpeed;

        TargetDirection.Normalize();    

        //TargetDirection = SmoothingFuncitons.Damp(TargetDirection, sDirection, Flocking.SeparationWeight, Time.deltaTime);
        //TargetDirection = SmoothingFuncitons.Damp(TargetDirection, aDirection, Flocking.AlignmentWeight, Time.deltaTime);
        //TargetDirection = SmoothingFuncitons.Damp(TargetDirection, cDirection, Flocking.CohesionWeight, Time.deltaTime);

        CurrentDirection = SmoothingFuncitons.Damp(CurrentDirection, TargetDirection, RotationSpeed, Time.deltaTime);
        CurrentDirection.y = 0.0f;
        CurrentDirection.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(CurrentDirection, Vector3.up);
        transform.rotation = targetRot;

        CurrentSpeed = SmoothingFuncitons.ApproachReferenceLinear(CurrentSpeed, TargetSpeed, Acceleration * Time.deltaTime);
        transform.Translate(CurrentDirection * CurrentSpeed * Time.deltaTime);

        //ProcessPositionOutsideOfScreen();
    }

    public void ProcessPositionOutsideOfScreen()
    {
        Vector3 viewportPos = Flocking.MainCamera.WorldToViewportPoint(transform.position);

        bool wrapped = false;

        if (viewportPos.x < 0f - transform.localScale.x) { viewportPos.x = 1f + transform.localScale.x; wrapped = true; }
        else if (viewportPos.x > 1f + transform.localScale.x) { viewportPos.x = 0f - transform.localScale.x; wrapped = true; }

        if (viewportPos.y < 0f - transform.localScale.y) { viewportPos.y = 1f + transform.localScale.y; wrapped = true; }
        else if (viewportPos.y > 1f + transform.localScale.y) { viewportPos.y = 0f - transform.localScale.y; wrapped = true; }

        if (wrapped)
        {
            transform.Translate(Flocking.MainCamera.ViewportToWorldPoint(viewportPos) - transform.position);
        }
    }

    public bool isInRange(Vector3 pos1, Vector3 pos2, float range)
    {
        return (pos1 - pos2).sqrMagnitude <= range * range; 
    }
}
