using System;
using System.Collections.Generic;
using UnityEngine;

public class ShapeTesselator
{

    StackMatrix4 stackMatrix = new StackMatrix4(64);
    Vector3 rotationOrigin;

    /// <summary>
    /// XYZ Vertex positions for every vertex in a cube. Origin is the cube middle point.
    /// </summary>
    public static int[] CubeVertices = {
            // North face
            -1, -1, -1,
            -1,  1, -1,
            1,  1, -1,
            1, -1, -1,

            // East face
            1, -1, -1,     // bot left
            1,  1, -1,     // top left
            1,  1,  1,     // top right
            1, -1,  1,     // bot right

            // South face
            -1, -1,  1,
            1, -1,  1,
            1,  1,  1,
            -1,  1,  1,

            // West face
            -1, -1, -1,
            -1, -1,  1,
            -1,  1,  1,
            -1,  1, -1,
            
            // Top face
            -1,  1, -1,
            -1,  1,  1,
            1,  1,  1,
            1,  1, -1,
                          
            // Bottom face
            -1, -1, -1,
            1, -1, -1,
            1, -1,  1,
            -1, -1,  1
        };

    /// <summary>
    /// UV Coords for every Vertex in a cube
    /// </summary>
    public static int[] CubeUvCoords = {
            // North
            1, 0,
            1, 1,
            0, 1,
            0, 0,

            // East 
            1, 0,
            1, 1,
            0, 1,
            0, 0,

            // South
            0, 0,
            1, 0,
            1, 1,
            0, 1,
            
            // West
            0, 0,
            1, 0,
            1, 1,
            0, 1,

            // Top face
            0, 1,
            0, 0,
            1, 0,
            1, 1,

            // Bottom face
            1, 1,
            0, 1,
            0, 0,
            1, 0,
        };


    /// <summary>
    /// Indices for every triangle in a cube
    /// </summary>
    public static int[] CubeVertexIndices = {
            0, 1, 2,      0, 2, 3,    // North face
            4, 5, 6,      4, 6, 7,    // East face
            8, 9, 10,     8, 10, 11,  // South face
            12, 13, 14,   12, 14, 15, // West face
            16, 17, 18,   16, 18, 19, // Top face
            20, 21, 22,   20, 22, 23  // Bottom face
        };

    /// <summary>
    /// Can be used for any face if offseted correctly
    /// </summary>
    public static int[] BaseCubeVertexIndices =
    {
            0, 1, 2,      0, 2, 3
        };

    public List<VSMeshData> TesselateShape(ShapeJSON shape)
    {
        List<VSMeshData> meshData = new List<VSMeshData>();

        System.DateTime pre = System.DateTime.Now;
        ResolveAllMatricesForShape(shape);
        Debug.Log("Calculating full shape matrices took " + (DateTime.Now - pre).TotalMilliseconds + "ms.");

        pre = System.DateTime.Now;
        TesselateShapeElements(meshData, shape.Elements, shape.TextureSizeMultipliers);
        Debug.Log("Calculating mesh data for shape took " + (DateTime.Now - pre).TotalMilliseconds + "ms.");
        return meshData;
    }
        
    private void TesselateShapeElements(List<VSMeshData> meshData, ShapeElementJSON[] elements, Vector2[] textureSizes)
    {
        foreach (ShapeElementJSON element in elements)
        {
            //Tesselate element now.
            VSMeshData elementMeshData = new VSMeshData();
            TesselateShapeElement(elementMeshData, element, textureSizes);
            elementMeshData.MatrixTransform(element.cachedMatrix);
            meshData.Add(elementMeshData);

            //Now do children.
            if (element.Children != null)
            {
                TesselateShapeElements(meshData, element.Children, textureSizes);
            }
        }
    }

