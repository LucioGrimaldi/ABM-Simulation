using System;
using UnityEngine;
using GerardoUtils;
using System.Collections.Generic;

public class Grid3D<TGridObject>                                                 //cambiare da TGRidObject a reference di TGridObject
{
    private int width, lenght, height;
    private TGridObject[,,] gridArray;
    private static float cellSize;
    private TextMesh[,,] debugTextArray;
    private static Vector3 originPosition;
    bool showDebug = false;

    //public int Width { get; set;}

    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs
    {
        public int x;
        public int y;
        public int z;
    }

    public Grid3D(int width, int height, int lenght, float cellSize, Vector3 originPosition, Func<Grid3D<TGridObject>, int, int, int, TGridObject> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.lenght = lenght;
        Grid3D<TGridObject>.cellSize = cellSize;
        Grid3D<TGridObject>.originPosition = originPosition;

        gridArray = new TGridObject[width, height, lenght];
        debugTextArray = new TextMesh[width, height, lenght];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for(int y = 0; y < gridArray.GetLength(1); y++)
            {
                for (int z = 0; z < gridArray.GetLength(2); z++)
                {
                    gridArray[x, y, z] = createGridObject(this, x, y, z);   //crea oggetto griglia del tipo che vogliamo, se è int ritorna 0 
                }
            }
        }

        if (showDebug)
        {
            TextMesh[,,] debugTextArray = new TextMesh[width, height, lenght];
            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for(int y = 0; y < gridArray.GetLength(1); y++)
                {
                    for (int z = 0; z < gridArray.GetLength(2); z++)
                    {
                        debugTextArray[x, y, z] = UtilsClass.CreateWorldText(gridArray[x, y, z]?.ToString(), null, GetWorldPosition(x, y, z) + new Vector3(cellSize, cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center);
                        Debug.DrawLine(GetWorldPosition(x, y, z), GetWorldPosition(x + 1, y, z), Color.white, 999f);
                        Debug.DrawLine(GetWorldPosition(x, y, z), GetWorldPosition(x, y + 1, z), Color.white, 999f);
                        Debug.DrawLine(GetWorldPosition(x, y, z), GetWorldPosition(x, y, z + 1), Color.white, 999f);
                    }
                }
                
            }
            Debug.DrawLine(GetWorldPosition(width, 0, 0), GetWorldPosition(width, height, lenght), Color.white, 999f);
            Debug.DrawLine(GetWorldPosition(0, height, 0), GetWorldPosition(width, height,lenght), Color.white, 999f);
            Debug.DrawLine(GetWorldPosition(0, 0, lenght), GetWorldPosition(width, height, lenght), Color.white, 999f);


        }
    }

    //METODI

    //Converti x,y (gridPosition) in worldPosition
    public static Vector3 GetWorldPosition(int x, int y, int z)
    {
        return new Vector3(x, y, z) * cellSize + originPosition;
    }

    //Converti worldPosition in gridPosition x,y
    public void GetXYZ(Vector3 worldPosition, out int x, out int y, out int z)
    {
        
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);

        if (height == 1)
            y = 0;
        
    }
    public void SetGridObject(int x, int y, int z, TGridObject value)
    {
        if (x >= 0 && y >= 0 && z >= 0 && x < width && y < height && z < lenght)    //ignora valori invalidi
        {
            
            gridArray[x, y, z] = value;
            //debugTextArray[x, z].text = gridArray[x, z].ToString();
            OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, y = y, z = z });
        }
    }
    public void TriggerGridObjectChanged(int x, int y, int z)
    {
        OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, y = y, z = z });
    }
    public void SetGridObject(Vector3 worldPosition, TGridObject value)
    {
        int x, y, z;
        GetXYZ(worldPosition, out x, out y, out z);

        //prendi dimensioni base (da value) e vedi quante celle occupa (1x1, 2x2)
        //calcola le celle da occupare e poi settale


        SetGridObject(x, y, z, value);
    }

    //Get coordinate x,y,z
    public TGridObject GetGridObject(int x, int y, int z)
    {
        if (x >= 0 && y >= 0 && z >= 0 && x < width && y < height && z < lenght)
            return gridArray[x, y, z];
        else
            return default(TGridObject);    //Se è una griglia di interi ritorna 0, se bool ritorna false, altri tipi ritorna null
    }

    //Get worldPosition
    public TGridObject GetGridObject(Vector3 worldPosition)
    {
        int x, y, z;
        GetXYZ(worldPosition, out x, out y, out z);
        return GetGridObject(x, y, z);
    }


    public int Width { get => width; set => width = value; }
    public int Height { get => height; set => height = value; }
    public int Lenght { get => lenght; set => lenght = value; }
    public float CellSize { get => cellSize; set => cellSize = value; }
    public TGridObject[,,] GridArray { get => gridArray; set => gridArray = value; }
}
