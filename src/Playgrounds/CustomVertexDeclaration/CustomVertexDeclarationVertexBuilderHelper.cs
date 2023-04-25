using Microsoft.Xna.Framework;

namespace MonogameShaderPlayground.Primitives
{
    public static class CustomVertexDeclarationVertexBuilderHelper
    {
        public static MyCustomVertexPositionNormalTextureTangentBinormal[] ConstructVertexPositionNormalTextureTangentBinormalCube(Vector3 pos, float size)
        {
            var vertices = new MyCustomVertexPositionNormalTextureTangentBinormal[36];

            // Face Up
            vertices[0] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[1] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[2] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            vertices[3] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[4] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[5] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            // Face Front
            vertices[6] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[7] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[8] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));

            vertices[9] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[10] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            vertices[11] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));

            // Face Down
            vertices[12] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[13] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[14] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            vertices[15] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[16] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 1));
            vertices[17] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

            // Face Left
            vertices[18] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[19] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[20] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            vertices[21] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[22] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            vertices[23] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            // Face Back
            vertices[24] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[25] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[26] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));

            vertices[27] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[28] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
            vertices[29] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(0, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0));

            // Face Right
            vertices[30] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 1), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[31] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[32] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));

            vertices[33] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[34] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 1, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 1), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            vertices[35] = new MyCustomVertexPositionNormalTextureTangentBinormal(new Vector3(1, 0, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));

            return vertices;
        }
    }
}
