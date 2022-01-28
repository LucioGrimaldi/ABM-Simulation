using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimObjectRender;

public class Flocker_PO : PO_Continuous3D
{
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        dead = false;
        return base.Init(simObject, po, isGhost, isMovable);
    }

    // VARIABLES
    public bool dead;

    // UNITY LOOP METHODS

    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {

    }
    /// <summary>
    /// LateUpdate routine (Unity Process)
    /// </summary>
    private void LateUpdate()
    {
        if (simObjectRender.RenderType.Equals(RenderTypeEnum.MESH))         // TODO (TEXTURE,PARTICLE_SYSTEM,OTHER..)
        {
            if (isGhost)
            {
                if (isMovable)
                {
                    Vector3 targetPosition = continuousSystem.MouseClickToSpawnPosition(this);
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(Vector3.zero), Time.deltaTime * 15f);
                }
            }
            else
            {
                ChangeMesh((bool)simObject.Parameters["dead"]);
                if (isMovable)
                {
                    Vector3 targetPosition = ContinuousSystem.MasonToUnityPosition3D(GetPosition());
                    transform.rotation = Quaternion.Lerp(transform.rotation, GetFacingDirection(transform.position, targetPosition), Time.deltaTime * 15f);
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
                }
            }
        }
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {

    }

    private void ChangeMesh(bool dead)
    {
        if (!this.dead && dead)
        {
            this.dead = dead;
            Transform temp = transform.Find("Model");
            temp.parent = null;
            GameObject newModel = Instantiate(SimObjectRender.Meshes["dead"].transform.Find("Model").gameObject, temp.position, temp.rotation);
            SimSpaceSystem.SetLayerRecursive(newModel.gameObject, 9);
            newModel.name = "Model";
            newModel.transform.parent = transform;
            Destroy(temp.gameObject);
        }


    }

}