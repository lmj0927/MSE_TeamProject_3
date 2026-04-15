using Unity.VisualScripting;
using UnityEngine;

public class Customer : MonoBehaviour
{
    [SerializeField]
    private GameObject hat;
    [SerializeField]
    private GameObject body;
    [SerializeField]
    private Texture2D[] faces;


    void Start()
    {
        RandomColor();
        int ran = Random.Range(0, 2);
        SetHat(ran);
        SetFace(0);
    }

    // Update is called once per frame
    void Update()
    {
     
    }

    void RandomColor()
    {
        Color randomC = new Color(Random.value, Random.value, Random.value);
        Renderer rd = body.GetComponent<Renderer>();

        rd.materials[0].SetColor("_BaseColor", randomC);
    }

    // 0 is nothing
    // 1 is fedora
    // 2 is party hat
    void SetHat(int type)
    {
        int max = hat.transform.childCount;

        for (int i = 0; i < max; i++)
        {
            hat.transform.GetChild(i).gameObject.SetActive(i == (type-1));

        }
    }

    // 0 is happy (default)
    // 1 is uncomfortable
    // 2 is angry
    void SetFace(int type)
    {
        type = Mathf.Abs(type);
        int idx = faces.Length > type ? type : 0;
        
        Renderer rd = body.GetComponent<Renderer>();


        rd.materials[1].SetTexture("_BaseMap", faces[idx]);
    }

    void ApplyRotation(float degree)
    {
        transform.Rotate(0,degree,0);
    }
}
