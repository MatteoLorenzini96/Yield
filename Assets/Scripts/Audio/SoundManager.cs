using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Music Settings")]
    public List<NamedAudioClip> mainThemes;
    [Range(0f, 1f)] public float musicVolume = 1f;
    public float musicFadeTime = 1f;
    public AudioSource musicSourceA; // Assegna in Inspector
    public AudioSource musicSourceB; // Assegna in Inspector
    private bool usingA = true;
    private Dictionary<string, AudioClip> musicDict;
    private Coroutine _currentCrossfade;

    [Header("SFX Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public float pitchVariance = 0.05f;
    public List<NamedAudioClip> sfxClips;
    private Dictionary<string, AudioClip> sfxDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Dizionari clip
            musicDict = new Dictionary<string, AudioClip>();
            foreach (var item in mainThemes)
                if (item.clip != null && !musicDict.ContainsKey(item.name))
                    musicDict.Add(item.name, item.clip);

            sfxDict = new Dictionary<string, AudioClip>();
            foreach (var item in sfxClips)
                if (item.clip != null && !sfxDict.ContainsKey(item.name))
                    sfxDict.Add(item.name, item.clip);

            // Assicuriamoci che le AudioSource musica non siano null
            if (musicSourceA != null) { musicSourceA.loop = true; musicSourceA.spatialBlend = 0f; }
            if (musicSourceB != null) { musicSourceB.loop = true; musicSourceB.spatialBlend = 0f; }

        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Music Functions
    public void PlayMusic(string clipName, float targetVolume = -1f, float fadeTime = -1f)
    {
        if (!musicDict.ContainsKey(clipName)) return;

        AudioClip clip = musicDict[clipName];
        if (targetVolume < 0) targetVolume = musicVolume;
        if (fadeTime < 0) fadeTime = musicFadeTime;

        AudioSource current = usingA ? musicSourceA : musicSourceB;
        AudioSource next = usingA ? musicSourceB : musicSourceA;

        if (current == null || next == null) return;

        next.clip = clip;
        next.volume = 0f;
        next.Play();

        // Stop della coroutine precedente se esiste
        if (_currentCrossfade != null)
            StopCoroutine(_currentCrossfade);

        _currentCrossfade = StartCoroutine(Crossfade(current, next, fadeTime, targetVolume));
        usingA = !usingA;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float duration, float targetVolume)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            from.volume = Mathf.Lerp(targetVolume, 0f, t);
            to.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        from.volume = 0f;
        to.volume = targetVolume;
        from.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (usingA)
        {
            musicSourceA.volume = musicVolume;
            musicSourceB.volume = 0f;
        }
        else
        {
            musicSourceB.volume = musicVolume;
            musicSourceA.volume = 0f;
        }
    }
    #endregion

    #region SFX Functions
    public void PlaySFX(string clipName, Transform parentTransform = null, float volume = 1f)
    {
        if (!sfxDict.ContainsKey(clipName)) return;

        AudioClip clip = sfxDict[clipName];
        if (clip == null) return;

        GameObject tempGO = new GameObject("SFX_" + clipName);
        if (parentTransform != null)
            tempGO.transform.parent = parentTransform;

        tempGO.transform.localPosition = Vector3.zero;
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.volume = Mathf.Clamp01(volume) * sfxVolume;
        aSource.spatialBlend = 1f; // 3D
        aSource.Play();

        Destroy(tempGO, clip.length);
    }

    public void PlaySFXWithPitch(string clipName, Transform parentTransform = null, float volume = 1f)
    {
        if (!sfxDict.ContainsKey(clipName)) return;

        AudioClip clip = sfxDict[clipName];
        if (clip == null) return;

        GameObject tempGO = new GameObject("SFX_" + clipName);
        if (parentTransform != null)
            tempGO.transform.parent = parentTransform;

        tempGO.transform.localPosition = Vector3.zero;
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        float randomPitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        aSource.pitch = randomPitch;
        aSource.clip = clip;
        aSource.volume = Mathf.Clamp01(volume) * sfxVolume;
        aSource.spatialBlend = 1f; // 3D
        aSource.Play();

        Destroy(tempGO, clip.length);
    }
    #endregion
}
