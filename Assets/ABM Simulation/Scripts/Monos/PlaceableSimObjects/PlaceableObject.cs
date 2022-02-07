using System;
using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public static T Create<T>(SimObject simObject, T po, bool isGhost, bool isMovable) where T : PlaceableObject, new()
    {
        return (T)po.Init(simObject, po, isGhost, isMovable);
    }
    protected virtual PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        return null;
    }

    // NON-STATIC
    protected SimObject simObject;
    [SerializeField] protected SimObjectRender simObjectRender;

    [SerializeField] protected bool isGhost = false;
    [SerializeField] protected bool isMovable = false;
    [SerializeField] protected bool isSelected = false;

    public SimObject SimObject { get => simObject; set => simObject = value; }
    public SimObjectRender SimObjectRender { get => simObjectRender; set => simObjectRender = value; }
    public bool IsGhost { get => isGhost; set => isGhost = value; }
    public bool IsMovable { get => isMovable; set => isMovable = value; }
    public bool IsSelected { get => isSelected; set => isSelected = value; }


    public virtual void MakeGhost(bool isMovable)
    {
        SetMaterialRecursive(transform.Find("Model").gameObject, SimObjectRender.ghostMaterial);
        SimSpaceSystem.SetLayerRecursive(this.gameObject, 10);
        isGhost = true; this.isMovable = isMovable;
    }
    public virtual bool PlaceGhost(Vector3 position)
    {
        isMovable = false;
        return true;
    }
    public virtual bool PlaceGhost()
    {
        isMovable = false;
        return true;
    }
    public virtual void Rotate() { }
    public virtual void Rotate(Vector3 dir) { }
    public virtual void Rotate(PO_Discrete2D.DirEnum dir) { }
    public virtual void Confirm()
    {
        if (SimObjectRender.RenderType.Equals(SimObjectRender.RenderTypeEnum.MESH))
        {
            // TODO
            //transform.Find("Model").GetComponent<MeshFilter>().mesh = SimObjectRender.Meshes["default"].GetComponent<Mesh>();
            SetMaterialRecursive(transform.Find("Model").gameObject, SimObjectRender.Materials["default"]);
        }
    }
    public virtual void Destroy()
    {
        Destroy(gameObject);
    }
    public virtual void SetScale(float scale)
    {

    }
    public virtual void SetScale(float scale_x, float scale_y)
    {

    }
    public virtual void SetScale(float scale_x, float scale_y, float scale_z)
    {

    }
    public virtual void SetMaterialRecursive(GameObject targetGameObject, Material material)
    {
        if(targetGameObject.GetComponent<MeshRenderer>() != null) targetGameObject.GetComponent<MeshRenderer>().material = material;
        Material[] new_mats = new Material[] { material };
        if (targetGameObject.GetComponent<SkinnedMeshRenderer>() != null) targetGameObject.GetComponent<SkinnedMeshRenderer>().materials = new_mats;
        foreach (Transform child in targetGameObject.transform)
        {
            SetMaterialRecursive(child.gameObject, material);
        }
    }
    public virtual void Highlight()
    {
        gameObject.transform.Find("Model").GetComponent<Outline>().enabled = true;
    }
    public virtual void DeHighlight()
    {
        gameObject.transform.Find("Model").GetComponent<Outline>().enabled = false;
    }
    public override string ToString()
    {
        return "TYPE: " + simObject.Type + " " + ", CLASS_NAME: " + simObject.Class_name + ", ID: " + simObject.Id + " as " + SimObjectRender.RenderType + "."; 
    }
}
