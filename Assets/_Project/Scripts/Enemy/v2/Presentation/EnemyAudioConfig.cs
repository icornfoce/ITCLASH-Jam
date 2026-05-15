using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// <summary>
    /// One audio cue slot — picks a random clip from the array on each play, with
    /// pitch + volume jitter to avoid repetitive sound.
    /// </summary>
    [Serializable]
    public class AudioCue
    {
        [Tooltip("Clips to choose from. Leave empty if this cue is unused on this enemy.")]
        public AudioClip[] clips;

        [Tooltip("Base playback volume.")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Random ± jitter applied to volume per play.")]
        [Range(0f, 0.5f)] public float volumeJitter = 0.05f;

        [Tooltip("Lower bound of randomised playback pitch.")]
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;

        [Tooltip("Upper bound of randomised playback pitch.")]
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;

        public void Play(AudioSource src)
        {
            if (src == null || clips == null || clips.Length == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null) return;

            src.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            float v = Mathf.Clamp01(volume + UnityEngine.Random.Range(-volumeJitter, volumeJitter));
            src.PlayOneShot(clip, v);
        }

        public void PlayAtPoint(Vector3 pos)
        {
            if (clips == null || clips.Length == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null) return;

            // Note: PlayClipAtPoint doesn't support pitch easily, 
            // but for a one-off death sound it's better than no sound.
            float v = Mathf.Clamp01(volume + UnityEngine.Random.Range(-volumeJitter, volumeJitter));
            AudioSource.PlayClipAtPoint(clip, pos, v);
        }
    }

    /// <summary>
    /// All audio slots for an enemy: one <see cref="AudioCue"/> per logical action.
    /// Auto-creates an AudioSource if one isn't assigned, configured for 3D playback.
    /// </summary>
    [Serializable]
    public class EnemyAudioConfig
    {
        [Tooltip("AudioSource for one-shot SFX. Auto-added if left empty.")]
        [SerializeField] AudioSource source;

        [Header("Cues")]
        public AudioCue spawn       = new AudioCue();
        public AudioCue footstep    = new AudioCue();
        public AudioCue attackSwing = new AudioCue();
        public AudioCue attackHit   = new AudioCue();
        public AudioCue dash        = new AudioCue();
        public AudioCue cast        = new AudioCue();
        public AudioCue heal        = new AudioCue();
        public AudioCue getHit      = new AudioCue();
        public AudioCue death       = new AudioCue();

        public AudioSource Source => source;

        public void Initialize(GameObject owner)
        {
            if (owner == null) return;
            if (source == null) source = owner.GetComponent<AudioSource>();
            if (source == null)
            {
                source = owner.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;          // 3D
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = 35f;
            }
        }

        public void PlaySpawn()       => spawn.Play(source);
        public void PlayFootstep()    => footstep.Play(source);
        public void PlayAttackSwing() => attackSwing.Play(source);
        public void PlayAttackHit()   => attackHit.Play(source);
        public void PlayDash()        => dash.Play(source);
        public void PlayCast()        => cast.Play(source);
        public void PlayHeal()        => heal.Play(source);
        public void PlayGetHit()      => getHit.Play(source);
        public void PlayDeath(Vector3? pos = null)
        {
            if (pos.HasValue) death.PlayAtPoint(pos.Value);
            else death.Play(source);
        }
    }
}
