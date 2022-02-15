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
    protected override void Start()
    {
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    protected override void OnEnable()
    {

    }
    /// <summary>
    /// LateUpdate routine (Unity Process)
    /// </summary>
    protected override void LateUpdate()
    {
        if (simObjectRender.RenderType.Equals(RenderTypeEnum.MESH))         // TODO (TEXTURE,PARTICLE_SYSTEM,OTHER..)
        {
            if (isGhost)
            {
                ChangeMesh((bool)simObject.Parameters["dead"]);
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
    protected override void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    protected override void OnDisable()
    {

    }

    private void ChangeMesh(bool dead)
    {
        string model = "";
        if (!this.dead && dead) model = "dead";
        else if (this.dead && !dead) model = "default";
        if (this.dead != dead)
        {
            this.dead = dead;
            if(!isGhost) isMovable = !dead;
            transform.localScale = new Vector3(1, 1, 1);
            transform.localRotation = Quaternion.Euler(Vector3.zero);
            Transform temp = transform.Find("Model");
            temp.parent = null;
            GameObject newModel = Instantiate(SimObjectRender.Meshes[model].transform.Find("Model").gameObject, temp.position, Quaternion.Euler(Vector3.zero));
            newModel.name = "Model";
            newModel.transform.parent = transform;
            SetScale(ContinuousSystem.width, ContinuousSystem.height, ContinuousSystem.length);
            Destroy(temp.gameObject);
            if (isGhost) SetMaterialRecursive(transform.Find("Model").gameObject, SimObjectRender.ghostMaterial);
            SimSpaceSystem.SetLayerRecursive(newModel.gameObject, 9);
            if (isSelected) Highlight();
            if(transform.Find("Chicken")) transform.Find("Chicken").GetComponent<Outline>().enabled = true;

        }
    }

}
