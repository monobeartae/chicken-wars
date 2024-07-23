using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager instance = null;
    #endregion

    public GameObject BreakBoxSFX;

    private List<AudioSource> isPlayingList = new List<AudioSource>();

    void Start()
    {
        instance = this;
    }

    void Update()
    {
        foreach (AudioSource audio in isPlayingList)
        {
            if (!audio.isPlaying)
            {
                Destroy(audio.gameObject);
            }
        }
        isPlayingList.RemoveAll(s => s == null);
    }
    public void Play3DAudio(SFX_ID sound, Vector3 pos)
    {
        switch (sound)
        {
            case SFX_ID.BREAK_BOX:
                Instantiate(BreakBoxSFX, pos, Quaternion.identity);
                break;
        }
    }
}

public enum SFX_ID
{
    BREAK_BOX,

    NUM_TOTAL
}
