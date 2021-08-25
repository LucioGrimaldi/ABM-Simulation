using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//creo transform e mantengo dati di oggetti costruiti

public class PlacedObject : MonoBehaviour
{

    private ScriptableObjectType placedObjectTypeSO;
    private ScriptableObjectType.Dir direction;
    private Vector3Int origin;


    public static PlacedObject Create(Vector3 worldPosition, Vector3Int origin, ScriptableObjectType.Dir dir, ScriptableObjectType placedObjectTypeSO)
    {
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAnglePrefab(dir), 0));

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();

        placedObject.placedObjectTypeSO = placedObjectTypeSO;
        placedObject.origin = origin;
        placedObject.direction = dir;

        //placedObject.Setup(placedObjectTypeSO, origin, dir);

        return placedObject;
    }


    //private void Setup(ScriptableObjectType placedObjectTypeSO, Vector3Int origin, ScriptableObjectType.Dir dir)
    //{
    //    this.placedObjectTypeSO = placedObjectTypeSO;
    //    this.origin = origin;
    //    this.direction = dir;
    //}


    //Prendo le gridposition che l'oggetto sta occupando
    public List<Vector3Int> GetGridPositionList()
    {
        return placedObjectTypeSO.GetGridPositionList(origin, direction);
    }


    public void Destroy()
    {
        Destroy(gameObject); //distruggo questo gameObject
    }


    public override string ToString()
    {
        return placedObjectTypeSO.nameString;
    }

}