    private void TesselateShapeElement(VSMeshData meshData, ShapeElementJSON element, Vector2[] textureSizes)
    {
        Vector3 size = new Vector3(
            ((float)element.To[0] - (float)element.From[0]) / 16f,
            ((float)element.To[1] - (float)element.From[1]) / 16f,
            ((float)element.To[2] - (float)element.From[2]) / 16f);
        if (size == Vector3.zero) return;

        Vector3 relativeCenter = size / 2;

        for (int f = 0; f < 6; f++)
        {
            ShapeElementFaceJSON face = element.FacesResolved[f];
            if (face == null) continue;
            BlockFacing facing = BlockFacing.ALLFACES[f];

            Vector2 uv1 = new Vector2(face.Uv[0], face.Uv[3]);
            Vector2 uv2 = new Vector2(face.Uv[2], face.Uv[1]);

            Vector2 uvSize = uv2 - uv1;
            int rot = (int)(face.Rotation / 90);

            AddFace(meshData, facing, relativeCenter, size, uv1, uvSize, face.textureIndex, rot % 4, textureSizes);
        }
    }

    private void AddFace(VSMeshData modeldata, BlockFacing facing, Vector3 relativeCenter, Vector3 size, Vector2 uvStart, Vector2 uvSize, int faceTextureIndex, int uvRotation, Vector2[] textureSizes)
    {
        int coordPos = facing.index * 12; // 4 * 3 xyz's perface
        int uvPos = facing.index * 8;     // 4 * 2 uvs per face
        int lastVertexNumber = modeldata.vertices.Count;

        for (int i = 0; i < 4; i++)
        {
            int uvIndex = 2 * ((uvRotation + i) % 4) + uvPos;
            modeldata.vertices.Add(new Vector3(relativeCenter.x + size.x * CubeVertices[coordPos++] / 2,
                relativeCenter.y + size.y * CubeVertices[coordPos++] / 2,
                relativeCenter.z + size.z * CubeVertices[coordPos++] / 2));
            modeldata.uvs.Add(new Vector2(uvStart.x + uvSize.x * CubeUvCoords[uvIndex],
                uvStart.y + uvSize.y * CubeUvCoords[uvIndex + 1]) / (ShapeJSON.MaxTextureSize * textureSizes[faceTextureIndex]));
            modeldata.textureIndices.Add(faceTextureIndex);
        }

        // 2 triangles = 6 indices per face
        modeldata.indices.Add(lastVertexNumber + 0);
        modeldata.indices.Add(lastVertexNumber + 1);
        modeldata.indices.Add(lastVertexNumber + 2);
        modeldata.indices.Add(lastVertexNumber + 0);
        modeldata.indices.Add(lastVertexNumber + 2);
        modeldata.indices.Add(lastVertexNumber + 3);

    }

    public void ResolveAllMatricesForShape(ShapeJSON shape)
    {
        stackMatrix.Clear();
        stackMatrix.PushIdentity();
        ResolveMatricesForShapeElements(shape.Elements);
    }

    private void ResolveMatricesForShapeElements(ShapeElementJSON[] elements)
    {
        foreach (ShapeElementJSON element in elements)
        {
            stackMatrix.Push();
            if (element.RotationOrigin == null)
            {
                rotationOrigin = Vector3.zero;
            }
            else
            {
                rotationOrigin = new Vector3((float)element.RotationOrigin[0], (float)element.RotationOrigin[1], (float)element.RotationOrigin[2]);
                stackMatrix.Translate(rotationOrigin.x / 16, rotationOrigin.y / 16, rotationOrigin.z / 16);
            }

            stackMatrix.Rotate(element.RotationX, element.RotationY, element.RotationZ);
            stackMatrix.Scale(element.ScaleX, element.ScaleY, element.ScaleZ);
            stackMatrix.Translate((element.From[0] - rotationOrigin.x) / 16.0f, (element.From[1] - rotationOrigin.y) / 16.0f, (element.From[2] - rotationOrigin.z) / 16.0);

            //Clone the matrix for the element.
            element.cachedMatrix = stackMatrix.Top * Matrix4x4.identity;

            //Now do children.
            if (element.Children != null)
            {
                ResolveMatricesForShapeElements(element.Children);
            }
            stackMatrix.Pop();
        }
    }

}
