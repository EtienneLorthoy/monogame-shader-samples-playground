using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameShaderPlayground.Helpers
{
    /// <summary>
    /// This class is used to generate vertices for a spheres or cubes. It provides basic primitives to allow 
    /// each playgrounds to focus on the rendering code. In a real 3D app, you would probably use 3D models or something.
    /// NOTE: This class provide Vertices for Monogame builtin IVertexType ONLY (like VertexPositionColorNormal). If a playground
    /// requires a custom IVertexType, it will have its own VerticesBuilderHelper class for its custom IVertexType.
    /// See CustomVertexDeclarationPlayground for an example of a playground that requires a custom IVertexType.
    /// </summary>
    public static class VertexsBuilderHelper
    {
        public static Tuple<VertexPositionColorNormal[], int[]> GenerateSphereVertices(float diameter, int tessellation)
        {
            var vertices = new List<VertexPositionColorNormal>();
            var AddVertex = new Action<Vector3, Vector3>(delegate (Vector3 position, Vector3 normal)
            {
                vertices.Add(new VertexPositionColorNormal(position, Color.White, normal));
            });
            
            if (tessellation < 3)
                throw new ArgumentOutOfRangeException("tessellation");

            int verticalSegments = tessellation;
            int horizontalSegments = tessellation * 2;

            float radius = diameter / 2;

            // Start with a single vertex at the bottom of the sphere.
            AddVertex(Vector3.Down * radius, Vector3.Down);

            // Create rings of vertices at progressively higher latitudes.
            for (int i = 0; i < verticalSegments - 1; i++)
            {
                float latitude = ((i + 1) * MathHelper.Pi / verticalSegments) - MathHelper.PiOver2;

                float dy = (float)Math.Sin(latitude);
                float dxz = (float)Math.Cos(latitude);

                // Create a single ring of vertices at this latitude.
                for (int j = 0; j < horizontalSegments; j++)
                {
                    float longitude = j * MathHelper.TwoPi / horizontalSegments;

                    float dx = (float)Math.Cos(longitude) * dxz;
                    float dz = (float)Math.Sin(longitude) * dxz;

                    Vector3 normal = new Vector3(dx, dy, dz);

                    AddVertex(normal * radius, normal);
                }
            }

            // Finish with a single vertex at the top of the sphere.
            AddVertex(Vector3.Up * radius, Vector3.Up);

            // Create Indexes
            var indices = new List<int>();
            var AddIndex = new Action<int>(index => indices.Add(index));

            // Create a fan connecting the bottom vertex to the bottom latitude ring.
            for (int i = 0; i < horizontalSegments; i++)
            {
                AddIndex(0);
                AddIndex(1 + (i + 1) % horizontalSegments);
                AddIndex(1 + i);
            }

            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int i = 0; i < verticalSegments - 2; i++)
            {
                for (int j = 0; j < horizontalSegments; j++)
                {
                    int nextI = i + 1;
                    int nextJ = (j + 1) % horizontalSegments;

                    AddIndex(1 + i * horizontalSegments + j);
                    AddIndex(1 + i * horizontalSegments + nextJ);
                    AddIndex(1 + nextI * horizontalSegments + j);

                    AddIndex(1 + i * horizontalSegments + nextJ);
                    AddIndex(1 + nextI * horizontalSegments + nextJ);
                    AddIndex(1 + nextI * horizontalSegments + j);
                }
            }

            // Create a fan connecting the top vertex to the top latitude ring.
            // for (int i = 0; i < horizontalSegments; i++)
            // {
            //     AddIndex(CurrentVertex - 1);
            //     AddIndex(CurrentVertex - 2 - (i + 1) % horizontalSegments);
            //     AddIndex(CurrentVertex - 2 - i);
            // }

            return new Tuple<VertexPositionColorNormal[], int[]>(vertices.ToArray(), indices.ToArray());
        }

        public static VertexPositionColorNormal[] ConstructVertexPositionColorNormalCube(Vector3 pos, float size)
        {
            var vertices = new VertexPositionColorNormal[36];

            // Face Up
            vertices[0] = new VertexPositionColorNormal(new Vector3(0, 1, 0) * size + pos, Color.White, new Vector3(0, 1, 0));
            vertices[1] = new VertexPositionColorNormal(new Vector3(1, 1, 0) * size + pos, Color.White, new Vector3(0, 1, 0));
            vertices[2] = new VertexPositionColorNormal(new Vector3(0, 1, 1) * size + pos, Color.White, new Vector3(0, 1, 0));

            vertices[3] = new VertexPositionColorNormal(new Vector3(0, 1, 1) * size + pos, Color.White, new Vector3(0, 1, 0));
            vertices[4] = new VertexPositionColorNormal(new Vector3(1, 1, 0) * size + pos, Color.White, new Vector3(0, 1, 0));
            vertices[5] = new VertexPositionColorNormal(new Vector3(1, 1, 1) * size + pos, Color.White, new Vector3(0, 1, 0));

            // Face Front
            vertices[6] = new VertexPositionColorNormal(new Vector3(0, 1, 1) * size + pos, Color.White, new Vector3(0, 0, 1));
            vertices[7] = new VertexPositionColorNormal(new Vector3(1, 1, 1) * size + pos, Color.White, new Vector3(0, 0, 1));
            vertices[8] = new VertexPositionColorNormal(new Vector3(0, 0, 1) * size + pos, Color.White, new Vector3(0, 0, 1));

            vertices[9] = new VertexPositionColorNormal(new Vector3(0, 0, 1) * size + pos, Color.White, new Vector3(0, 0, 1));
            vertices[10] = new VertexPositionColorNormal(new Vector3(1, 1, 1) * size + pos, Color.White, new Vector3(0, 0, 1));
            vertices[11] = new VertexPositionColorNormal(new Vector3(1, 0, 1) * size + pos, Color.White, new Vector3(0, 0, 1));

            // Face Down
            vertices[12] = new VertexPositionColorNormal(new Vector3(1, 0, 1) * size + pos, Color.White, new Vector3(0, -1, 0));
            vertices[13] = new VertexPositionColorNormal(new Vector3(1, 0, 0) * size + pos, Color.White, new Vector3(0, -1, 0));
            vertices[14] = new VertexPositionColorNormal(new Vector3(0, 0, 1) * size + pos, Color.White, new Vector3(0, -1, 0));

            vertices[15] = new VertexPositionColorNormal(new Vector3(0, 0, 1) * size + pos, Color.White, new Vector3(0, -1, 0));
            vertices[16] = new VertexPositionColorNormal(new Vector3(1, 0, 0) * size + pos, Color.White, new Vector3(0, -1, 0));
            vertices[17] = new VertexPositionColorNormal(new Vector3(0, 0, 0) * size + pos, Color.White, new Vector3(0, -1, 0));

            // Face Back
            vertices[18] = new VertexPositionColorNormal(new Vector3(1, 0, 0) * size + pos, Color.White, new Vector3(0, 0, -1));
            vertices[19] = new VertexPositionColorNormal(new Vector3(1, 1, 0) * size + pos, Color.White, new Vector3(0, 0, -1));
            vertices[20] = new VertexPositionColorNormal(new Vector3(0, 0, 0) * size + pos, Color.White, new Vector3(0, 0, -1));

            vertices[21] = new VertexPositionColorNormal(new Vector3(0, 0, 0) * size + pos, Color.White, new Vector3(0, 0, -1));
            vertices[22] = new VertexPositionColorNormal(new Vector3(1, 1, 0) * size + pos, Color.White, new Vector3(0, 0, -1));
            vertices[23] = new VertexPositionColorNormal(new Vector3(0, 1, 0) * size + pos, Color.White, new Vector3(0, 0, -1));

            // Face Left
            vertices[24] = new VertexPositionColorNormal(new Vector3(0, 1, 0) * size + pos, Color.White, new Vector3(-1, 0, 0));
            vertices[25] = new VertexPositionColorNormal(new Vector3(0, 1, 1) * size + pos, Color.White, new Vector3(-1, 0, 0));
            vertices[26] = new VertexPositionColorNormal(new Vector3(0, 0, 0) * size + pos, Color.White, new Vector3(-1, 0, 0));

            vertices[27] = new VertexPositionColorNormal(new Vector3(0, 0, 0) * size + pos, Color.White, new Vector3(-1, 0, 0));
            vertices[28] = new VertexPositionColorNormal(new Vector3(0, 1, 1) * size + pos, Color.White, new Vector3(-1, 0, 0));
            vertices[29] = new VertexPositionColorNormal(new Vector3(0, 0, 1) * size + pos, Color.White, new Vector3(-1, 0, 0));

            // Face Right
            vertices[30] = new VertexPositionColorNormal(new Vector3(1, 1, 1) * size + pos, Color.White, new Vector3(1, 0, 0));
            vertices[31] = new VertexPositionColorNormal(new Vector3(1, 1, 0) * size + pos, Color.White, new Vector3(1, 0, 0));
            vertices[32] = new VertexPositionColorNormal(new Vector3(1, 0, 1) * size + pos, Color.White, new Vector3(1, 0, 0));

            vertices[33] = new VertexPositionColorNormal(new Vector3(1, 0, 1) * size + pos, Color.White, new Vector3(1, 0, 0));
            vertices[34] = new VertexPositionColorNormal(new Vector3(1, 1, 0) * size + pos, Color.White, new Vector3(1, 0, 0));
            vertices[35] = new VertexPositionColorNormal(new Vector3(1, 0, 0) * size + pos, Color.White, new Vector3(1, 0, 0));

            return vertices;
        }

        public static VertexPosition[] ConstructVertexPositionCube(Vector3 pos, float size)
        {
            // Vertices
            var vertices = new VertexPosition[36];

            // Face Up
            vertices[0] = new VertexPosition(new Vector3(0, 1, 0) * size + pos);
            vertices[1] = new VertexPosition(new Vector3(1, 1, 0) * size + pos);
            vertices[2] = new VertexPosition(new Vector3(0, 1, 1) * size + pos);

            vertices[3] = new VertexPosition(new Vector3(0, 1, 1) * size + pos);
            vertices[4] = new VertexPosition(new Vector3(1, 1, 0) * size + pos);
            vertices[5] = new VertexPosition(new Vector3(1, 1, 1) * size + pos);

            // Face Front
            vertices[6] = new VertexPosition(new Vector3(0, 1, 1) * size + pos);
            vertices[7] = new VertexPosition(new Vector3(1, 1, 1) * size + pos);
            vertices[8] = new VertexPosition(new Vector3(0, 0, 1) * size + pos);

            vertices[9] = new VertexPosition(new Vector3(0, 0, 1) * size + pos);
            vertices[10] = new VertexPosition(new Vector3(1, 1, 1) * size + pos);
            vertices[11] = new VertexPosition(new Vector3(1, 0, 1) * size + pos);

            // Face Down
            vertices[12] = new VertexPosition(new Vector3(1, 0, 1) * size + pos);
            vertices[13] = new VertexPosition(new Vector3(1, 0, 0) * size + pos);
            vertices[14] = new VertexPosition(new Vector3(0, 0, 1) * size + pos);

            vertices[15] = new VertexPosition(new Vector3(0, 0, 1) * size + pos);
            vertices[16] = new VertexPosition(new Vector3(1, 0, 0) * size + pos);
            vertices[17] = new VertexPosition(new Vector3(0, 0, 0) * size + pos);

            // Face Left
            vertices[18] = new VertexPosition(new Vector3(0, 1, 0) * size + pos);
            vertices[19] = new VertexPosition(new Vector3(0, 1, 1) * size + pos);
            vertices[20] = new VertexPosition(new Vector3(0, 0, 0) * size + pos);

            vertices[21] = new VertexPosition(new Vector3(0, 0, 0) * size + pos);
            vertices[22] = new VertexPosition(new Vector3(0, 1, 1) * size + pos);
            vertices[23] = new VertexPosition(new Vector3(0, 0, 1) * size + pos);

            // Face Back
            vertices[24] = new VertexPosition(new Vector3(1, 1, 0) * size + pos);
            vertices[25] = new VertexPosition(new Vector3(0, 1, 0) * size + pos);
            vertices[26] = new VertexPosition(new Vector3(1, 0, 0) * size + pos);

            vertices[27] = new VertexPosition(new Vector3(1, 0, 0) * size + pos);
            vertices[28] = new VertexPosition(new Vector3(0, 1, 0) * size + pos);
            vertices[29] = new VertexPosition(new Vector3(0, 0, 0) * size + pos);

            // Face Right
            vertices[30] = new VertexPosition(new Vector3(1, 1, 1) * size + pos);
            vertices[31] = new VertexPosition(new Vector3(1, 1, 0) * size + pos);
            vertices[32] = new VertexPosition(new Vector3(1, 0, 1) * size + pos);

            vertices[33] = new VertexPosition(new Vector3(1, 0, 1) * size + pos);
            vertices[34] = new VertexPosition(new Vector3(1, 1, 0) * size + pos);
            vertices[35] = new VertexPosition(new Vector3(1, 0, 0) * size + pos);

            return vertices;
        }
    
        public static VertexPositionTexture[] ConstructVertexPositionTextureCube(Vector3 pos, float size)
        {
            var vertices = new VertexPositionTexture[36];

            // Face Up
            vertices[0] = new VertexPositionTexture(new Vector3(0, 1, 0) * size + pos, new Vector2(0, 0));
            vertices[1] = new VertexPositionTexture(new Vector3(1, 1, 0) * size + pos, new Vector2(1, 0));
            vertices[2] = new VertexPositionTexture(new Vector3(0, 1, 1) * size + pos, new Vector2(0, 1));

            vertices[3] = new VertexPositionTexture(new Vector3(0, 1, 1) * size + pos, new Vector2(0, 1));
            vertices[4] = new VertexPositionTexture(new Vector3(1, 1, 0) * size + pos, new Vector2(1, 0));
            vertices[5] = new VertexPositionTexture(new Vector3(1, 1, 1) * size + pos, new Vector2(1, 1));

            // Face Front
            vertices[6] = new VertexPositionTexture(new Vector3(0, 1, 1) * size + pos, new Vector2(0, 0));
            vertices[7] = new VertexPositionTexture(new Vector3(1, 1, 1) * size + pos, new Vector2(1, 0));
            vertices[8] = new VertexPositionTexture(new Vector3(0, 0, 1) * size + pos, new Vector2(0, 1));

            vertices[9] = new VertexPositionTexture(new Vector3(0, 0, 1) * size + pos, new Vector2(0, 1));
            vertices[10] = new VertexPositionTexture(new Vector3(1, 1, 1) * size + pos, new Vector2(1, 0));
            vertices[11] = new VertexPositionTexture(new Vector3(1, 0, 1) * size + pos, new Vector2(1, 1));

            // Face Down
            vertices[12] = new VertexPositionTexture(new Vector3(1, 0, 1) * size + pos, new Vector2(0, 0));
            vertices[13] = new VertexPositionTexture(new Vector3(1, 0, 0) * size + pos, new Vector2(1, 0));
            vertices[14] = new VertexPositionTexture(new Vector3(0, 0, 1) * size + pos, new Vector2(0, 1));

            vertices[15] = new VertexPositionTexture(new Vector3(0, 0, 1) * size + pos, new Vector2(0, 1));
            vertices[16] = new VertexPositionTexture(new Vector3(1, 0, 0) * size + pos, new Vector2(1, 0));
            vertices[17] = new VertexPositionTexture(new Vector3(0, 0, 0) * size + pos, new Vector2(1, 1));

            // Face Left
            vertices[18] = new VertexPositionTexture(new Vector3(0, 1, 0) * size + pos, new Vector2(0, 0));
            vertices[19] = new VertexPositionTexture(new Vector3(0, 1, 1) * size + pos, new Vector2(1, 0));
            vertices[20] = new VertexPositionTexture(new Vector3(0, 0, 0) * size + pos, new Vector2(0, 1));

            vertices[21] = new VertexPositionTexture(new Vector3(0, 0, 0) * size + pos, new Vector2(0, 1));
            vertices[22] = new VertexPositionTexture(new Vector3(0, 1, 1) * size + pos, new Vector2(1, 0));
            vertices[23] = new VertexPositionTexture(new Vector3(0, 0, 1) * size + pos, new Vector2(1, 1));

            // Face Back
            vertices[24] = new VertexPositionTexture(new Vector3(1, 1, 0) * size + pos, new Vector2(0, 0));
            vertices[25] = new VertexPositionTexture(new Vector3(0, 1, 0) * size + pos, new Vector2(1, 0));
            vertices[26] = new VertexPositionTexture(new Vector3(1, 0, 0) * size + pos, new Vector2(0, 1));

            vertices[27] = new VertexPositionTexture(new Vector3(1, 0, 0) * size + pos, new Vector2(0, 1));
            vertices[28] = new VertexPositionTexture(new Vector3(0, 1, 0) * size + pos, new Vector2(1, 0));
            vertices[29] = new VertexPositionTexture(new Vector3(0, 0, 0) * size + pos, new Vector2(1, 1));

            // Face Right
            vertices[30] = new VertexPositionTexture(new Vector3(1, 1, 1) * size + pos, new Vector2(0, 0));
            vertices[31] = new VertexPositionTexture(new Vector3(1, 1, 0) * size + pos, new Vector2(1, 0));
            vertices[32] = new VertexPositionTexture(new Vector3(1, 0, 1) * size + pos, new Vector2(0, 1));

            vertices[33] = new VertexPositionTexture(new Vector3(1, 0, 1) * size + pos, new Vector2(0, 1));
            vertices[34] = new VertexPositionTexture(new Vector3(1, 1, 0) * size + pos, new Vector2(1, 0));
            vertices[35] = new VertexPositionTexture(new Vector3(1, 0, 0) * size + pos, new Vector2(1, 1));

            return vertices;
        }
    
        public static VertexPositionNormalTexture[] ConstructVertexPositionNormalTextureCube(Vector3 pos, float size)
        {
            var vertices = new VertexPositionNormalTexture[36];

            // Face Up
            vertices[0] = new VertexPositionNormalTexture(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 0));
            vertices[1] = new VertexPositionNormalTexture(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 0));
            vertices[2] = new VertexPositionNormalTexture(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 1));

            vertices[3] = new VertexPositionNormalTexture(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(0, 1));
            vertices[4] = new VertexPositionNormalTexture(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 0));
            vertices[5] = new VertexPositionNormalTexture(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 1, 0), new Vector2(1, 1));

            // Face Front
            vertices[6] = new VertexPositionNormalTexture(new Vector3(0, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 0));
            vertices[7] = new VertexPositionNormalTexture(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 0));
            vertices[8] = new VertexPositionNormalTexture(new Vector3(0, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 1));

            vertices[9] = new VertexPositionNormalTexture(new Vector3(0, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(0, 1));
            vertices[10] = new VertexPositionNormalTexture(new Vector3(1, 1, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 0));
            vertices[11] = new VertexPositionNormalTexture(new Vector3(1, 0, 1) * size + pos, new Vector3(0, 0, 1), new Vector2(1, 1));

            // Face Down
            vertices[12] = new VertexPositionNormalTexture(new Vector3(1, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 0));
            vertices[13] = new VertexPositionNormalTexture(new Vector3(1, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 0));
            vertices[14] = new VertexPositionNormalTexture(new Vector3(0, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 1));

            vertices[15] = new VertexPositionNormalTexture(new Vector3(0, 0, 1) * size + pos, new Vector3(0, -1, 0), new Vector2(0, 1));
            vertices[16] = new VertexPositionNormalTexture(new Vector3(1, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 0));
            vertices[17] = new VertexPositionNormalTexture(new Vector3(0, 0, 0) * size + pos, new Vector3(0, -1, 0), new Vector2(1, 1));

            // Face Back
            vertices[18] = new VertexPositionNormalTexture(new Vector3(1, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 0));
            vertices[19] = new VertexPositionNormalTexture(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 0));
            vertices[20] = new VertexPositionNormalTexture(new Vector3(0, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 1));

            vertices[21] = new VertexPositionNormalTexture(new Vector3(0, 0, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(0, 1));
            vertices[22] = new VertexPositionNormalTexture(new Vector3(1, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 0));
            vertices[23] = new VertexPositionNormalTexture(new Vector3(0, 1, 0) * size + pos, new Vector3(0, 0, -1), new Vector2(1, 1));

            // Face Left
            vertices[24] = new VertexPositionNormalTexture(new Vector3(0, 1, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 0));
            vertices[25] = new VertexPositionNormalTexture(new Vector3(0, 1, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 0));
            vertices[26] = new VertexPositionNormalTexture(new Vector3(0, 0, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 1));

            vertices[27] = new VertexPositionNormalTexture(new Vector3(0, 0, 0) * size + pos, new Vector3(-1, 0, 0), new Vector2(0, 1));
            vertices[28] = new VertexPositionNormalTexture(new Vector3(0, 1, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 0));
            vertices[29] = new VertexPositionNormalTexture(new Vector3(0, 0, 1) * size + pos, new Vector3(-1, 0, 0), new Vector2(1, 1));

            // Face Right
            vertices[30] = new VertexPositionNormalTexture(new Vector3(1, 1, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 0));
            vertices[31] = new VertexPositionNormalTexture(new Vector3(1, 1, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 0));
            vertices[32] = new VertexPositionNormalTexture(new Vector3(1, 0, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 1));

            vertices[33] = new VertexPositionNormalTexture(new Vector3(1, 0, 1) * size + pos, new Vector3(1, 0, 0), new Vector2(0, 1));
            vertices[34] = new VertexPositionNormalTexture(new Vector3(1, 1, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 0));
            vertices[35] = new VertexPositionNormalTexture(new Vector3(1, 0, 0) * size + pos, new Vector3(1, 0, 0), new Vector2(1, 1));

            return vertices;
        }
    }
}