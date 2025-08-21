using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gameheads : MonoBehaviour
{
    [Header("Billboard Settings")]
    public MeshRenderer meshRenderer;
    
    [Header("Animation Settings")]
    public float scrollSpeed = 1.0f;
    public bool scrollMaterial4 = true;
    public bool scrollMaterial5 = true;
    
    private Material[] materialInstances;
    private MaterialPropertyBlock[] propertyBlocks;
    
    void Start()
    {
        // Get the mesh renderer if not assigned
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
            
        // Create material instances to avoid affecting other objects
        materialInstances = new Material[meshRenderer.materials.Length];
        for (int i = 0; i < meshRenderer.materials.Length; i++)
        {
            materialInstances[i] = new Material(meshRenderer.materials[i]);
        }
        meshRenderer.materials = materialInstances;
        
        // Alternative approach using MaterialPropertyBlocks (more performance-friendly)
        // Uncomment these lines and comment out the material instances code above if you prefer this method
        /*
        propertyBlocks = new MaterialPropertyBlock[meshRenderer.materials.Length];
        for (int i = 0; i < propertyBlocks.Length; i++)
        {
            propertyBlocks[i] = new MaterialPropertyBlock();
        }
        */
    }
    
    void Update()
    {
        // Method 1: Using Material Instances
        AnimateWithMaterialInstances();
        
        // Method 2: Using MaterialPropertyBlocks (uncomment if using this approach)
        // AnimateWithPropertyBlocks();
    }
    
    void AnimateWithMaterialInstances()
    {
        float offsetX = Time.time * scrollSpeed;
        
        // Animate 5th material (index 4)
        if (scrollMaterial5 && materialInstances.Length > 4)
        {
            Vector2 currentOffset = materialInstances[4].mainTextureOffset;
            materialInstances[4].mainTextureOffset = new Vector2(offsetX, currentOffset.y);
        }
        
        // Animate 5th material (index 5)
        if (scrollMaterial5 && materialInstances.Length > 5)
        {
            Vector2 currentOffset = materialInstances[5].mainTextureOffset;
            materialInstances[5].mainTextureOffset = new Vector2(offsetX, currentOffset.y);
        }
    }
    
    void AnimateWithPropertyBlocks()
    {
        float offsetX = Time.time * scrollSpeed;
        
        // Animate 5th material (index 4)
        if (scrollMaterial5 && propertyBlocks.Length > 4)
        {
            Vector4 tilingOffset = new Vector4(1, 1, offsetX, 0); // Scale X, Scale Y, Offset X, Offset Y
            propertyBlocks[4].SetVector("_BaseMap_ST", tilingOffset);
            meshRenderer.SetPropertyBlock(propertyBlocks[4], 4);
        }
        
        // Animate 5th material (index 5)
        if (scrollMaterial5 && propertyBlocks.Length > 5)
        {
            Vector4 tilingOffset = new Vector4(1, 1, offsetX, 0);
            propertyBlocks[5].SetVector("_BaseMap_ST", tilingOffset);
            meshRenderer.SetPropertyBlock(propertyBlocks[5], 5);
        }
    }
    
    void OnDestroy()
    {
        // Clean up material instances to prevent memory leaks
        if (materialInstances != null)
        {
            for (int i = 0; i < materialInstances.Length; i++)
            {
                if (materialInstances[i] != null)
                    DestroyImmediate(materialInstances[i]);
            }
        }
    }
}