using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GerardoUtils;

public class Grid2D<TGridObject>

{
    private int width, height;
    //private int lenght;
    private TGridObject[,] gridArray;
    private float cellSize;
    private TextMesh[,] debugTextArray;
    private Vector3 originPosition;

    //public int Width { get; set;}

    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }

    public Grid2D(int width, int height, float cellSize, Vector3 originPosition, Func<Grid2D<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];
        debugTextArray = new TextMesh[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridObject(this, x, z);   //crea oggetto griglia del tipo che vogliamo, se è int ritorna 0 
            }
        }


        bool showDebug = true;
        if (showDebug)
        {
            TextMesh[,] debugTextArray = new TextMesh[width, height];
            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int z = 0; z < gridArray.GetLength(1); z++)
                {
                    debugTextArray[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center);
                    //Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 999f);
                    //Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 999f);

                }
            }
            //Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 999f);
            //Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 999f);


        }
    }

    //METODI


    //Converti x,y (gridPosition) in worldPosition
    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }


    //Converti worldPosition in gridPosition x,y
    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }


    public void SetGridObject(int x, int z, TGridObject value)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)    //ignora valori invalidi
        {
            gridArray[x, z] = value;
            //debugTextArray[x, z].text = gridArray[x, z].ToString();
            OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z });
        }
    }

    public void TriggerGridObjectChanged(int x, int z)
    {
        OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z });
    }


    public void SetGridObject(Vector3 worldPosition, TGridObject value)
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        SetGridObject(x, z, value);
    }


    //Get coordinate x,y
    public TGridObject GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
            return gridArray[x, z];
        else
            return default(TGridObject);    //Se è una griglia di interi ritorna 0, se bool ritorna false, altri tipi ritorna null
    }


    //Get worldPosition
    public TGridObject GetGridObject(Vector3 worldPosition)
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }



    public int Width { get => width; set => width = value; }
    public int Height { get => height; set => height = value; }
    //public int Lenght { get => lenght; set => lenght = value; }
    public float CellSize { get => cellSize; set => cellSize = value; }
}
