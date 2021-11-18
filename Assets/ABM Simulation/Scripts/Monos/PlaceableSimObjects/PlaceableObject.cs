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

    public SimObject SimObject { get => simObject; set => simObject = value; }
    public SimObjectRender SimObjectRender { get => simObjectRender; set => simObjectRender = value; }
    public bool IsGhost { get => isGhost; set => isGhost = value; }
    public bool IsMovable { get => isMovable; set => isMovable = value; }

    public virtual void MakeGhost()
    {
        SetMaterialRecursive(transform.GetChild(1).gameObject, SimObjectRender.ghostMaterial);
        SimSpaceSystem.SetLayerRecursive(this.gameObject, 10);
        isGhost = true; isMovable = true;
    }
    public virtual bool Place(Vector3 position)
    {
        gameObject.transform.position = position;
        isMovable = false;
        return true;
    }
    public virtual void Confirm()
    {
        isGhost = false; isMovable = false;
        // SetMaterialRecursive(transform.GetChild(1).gameObject, simObjectRender.Materials["default"]);                    NOT WORKING PROPERLY 'CAUSE OF FUNKY MESHES
        gameObject.transform.GetChild(1).transform.parent = null;
        Instantiate(simObjectRender.Meshes["default"], transform.position, transform.rotation, this.transform);
        SimSpaceSystem.SetLayerRecursive(this.gameObject, 9);
    }
    public virtual void Rotate() { }
    public virtual void Destroy()
    {
        GameObject.Destroy(this.gameObject);
    }
    public virtual void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale/10f, scale/10f, scale/10f);
    }
    public virtual void SetMaterialRecursive(GameObject targetGameObject, Material material)
    {
        if(targetGameObject.GetComponent<MeshRenderer>() != null) targetGameObject.GetComponent<MeshRenderer>().material = material;
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
