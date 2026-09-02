using NUnit.Framework;
using UnityEngine;

namespace FlowState.Rendering.Tests
{
    public sealed class PlayerComicBorderTests
    {
        [Test]
        public void Rebuild_CreatesExactlyFourComicPlatesPerMesh()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PlayerComicBorder border = target.AddComponent<PlayerComicBorder>();
                border.Rebuild();

                Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
                int plateCount = 0;
                foreach (Renderer renderer in renderers)
                {
                    Material material = renderer.sharedMaterial;
                    if (material != null && material.shader != null &&
                        material.shader.name == "FLOWSTATE/FSSRS/Player Comic Plate")
                    {
                        plateCount++;
                    }
                }

                Assert.That(Shader.Find("FLOWSTATE/FSSRS/Player Comic Plate"), Is.Not.Null);
                Assert.That(plateCount, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
