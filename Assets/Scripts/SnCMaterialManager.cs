using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ColorMaterialSet
{
    public Color skyBoxColor;
    public List<Material> materials;
}

public class SnCMaterialManager : MonoBehaviour
{
    public List<ColorMaterialSet> colorMaterialSets;
    [HideInInspector]
    public ColorMaterialSet currentColorMaterialSet;
    public List<Material> currentMaterials;

    public void ChangeCurrentMaterialSet() 
    {
        currentColorMaterialSet = colorMaterialSets[Random.Range(0, colorMaterialSets.Count)];
        currentMaterials = currentColorMaterialSet.materials;
    }
}
