using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {

    [System.Serializable]
    public class AudioEvent
    {
        public string name;
        public bool isMusic = false;
        public AudioClip[] clips;
    }

    public AudioEvent[] events;
    public AudioSource audioSource;
    public AudioSource musicSource;

    static AudioManager m_instance;

    public static AudioManager Instance { get { return m_instance; } }

    void Awake()
    {
        m_instance = this;
    }

    public void Play(string eventName)
    {
        foreach (var evt in events)
        {
            if (evt.name == eventName)
            {
                if (evt.isMusic)
                {
                    if (musicSource.clip != evt.clips[0])
                    {
                        musicSource.clip = evt.clips[0];
                        musicSource.Play();
                    }
                }
                else
                {
                    audioSource.PlayOneShot(evt.clips[Random.Range(0, evt.clips.Length)]);
                }
            }
        }
    }
}
