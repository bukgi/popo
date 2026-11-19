using System;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PMDSpriteAnimator : MonoBehaviour
    {
        private const float TickDurationSeconds = 1f / 60f;

        [SerializeField] public PMDCharacterData characterData;
        [SerializeField] public string currentAnimName = "Idle";
        [SerializeField] public PMDDirection currentDirection = PMDDirection.Down;
        [SerializeField] private bool loop = true;
        [SerializeField] private float playbackSpeed = 1f;

        public string currentAnimation
        {
            get => currentAnimName;
            set => Play(value, loop, true);
        }

        public void RefreshSequence(bool resetFrame = true)
        {
            Play(currentAnimName, loop, resetFrame);
        }

        private SpriteRenderer _spriteRenderer;
        private PMDAnimSequence _activeSequence;
        private int _currentFrameIndex;
        private float _frameTimer;
        private bool _isPlayingOneShot;
        private string _returnToAnim = "Idle";
        private Action _onHitFrameCallback;
        private bool _hitFrameTriggered;

        public PMDCharacterData CharacterData
        {
            get => characterData;
            set
            {
                characterData = value;
                Play(currentAnimName, true, true);
            }
        }

        public PMDDirection CurrentDirection
        {
            get => currentDirection;
            set
            {
                if (currentDirection != value)
                {
                    currentDirection = value;
                    RefreshSpriteFrame();
                }
            }
        }

        public string CurrentAnimName => currentAnimName;
        public bool IsPlayingOneShot => _isPlayingOneShot;
        public float PlaybackSpeed
        {
            get => playbackSpeed;
            set => playbackSpeed = Mathf.Max(0.1f, value);
        }

        private void EnsureLatestPikachuSprites()
        {
            if (characterData == null)
                return;

            if (characterData.runtimeVersion != OpenWorld.PMDRuntimeCharacterLoader.CurrentSpriteSheetVersion)
            {
                string folder = $"{characterData.pokemonId}_{characterData.pokemonName}";
                var fresh = OpenWorld.PMDRuntimeCharacterLoader.ReloadCharacterFromDisk(folder);
                if (fresh != null)
                {
                    characterData = fresh;
                    _activeSequence = null;
                }
            }
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureLatestPikachuSprites();
            if (characterData != null)
            {
                Play(currentAnimName, loop, true);
            }
        }

        private void OnEnable()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            EnsureLatestPikachuSprites();
            if (characterData != null && _activeSequence == null)
            {
                Play(currentAnimName, loop, true);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (characterData == null)
                return;

            EnsureLatestPikachuSprites();

            if (_activeSequence == null)
            {
                Play(currentAnimName, loop, true);
                if (_activeSequence == null)
                    return;
            }

            List<Sprite> frames = _activeSequence.GetFramesForDirection(currentDirection);
            if (frames == null || frames.Count == 0)
                return;

            int ticks = GetCurrentFrameDurationTicks();
            float frameDuration = Mathf.Max(1, ticks) * TickDurationSeconds;

            float dt = Time.deltaTime > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            _frameTimer += dt * Mathf.Max(0.01f, playbackSpeed);
            while (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _currentFrameIndex++;

                if (!_hitFrameTriggered && _activeSequence.hitFrame >= 0 && _currentFrameIndex >= _activeSequence.hitFrame)
                {
                    _hitFrameTriggered = true;
                    _onHitFrameCallback?.Invoke();
                }

                if (_currentFrameIndex >= frames.Count)
                {
                    if (_isPlayingOneShot)
                    {
                        if (!_hitFrameTriggered)
                        {
                            _hitFrameTriggered = true;
                            _onHitFrameCallback?.Invoke();
                        }
                        _isPlayingOneShot = false;
                        _onHitFrameCallback = null;
                        Play(_returnToAnim, true, true);
                        return;
                    }

                    if (loop)
                    {
                        _currentFrameIndex = 0;
                    }
                    else
                    {
                        _currentFrameIndex = frames.Count - 1;
                        break;
                    }
                }

                RefreshSpriteFrame();
                ticks = GetCurrentFrameDurationTicks();
                frameDuration = Mathf.Max(1, ticks) * TickDurationSeconds;
            }
        }

        public void Play(string animName, bool shouldLoop = true, bool forceRestart = false)
        {
            if (characterData == null)
                return;

            if (!forceRestart && !_isPlayingOneShot && string.Equals(currentAnimName, animName, StringComparison.OrdinalIgnoreCase) && _activeSequence != null)
                return;

            var seq = characterData.GetAnimation(animName) ?? characterData.GetAnimation("Idle");
            if (seq == null)
                return;

            currentAnimName = seq.name;
            _activeSequence = seq;
            loop = shouldLoop;
            _currentFrameIndex = 0;
            _frameTimer = 0f;
            _isPlayingOneShot = false;
            RefreshSpriteFrame();
        }

        public void PlayOneShot(string animName, string fallbackLoopAnim = "Idle", Action onHitFrame = null)
        {
            if (characterData == null)
            {
                onHitFrame?.Invoke();
                return;
            }

            var seq = characterData.GetAnimation(animName)
                      ?? characterData.GetAnimation("Attack")
                      ?? characterData.GetAnimation("Strike")
                      ?? characterData.GetAnimation("Charge")
                      ?? characterData.GetAnimation("Hop");

            if (seq == null)
            {
                onHitFrame?.Invoke();
                return;
            }

            currentAnimName = seq.name;
            _activeSequence = seq;
            loop = false;
            _currentFrameIndex = 0;
            _frameTimer = 0f;
            _isPlayingOneShot = true;
            _returnToAnim = fallbackLoopAnim;
            _onHitFrameCallback = onHitFrame;
            _hitFrameTriggered = false;
            RefreshSpriteFrame();
        }

        private int GetCurrentFrameDurationTicks()
        {
            if (_activeSequence == null || _activeSequence.durations == null || _activeSequence.durations.Count == 0)
                return 6;

            int idx = Mathf.Clamp(_currentFrameIndex, 0, _activeSequence.durations.Count - 1);
            return _activeSequence.durations[idx];
        }

        private void RefreshSpriteFrame()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_activeSequence == null)
                return;

            List<Sprite> frames = _activeSequence.GetFramesForDirection(currentDirection);
            if (frames == null || frames.Count == 0)
                return;

            int clamped = Mathf.Clamp(_currentFrameIndex, 0, frames.Count - 1);
            _spriteRenderer.sprite = frames[clamped];
        }
    }
}

