using TMPro;
using UnityEngine;
using SFB;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class ShapeTester : MonoBehaviour
{

    public TMP_Text shapeDetails;
    public TMP_Text errorDetails;
    public GameObject shapePrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {    }

    public void OnAddShapeFromFile(bool deleteCurrent)
    {

        string[] selectedFiles = StandaloneFileBrowser.OpenFilePanel("Open Shape Files", "", "json", true);
        if (selectedFiles == null || selectedFiles.Length == 0) { return; }

        if (deleteCurrent)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (string s in selectedFiles)
        {
            AddNewShapeFromPath(s);
        }

        //Do this on the next frame. Gives Unity time to actually destroy the old game objects if it needs to.
        Invoke("RecalculatePolyCount", 0);
    }

    void RecalculatePolyCount()
    {
        int pCount = 0;
        foreach (MeshFilter filters in GetComponentsInChildren<MeshFilter>())
        {
            pCount += filters.mesh.vertexCount;
        }   
        shapeDetails.text = "Currently loaded " + transform.childCount + " models with a total of " + pCount + " polygons.";
    }

    void AddNewShapeFromPath(string filePath)
    {
        try
        {
            ShapeTesselator tess = new ShapeTesselator();
            ShapeJSON shape = ShapeAccessor.DeserializeShapeFromFile(filePath);
            VSMeshData mesh = tess.TesselateShape(shape);

            errorDetails.text = "Textures:";
            foreach (var val in shape.Textures)
            {
                errorDetails.text += "\n"+val.Key + " : " + val.Value;
            }

            GameObject ch = GameObject.Instantiate(shapePrefab, transform);
            Mesh unityMesh = new Mesh();
            unityMesh.SetVertices(mesh.vertices);
            unityMesh.SetUVs(0, mesh.uvs);
            unityMesh.SetTriangles(mesh.indices, 0);

            List<Vector2> textureIndicesV2 = new List<Vector2>();
            foreach (int i in mesh.textureIndices)
            {
                textureIndicesV2.Add(new Vector2(i + 0.5f, i + 0.5f));
            }
            unityMesh.SetUVs(1, textureIndicesV2);

            unityMesh.RecalculateBounds();
            unityMesh.RecalculateNormals();
            unityMesh.RecalculateTangents();

            ch.GetComponent<MeshFilter>().mesh = unityMesh;
            ch.GetComponent<MeshRenderer>().material.SetTexture("_AvailableTextures", shape.loadedTextures);

        } catch (System.Exception e)
        {
            errorDetails.text = "Failed to add shape from path: "+filePath+" with following exception: "+e.Message;
            errorDetails.color = Color.red;
        }
    }


}
