// Owned by JunYoung Park
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Header("BGM")]
    [SerializeField]
    private AudioSource bgm;
    [SerializeField]
    private AudioClip main;
    [SerializeField]
    private AudioClip lobby;
    [SerializeField]
    private AudioClip playing;

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

    [SerializeField]
    private AudioClip happy;
    [SerializeField]
    private AudioClip boring;
    [SerializeField]
    private AudioClip angry;


    private List<AudioSource> pool = new List<AudioSource>();

    private void Start()
    {
        ChangeBGM(0);
        GameManager.Instance.OnStageStart += () => ChangeBGM(2);
        GameManager.Instance.OnStageEnd += StopAllSFX;
        bgm.loop = true;  
    }

    // 0 is main
    // 1 is lobby
    // 2 is InGame
    // 3 is HurryUp(InGame pitch up)
    public void ChangeBGM(int index)
    {
        bgm.Stop();
        bgm.volume = 0f;
        Pitch(1f);
        
        switch (index)
        {
            case 0: Main(); break;
            case 1: Lobby(); break;
            case 2: InGame(); break;
            case 3: HurryUp(); break;
            default: break;
        }

    }

    private void Main()             // 메인~방선택 전 bgm
    {
        bgm.clip = main;
        BGMStart();
    }
    private void Lobby()            // 방 생성 + 팀원 모집중 bgm
    {
        bgm.clip = lobby;
        BGMStart();
    }

    private void InGame()
    {
        bgm.clip = playing;
        BGMStart(maxVol:0.8f);   
    }

    private void HurryUp()
    {
        if (bgm.clip != playing) InGame();

        Pitch(1.25f);
    }

    private void Pitch(float val, bool isAdd = false)
    {
        if (isAdd)
        {
            bgm.pitch += val;
        } else bgm.pitch = val;

    }

    private void BGMStart(float duration = 1.5f, float maxVol = 1f)
    {
        bgm.DOKill();

        bgm.Play();

        bgm.DOFade(maxVol, duration).OnComplete(() =>
        {
            bgm.volume = maxVol;
        });
    }

    private AudioSource GetSource()
    {
        foreach (AudioSource source in pool)
        {
            if (!source.isPlaying)
            {
                source.DOKill();
                source.volume = 1f;
                source.pitch = 1f;
                return source;
            }
        }

        GameObject obj = new GameObject("SFX_" + (pool.Count+1));
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
            if (audio.clip == grill) SFXEnd(audio);
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

            if (c != null) c.FinishFry(true);
        });

        Action StopAudio = null;
        StopAudio = () =>
        {
            if (audio.clip == fry) SFXEnd(audio);
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
            if (audio.clip == drink) SFXEnd(audio, 0.15f);
            c.OnDrinkFinished -= StopAudio;
        };

        c.OnDrinkFinished += StopAudio;
    }

    public void Happy(Customer c, float p) => PlayEmotion(c, happy, p);
    public void Boring(Customer c, float p) => PlayEmotion(c, boring, p);
    public void Angry(Customer c, float p) => PlayEmotion(c, angry, p);

    private void PlayEmotion(Customer c, AudioClip clip, float p)
    {
        AudioSource audio = GetSource();

        audio.pitch = p;
        audio.clip = clip;
        audio.loop = false;
        audio.Play();

        Action StopAudio = null;
        StopAudio = () =>
        {
            if (audio.clip == clip) SFXEnd(audio, 0f);

            c.OnEmotionChange -= StopAudio;
        };

        c.OnEmotionChange += StopAudio;

        DOVirtual.DelayedCall(clip.length / p, () =>
        {
            if (c != null)
            {
                c.OnEmotionChange -= StopAudio;
            }
        });
    }

    private void SFXEnd(AudioSource audio, float duration = 0.25f)
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

    private void StopAllSFX()
    {
        foreach (var source in pool)
        {
            source.DOKill();
            source.Stop();
            source.loop = false;
            source.clip = null;
            source.volume = 1f;
        }
    }
}
