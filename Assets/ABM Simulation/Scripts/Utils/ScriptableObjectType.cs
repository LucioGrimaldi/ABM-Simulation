using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "ScriptableObjects/PlacedScriptableObj")]
public class ScriptableObjectType : ScriptableObject
{
    public string nameString;
    public Transform prefab;
    public Transform visual;
    public int width, height, lenght;

    public enum Dir
    {
        Down,
        Left,
        Up,
        Right,
    }

    public List<Vector3Int> GetGridPositionList(Vector3Int offset, Dir dir)     //offset = posizione di Raycast
    {
        List<Vector3Int> gridPositionList = new List<Vector3Int>();
        switch (dir)
        {
            default:
            case Dir.Down:
            case Dir.Up:
                for (int x = 0; x < width; x++)
                {
                    //Debug.Log("width = " + width);
                    for (int y = 0; y < height; y++)
                    {
                        //Debug.Log("height = " + height);
                        for (int z = 0; z < lenght; z++)
                        {
                            //Debug.Log("lenght = " + lenght);
                            gridPositionList.Add(offset + new Vector3Int(x, y, z)); //sommo posizioni e ritorno vector3 di posizioni da occupare
                            
                        }
                    }
                }
                break;
            case Dir.Left:
            case Dir.Right:
                for (int x = 0; x < lenght; x++)
                {
                    //Debug.Log("lenght = " + lenght);
                    for (int y = 0; y < height; y++)
                    {
                        //Debug.Log("height = " + height);
                        for (int z = 0; z < width; z++)
                        {
                            //Debug.Log("width = " + width);
                            gridPositionList.Add(offset + new Vector3Int(x, y, z));
                        }
                    }
                }
                break;
        }
        return gridPositionList;
    }


    public static Dir GetNextDirection(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return Dir.Left;
            case Dir.Left: return Dir.Up;
            case Dir.Up: return Dir.Right;
            case Dir.Right: return Dir.Down;
        }
    }


    //ruoto prefab nella direzione desiderata
    public int GetRotationAnglePrefab(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return 0;
            case Dir.Left: return 90;
            case Dir.Up: return 180;
            case Dir.Right: return 270;
        }
    }


    //calcolo offset della direzione per spawnare oggetto sulla posizione giusta
    public Vector3Int GetRotationOffset(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return new Vector3Int(0, 0, 0);
            case Dir.Left: return new Vector3Int(0, 0, width);

            //"Mannaggia saliern"
            case Dir.Up: return new Vector3Int(width, 0, lenght);
            case Dir.Right: return new Vector3Int(lenght, 0, 0);
        }
    }

    public static explicit operator ScriptableObjectType(PlacedObject v)
    {
        throw new NotImplementedException();
    }
}
