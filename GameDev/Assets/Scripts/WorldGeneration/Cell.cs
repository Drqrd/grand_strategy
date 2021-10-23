using UnityEngine;

public class Cell
{
    public TectonicPlate ParentPlate { get; private set; }
    public int[] Triangles { get; private set; }
    public bool IsBorderTriangle { get; private set; }
    public int[] BorderLine { get; private set; }
    
    public Cell(TectonicPlate parent, int[] triangles, int[]borderLine = null)
    {
        ParentPlate = parent;
        Triangles = triangles;
        if (borderLine != null) 
        { 
            BorderLine = borderLine;
            IsBorderTriangle = true;
        }
        else
        {
            BorderLine = null;
            IsBorderTriangle = false;
        }
    }
}
