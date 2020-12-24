using System;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class CustomGrid
{ 
    public Node[,] Grid { get { return grid; } }
    public int Dimension { get; private set; }
    private int gridToWorldScale;
    private Node[,] grid;
    private Vector3 offset;
    public Node GetNode(int x, int y)
    {
        try
        {
            return grid[x, y];
        }
        catch (IndexOutOfRangeException) { return null; }
    }
    public Node GetNode(Vector2Int pos)
    {
        try
        {
            return grid[pos.x, pos.y];
        }
        catch (IndexOutOfRangeException) { return null; }
    }
    public void ChangeNode(int x, int y, Node node)
    {
        grid[x, y] = node;
    }
    public CustomGrid(Vector3 offset, int basesize = 75, int gridToWorldScale = 10)
    {
        this.offset = offset;
        this.gridToWorldScale = gridToWorldScale;
        Dimension = basesize;
        grid = new Node[basesize, basesize];
        for (int i = 0; i < basesize; i++)
        {
            for (int k = 0; k < basesize; k++)
            {
                grid[i, k] = new Node(i, k);
            }
        }

    }
    public Vector3 GetWorldPosition(Vector2Int position)
    {
        int x = position.x;
        int y = position.y;
        return (Vector3) (new Vector2(x, y) + (Vector2) offset) * gridToWorldScale + new Vector3(gridToWorldScale / 2, gridToWorldScale / 2, offset.z);
    }
    public Vector3 GetWorldPosition(int x, int y)
    {
        return (Vector3)(new Vector3(x + offset.x, 0, y+offset.z)) * gridToWorldScale + new Vector3(gridToWorldScale / 2, offset.y, gridToWorldScale / 2);
    }
    public Vector2Int WorldToGridPosition(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt((position.x - offset.x) / gridToWorldScale), Mathf.FloorToInt((position.z - offset.z) / gridToWorldScale));
    }

    public void IncreaseGridSize(int additionalspace)
    {
        Debug.Log(additionalspace);
        int currentsize = grid.GetUpperBound(0) + 1;
        Node[,] newGrid = new Node[currentsize + additionalspace, currentsize + additionalspace];
        for(int i = 0; i < currentsize; i++)
        {
            for (int k = 0; k < currentsize; k++)
            {
                newGrid[i, k] = grid[i, k];
            }
        }
        for (int i = currentsize; i < currentsize + additionalspace; i++)
        {
            for (int k = 0; k < currentsize + additionalspace; k++)
            {
                newGrid[i, k] = new Node(i, k);
            }
        }
        for (int i = currentsize; i < currentsize + additionalspace; i++)
        {
            for (int k = 0; k < currentsize + additionalspace; k++)
            {
                newGrid[k, i] = new Node(k, i);
            }
        }
        Dimension = currentsize + additionalspace;
        grid = newGrid;
    }
    public void ReduceGridSize(int decreaseRate)
    {
        int currentsize = grid.GetUpperBound(0) + 1;
        Node[,] newGrid = new Node[currentsize - decreaseRate, currentsize - decreaseRate];
        for (int i = 0; i < currentsize - decreaseRate; i++)
        {
            for (int k = 0; k < currentsize - decreaseRate; k++)
            {
                newGrid[i, k] = grid[i, k];
            }
        }
        Dimension = currentsize - decreaseRate;
        grid = newGrid;
    }
}

[System.Serializable]
public class Node
{
    public int x;
    public int y;
    public GameObject instantiatedGO;
    public Tile tile;
    public Node(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

[System.Serializable]
public class SaveFile
{
    public int gridDimension;
    public float[] offset;
    public int cellSize;

}
