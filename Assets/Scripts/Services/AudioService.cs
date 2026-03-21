using System;
using System.Collections.Generic;
using Lumenfall.Data;
using UnityEngine;

namespace Lumenfall.Services
{
    public sealed class AudioService : ServiceBehaviour
    {
        private readonly List<AudioSource> _sfxPool = new();
        private AudioSource _musicSource;
        private AudioSource _ambientSource;

        protected override Type ServiceType => typeof(AudioService);

        protected override void Awake()
        {
            base.Awake();
            _musicSource = CreateChannel("Music");
            _musicSource.loop = true;
            _ambientSource = CreateChannel("Ambient");
            _ambientSource.loop = true;
            for (int index = 0; index < 8; index++)
            {
                _sfxPool.Add(CreateChannel($"Sfx_{index}"));
            }
        }

        public void ApplySettings(UserSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            _musicSource.volume = settings.musicVolume;
            _ambientSource.volume = settings.ambienceVolume;
            foreach (AudioSource sfxSource in _sfxPool)
            {
                sfxSource.volume = settings.sfxVolume;
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || _musicSource.clip == clip)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.Play();
        }

        public void SetAmbientClip(AudioClip clip)
        {
            if (clip == null)
            {
                _ambientSource.Stop();
                _ambientSource.clip = null;
                return;
            }

            if (_ambientSource.clip == clip)
            {
                return;
            }

            _ambientSource.clip = clip;
            _ambientSource.Play();
        }

        public void PlaySfx(AudioClip clip, Vector3 worldPosition)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = GetFreeSfxSource();
            source.transform.position = worldPosition;
            source.clip = clip;
            source.Play();
        }

        private AudioSource CreateChannel(string channelName)
        {
            GameObject child = new(channelName);
            child.transform.SetParent(transform, false);
            return child.AddComponent<AudioSource>();
        }

        private AudioSource GetFreeSfxSource()
        {
            foreach (AudioSource source in _sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            AudioSource overflowSource = CreateChannel($"Sfx_{_sfxPool.Count}");
            _sfxPool.Add(overflowSource);
            return overflowSource;
        }
    }
}
