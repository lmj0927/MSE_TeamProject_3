using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField]
    private AudioSource ac;

    [SerializeField]
    private AudioClip entrance;
   
    public void Entrance()
    {
        ac.PlayOneShot(entrance);
    }
}
