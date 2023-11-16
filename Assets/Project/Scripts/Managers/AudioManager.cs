using DG.Tweening;
using Project.Scripts.Enums;
using Project.Scripts.Essentials;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scripts.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private AudioVariables m_AudioVars => GameConfig.Instance.Audio;

        [SerializeField] private AudioSource   m_MusicSource;
        [SerializeField] private AudioSource   m_FrontSource;
        [SerializeField] private AudioSource[] m_EffectSources;

        private void OnEnable()
        {
            DOTween.To(() => m_MusicSource.volume, x => m_MusicSource.volume = x, m_AudioVars.AudioDictionary[eAudioType.Music].VolumeRange.x, 2.5f)
                   .SetDelay(1.25f)
                   .SetEase(Ease.InSine)
                   .From(0);
            PlayMainAudio(eAudioType.Music);

            DontDestroyOnLoad(gameObject);
        }

        private void PlayMainAudio(eAudioType audioType)
        {
            m_MusicSource.clip = m_AudioVars.AudioDictionary[audioType].Clip;
            m_MusicSource.Play();
        }

        public void PlayHitAudio(eAudioType audioType, float volumePercentage = 0)
        {
            var data = m_AudioVars.AudioDictionary[audioType];
            foreach (var source in m_EffectSources)
            {
                if (audioType == eAudioType.Hit && source.isPlaying) continue;

                source.clip   = data.Clip;
                source.pitch  = Random.Range(data.PitchRange.x, data.PitchRange.y);
                source.volume = Mathf.Lerp(data.VolumeRange.x, data.VolumeRange.y, volumePercentage);
                source.Play();

                break;
            }
        }

        public void PlayFrontAudio(eAudioType audioType)
        {
            var data = m_AudioVars.AudioDictionary[audioType];
            m_FrontSource.clip   = data.Clip;
            m_FrontSource.pitch  = Random.Range(data.PitchRange.x,  data.PitchRange.y);
            m_FrontSource.volume = Random.Range(data.VolumeRange.x, data.VolumeRange.y);
            m_FrontSource.Play();
        }
    }
}