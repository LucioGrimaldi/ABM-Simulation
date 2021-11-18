using UnityEngine;
using static SimObjectRender;

public class PO_Continuous3D : PO_Continuous
{
    // STATIC
    private static ContinuousSystem continuousSystem;
    public static PO_Continuous3D Create(SimObject simObject, PO_Continuous3D po, bool isGhost, bool isMovable)
    {
        continuousSystem = GameObject.Find("SimSpaceSystem").GetComponent<ContinuousSystem>();

        PO_Continuous3D po_clone = Instantiate(po, continuousSystem.MasonToWorldPosition3D(po.GetPosition()), Quaternion.Euler(Vector3.zero));
        //po_clone.transform.GetChild(1).gameObject.AddComponent<Outline>().enabled = false;
        po_clone.simObject = simObject.Clone();

        po_clone.gameObject.GetComponent<MeshFilter>().mesh = po.SimObjectRender.Meshes["default"].GetComponent<MeshFilter>().mesh;
        if (isGhost)
        {
            po_clone.gameObject.GetComponent<MeshRenderer>().material = po_clone.SimObjectRender.ghostMaterial;
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 10);
        }
        else
        {
            po_clone.gameObject.GetComponent<MeshRenderer>().material = po_clone.simObjectRender.Materials["default"];
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 9);
            continuousSystem.placedObjectsDict.TryAdd((po_clone.simObject.Type, po_clone.simObject.Class_name, po_clone.simObject.Id), (isGhost, po_clone));
        }
        po_clone.isGhost = isGhost; po_clone.isMovable = isMovable;
        return po_clone;
    }


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
                if (isMovable)
                {
                    Vector3 targetPosition = continuousSystem.MasonToWorldPosition3D(GetPosition());
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f); 
                    transform.rotation = Quaternion.Lerp(transform.rotation, GetFacingDirection(transform.position, targetPosition), Time.deltaTime * 15f);
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


    public override void Rotate()
    {
        throw new System.NotImplementedException();
    }
    public void Rotate(Quaternion rotation)
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 15f);
    }
    public Vector3 GetPosition()
    {
        return (Vector3)simObject.Parameters["position"];
    }
    public Quaternion GetFacingDirection(Vector3 last_pos, Vector3 new_pos)
    {
        Vector3 dir = last_pos - new_pos;
        return Quaternion.LookRotation(dir, Vector3.up);
    }
}
