using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PropsSO", menuName = "PropsSO", order = 0)]
public class PropsSO : ScriptableObject 
{
    PropsData[] wallPropsData;
    PropsData[] wallDecorPropsData;
    PropsData[] floorPropsData;
    PropsData[] ceilingPropsData;


}

public class PropsData 
{
    public GameObject prefab;
    public Mesh mesh;
    public Material material;
    
}