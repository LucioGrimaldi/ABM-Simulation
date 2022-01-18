using UnityEngine;
using static SimObjectRender;

public class PO_Continuous3D : PO_Continuous
{
    // STATIC
    protected static ContinuousSystem continuousSystem;
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        continuousSystem = GameObject.Find("SimSpaceSystem").GetComponent<ContinuousSystem>();

        PO_Continuous3D po_clone = Instantiate((PO_Continuous3D)po, ContinuousSystem.MasonToUnityPosition3D((Vector3)simObject.Parameters["position"]), Quaternion.Euler(Vector3.zero));
        po_clone.transform.Find("Model").GetComponent<Outline>().enabled = false;
        //po_clone.transform.Find("Model").GetComponent<Outline>().OutlineWidth = 5f * Mathf.Max(po_clone.width, po_clone.lenght);
        SetScale(ContinuousSystem.width, ContinuousSystem.height, ContinuousSystem.lenght);
        if (isGhost)
        {
            po_clone.simObject = simObject.Clone();
            po_clone.SetMaterialRecursive(po_clone.transform.Find("Model").gameObject, po_clone.SimObjectRender.ghostMaterial);
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 10);
        }
        else
        {
            po_clone.simObject = simObject;
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
                    Vector3 targetPosition = ContinuousSystem.MasonToUnityPosition3D(GetPosition());
                    transform.rotation = Quaternion.Lerp(transform.rotation, GetFacingDirection(transform.position, targetPosition), Time.deltaTime * 15f);
                    transform.position = targetPosition;
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


    public override bool PlaceGhost(Vector3 position)
    {
        simObject.Parameters["position"] = ContinuousSystem.UnityToMasonPosition3D(position);
        continuousSystem.placedGhostsDict.TryAdd((simObject.Type, simObject.Class_name, simObject.Id), (isGhost, this));
        base.PlaceGhost(position);
        return true;
    }
    public override void Rotate()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0,90,0)), Time.deltaTime * 15f);
    }
    public void Rotate(Quaternion rotation)
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 15f);
    }
    public override void SetScale(float scale_x, float scale_y, float scale_z)
    {
        float scale = Mathf.Max(scale_x, scale_y, scale_z);
        transform.localScale = new Vector3(1 / (scale / 100f), 1 / (scale / 100f), 1 / (scale / 100f));
    }
    public Vector3 GetPosition()
    {
        return (Vector3)simObject.Parameters["position"];
    }
    public Quaternion GetFacingDirection(Vector3 last_pos, Vector3 new_pos)
    {
        Vector3 dir = new_pos - last_pos;
        if(!dir.Equals(Vector3.zero)) return Quaternion.LookRotation(dir, Vector3.up);
        return transform.rotation;
    }
}
