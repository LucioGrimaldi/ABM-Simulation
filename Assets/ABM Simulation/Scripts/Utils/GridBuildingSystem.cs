using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GerardoUtils;
using System;

public class GridBuildingSystem : MonoBehaviour
{
    public static GridBuildingSystem Instance { get; private set; }

    public event EventHandler OnSelectedChanged;
    public event EventHandler OnObjectPlaced;

    [SerializeField] private List<ScriptableObjectType> placedSOList;
    private ScriptableObjectType placedObjectTypeSO;

    //private Grid2D<GridObject> grid;
    private Grid3D<GridObject> grid;
    private ScriptableObjectType.Dir direction;

    private void Awake()
    {
        Instance = this;

        int gridWidth = 10;
        int gridHeight = 1;
        int gridLenght = 10;
        float cellSize = 10f;
        grid = new Grid3D<GridObject>(gridWidth, gridHeight, gridLenght, cellSize, Vector3.zero, (Grid3D<GridObject> g, int x, int y, int z) => new GridObject(g, x, y, z));  //Costruttore per ogni gridObject

        placedObjectTypeSO = null;// placedSOList[0];

    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && placedObjectTypeSO != null)
        {
            //grid.GetXYZ(Mouse3DPosition.GetMouseWorldPosition(), out int x, out int y, out int z);
            //Instantiate(prefab, grid.GetWorldPosition(x, y, z), Quaternion.identity);

            grid.GetXYZ(Mouse3DPosition.GetMouseWorldPosition(), out int x, out int y, out int z);
            List<Vector3Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector3Int(x, y, z), direction);

            //Controllo Canbuild
            bool canBuild = true;
            foreach(Vector3Int gridPosition in gridPositionList)
            {
                if(!grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).CanBuild())
                {
                    //Non posso costruire
                    canBuild = false;
                    break;
                }

            }

            GridObject gridObject = grid.GetGridObject(x, y, z);

            if (canBuild)
            {
                Vector3Int rotationOffset = placedObjectTypeSO.GetRotationOffset(direction);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y, z) + new Vector3(rotationOffset.x, rotationOffset.y, rotationOffset.z) * grid.CellSize; //calcolo offset di rotazione

                PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector3Int(x, y, z), direction, placedObjectTypeSO);

                //Transform builtTransform = Instantiate(placedObjectTypeSO.prefab, placedObjectWorldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAnglePrefab(direction), 0));
                
                //direction = ScriptableObjectType.Dir.Down;

                foreach (Vector3Int gridPosition in gridPositionList) //setto transform per ogni posizione in gridPositionList
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).SetPlacedObject(placedObject);
                }

                OnObjectPlaced?.Invoke(this, EventArgs.Empty);

                //gridObject.SetTransform(builtTransform);
                //DeselectObjectType();
            }
            else
            {
                //Cannot build
                UtilsClass.CreateWorldTextPopup("Cannot build here!", Mouse3DPosition.GetMouseWorldPosition());
            }

        }

        //Demolizione
        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            GridObject gridObject = grid.GetGridObject(Mouse3DPosition.GetMouseWorldPosition());
            PlacedObject placedObject = gridObject.GetPlacedObject();
            if (placedObject != null)
            {
                //Distruggilo
                placedObject.Destroy();

                List<Vector3Int> gridPositionList = placedObject.GetGridPositionList();

                foreach (Vector3Int gridPosition in gridPositionList) //resetto transform per ogni posizione in gridPositionList
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).ClearPlacedObject();
                }
            }

        }

        //Spostamento
        if (Input.GetKeyDown(KeyCode.F))
        {
            Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
            if (grid.GetGridObject(mousePosition) != null)
            {
                // Valid Grid Position
                PlacedObject placedObject = grid.GetGridObject(mousePosition).GetPlacedObject();
                if (placedObject != null)
                {
                    // Distruggi
                    placedObject.Destroy();

                    List<Vector3Int> gridPositionList = placedObject.GetGridPositionList();
                    foreach (Vector3Int gridPosition in gridPositionList)
                    {
                        grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).ClearPlacedObject();
                    }
                }

                //placedObjectTypeSO = placedObject;->>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>><
            }
        }


        if (Input.GetKeyDown(KeyCode.R))
        {
            direction = ScriptableObjectType.GetNextDirection(direction);
            UtilsClass.CreateWorldTextPopup(null, "" + direction, Mouse3DPosition.GetMouseWorldPosition(), 40, Color.green, Mouse3DPosition.GetMouseWorldPosition() + new Vector3(0, 10), 2f);

        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { placedObjectTypeSO = placedSOList[0]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { placedObjectTypeSO = placedSOList[1]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { placedObjectTypeSO = placedSOList[2]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { placedObjectTypeSO = placedSOList[3]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { placedObjectTypeSO = placedSOList[4]; RefreshSelectedObjectType(); }

        if (Input.GetKeyDown(KeyCode.F1)) { placedObjectTypeSO = placedSOList[5]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.F2)) { placedObjectTypeSO = placedSOList[6]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.F3)) { placedObjectTypeSO = placedSOList[7]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.F4)) { placedObjectTypeSO = placedSOList[8]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.F5)) { placedObjectTypeSO = placedSOList[9]; RefreshSelectedObjectType(); }

        if (Input.GetKeyDown(KeyCode.Escape)) { DeselectObjectType(); }


    }


    public class GridObject
    {
        private Grid3D<GridObject> grid;
        private int x;
        private int y;
        private int z;
        private PlacedObject placedObject;

        public GridObject(Grid3D<GridObject> grid, int x, int y, int z)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        //Set e Get transform
        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, y, z); //aggiorno ToString se c'è modifica
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, y, z);
        }


        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }


        public bool CanBuild()
        {
            return placedObject == null;   //se è null posso costruire, altrimenti no
            
        }


        public override string ToString()
        {
            return x + "," + y + "," + z + "\n" + placedObject;
        }

    }


    private void DeselectObjectType()
    {
        placedObjectTypeSO = null;
        RefreshSelectedObjectType();
    }

    private void RefreshSelectedObjectType()
    {
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
    }


    //Metodi per Ghost


    //public Vector3Int GetGridPosition(Vector3 worldPosition)
    //{
    //    grid.GetXYZ(worldPosition, out int x, out int y, out int z);
    //    return new Vector3Int(x, y, z);
    //}

    public Vector3 GetMouseWorldSnappedPosition()
    {
        Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        grid.GetXYZ(mousePosition, out int x, out int y, out int z);

        if (placedObjectTypeSO != null)
        {
            Vector3Int rotationOffset = placedObjectTypeSO.GetRotationOffset(direction);    //prendo offset rotazione
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y, z) + new Vector3(rotationOffset.x, 0, rotationOffset.z) * grid.CellSize; //applico offset
            return placedObjectWorldPosition;
        }
        else
        {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation()
    {
        if (placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAnglePrefab(direction), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }

    public ScriptableObjectType GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }
}
