// Owned by JunYoung Park
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField]
    private AudioSource ac;             //단일 조리환경이므로 수정필요

    [SerializeField]
    private AudioClip order;
    [SerializeField]
    private AudioClip slice;
    [SerializeField]
    private AudioClip grill;
   
    public void Order()
    {
        ac.PlayOneShot(order);
    }

    public void Slice()         //단일 조리환경이므로 수정필요
    {
        ac.PlayOneShot(slice);
    }

    public void GrillStart()    //단일 조리환경이므로 수정필요
    {
        ac.PlayOneShot(grill);
    }

    public void GrillEnd()
    {
        ac.Stop();
    }
}
