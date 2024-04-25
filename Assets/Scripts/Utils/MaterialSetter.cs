using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialSetter : MonoBehaviour
{
    private MeshRenderer _meshRenderer;
    
    public MeshRenderer MeshRenderer
    {
        get
        {
            if (!_meshRenderer)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }

            return _meshRenderer;
        }
    }

    public void SetSingleMaterial(Material material)
    {
        MeshRenderer.material = material;
    }

}
