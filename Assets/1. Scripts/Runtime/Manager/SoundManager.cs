// Owned by JunYoung Park
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField]
    private AudioSource bgm;

    [Header("SFX")]
    [SerializeField]
    private AudioClip order;
    [SerializeField]
    private AudioClip slice;
    [SerializeField]
    private AudioClip grill;
    [SerializeField]
    private AudioClip fry;
    [SerializeField]
    private AudioClip drink;
    [SerializeField]
    private AudioClip trash;


    private List<AudioSource> pool = new List<AudioSource>();

    private AudioSource GetSource()
    {
        foreach (AudioSource source in pool)
        {
            if (!source.isPlaying)
            {
                source.DOKill();
                source.volume = 1f;
                return source;
            }
        }

        GameObject obj = new GameObject();
        obj.transform.SetParent(transform);

        AudioSource audio = obj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        pool.Add(audio);
        return audio;

    }
    public void Order()
    {
        GetSource().PlayOneShot(order);
    }

    public void Slice()         
    {
        GetSource().PlayOneShot(slice);
    }

    public void Trash()
    {
        GetSource().PlayOneShot(trash);
    }

    public void GrillStart(GrillCounter c) 
    {
        AudioSource audio = GetSource();

        audio.volume = 1.2f;
        audio.clip = grill;
        audio.loop = true;
        audio.Play();

        Action StopAudio = null;
        StopAudio = () =>
        {
            if (audio.clip == grill) SoundEnd(audio);
            c.OnCookFinished -= StopAudio;
        };

        c.OnCookFinished += StopAudio;
    }

    public void FryStart(FrierCounter c)
    {
        AudioSource audio = GetSource();

        audio.clip = fry;
        audio.loop = false;
        audio.Play();

        float duration = 0.5f;

        float delay = Mathf.Max(0, audio.clip.length - duration);
        audio.DOFade(0f, duration).SetDelay(delay).OnComplete(() =>
        {
            audio.Stop();
            audio.clip = null;
            audio.volume = 1f;
            c.FinishFry(true);
        });

        Action StopAudio = null;
        StopAudio = () =>
        {
            if (audio.clip == fry) SoundEnd(audio);
            c.OnCookFinished -= StopAudio;
        };

        c.OnCookFinished += StopAudio;
    }

    public void DrinkStart(DrinkCounter c)
    {
        AudioSource audio = GetSource();

        audio.clip = drink;
        audio.loop = true;
        audio.Play();

        Action StopAudio = null;
        StopAudio = () =>
        {
            if (audio.clip == drink) SoundEnd(audio, 0.15f);
            c.OnDrinkFinished -= StopAudio;
        };

        c.OnDrinkFinished += StopAudio;
    }

    private void SoundEnd(AudioSource audio, float duration = 0.25f)
    {
        audio.DOKill();

        audio.DOFade(0f, duration).OnComplete(() =>
        {
            audio.Stop();
            audio.loop = false;
            audio.clip = null;
            audio.volume = 1f;
        });

        
    }
}
