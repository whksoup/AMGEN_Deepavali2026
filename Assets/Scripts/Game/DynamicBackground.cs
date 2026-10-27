using UnityEngine;
using UnityEngine.UI;

public class DynamicBackground : MonoBehaviour
{
    [SerializeField] private Graphic uiImage;     // Image or RawImage
    [SerializeField] private float reseedEverySec = 10f;
    [SerializeField] private Vector2 speedRange = new Vector2(0.18f, 0.32f);
    [SerializeField] private Vector2 radiusRange = new Vector2(0.5f, 0.75f);

    private Material mat;
    private float timer;

    void Awake()
    {
        // Make a unique instance so we don't edit the shared material
        mat = Instantiate(uiImage.material);
        uiImage.material = mat;
        Reseed(true);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= reseedEverySec)
        {
            timer = 0f;
            Reseed(false);
        }
    }

    void Reseed(bool first)
    {
        // Change only motion feel, keep the same palette
        var seed = new Vector4(Random.value * 6f, Random.value * 6f, Random.value * 6f, Random.value * 6f);
        mat.SetVector("_Seed", seed);
        mat.SetFloat("_Speed", Random.Range(speedRange.x, speedRange.y));
        mat.SetFloat("_Radius", Random.Range(radiusRange.x, radiusRange.y));

        if (first) timer = 0f;
    }
}
