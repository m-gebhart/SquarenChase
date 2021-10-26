using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ColorMaterialSet
{
    public Color skyBoxColor;
    public List<Material> materials;
    public ESnC_Landscapes landscape;
}

public enum ESnC_Landscapes
{
    concrete,
    town,
    farm,
    forest,
    desert
};

public class SnCMaterialManager : MonoBehaviour
{
    public List<ColorMaterialSet> colorMaterialSets;
    [HideInInspector]
    public ColorMaterialSet currentColorMaterialSet;
    [HideInInspector]
    public List<Material> currentMaterials;
    [HideInInspector]
    public ESnC_Landscapes currentLandscape;

    public void ChangeCurrentMaterialSet() 
    {
        currentColorMaterialSet = colorMaterialSets[Random.Range(0, colorMaterialSets.Count)];
        currentMaterials = currentColorMaterialSet.materials;
        currentLandscape = currentColorMaterialSet.landscape;
    }

    public ESnC_Landscapes GetLandscapeSetting() { return currentColorMaterialSet.landscape; }
}
