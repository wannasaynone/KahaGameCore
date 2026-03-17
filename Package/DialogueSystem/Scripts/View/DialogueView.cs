using Febucci.UI;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.View
{
    public class DialogueView : MonoBehaviour
    {
        private enum State
        {
            None,
            Typing,
            TypeCompleted,
            WaitingForOption
        }

        public event System.Action OnDialogueTextCompleted;

        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private GameObject speakerNameContainer;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject dialogueTextContainer;
        [SerializeField] private TypewriterByCharacter typeEffect;
        [SerializeField] private CanvasGroup blackoutOverlay;
        [SerializeField] private Transform optionContainer;
        [SerializeField] private DialogueView_OptionButton optionButtonPrefab;
        [SerializeField] private CGDisplayer cgDisplayer;

        [Header("Character Slots")]
        [SerializeField] private CharacterDisplayer leftCharacterImage;
        [SerializeField] private CharacterDisplayer middleCharacterImage;
        [SerializeField] private CharacterDisplayer rightCharacterImage;

        private State state = State.None;
        private CancellationTokenSource cts;
        private readonly List<DialogueView_OptionButton> spawnedOptionButtons = new List<DialogueView_OptionButton>();


        private void OnDisable()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            speakerNameContainer.SetActive(false);
            dialogueTextContainer.SetActive(false);
            speakerNameText.text = string.Empty;
            dialogueText.text = string.Empty;

            if (blackoutOverlay != null)
            {
                blackoutOverlay.alpha = 0f;
                blackoutOverlay.gameObject.SetActive(false);
            }

            HideOptions();
            state = State.None;
        }

        public void HideDialogueBox()
        {
            speakerNameContainer.SetActive(false);
            dialogueTextContainer.SetActive(false);
            speakerNameText.text = string.Empty;
            dialogueText.text = string.Empty;
        }

        public void SetSpeakerName(string name)
        {
            speakerNameText.text = name;
            if (string.IsNullOrEmpty(name))
            {
                speakerNameContainer.SetActive(false);
            }
            else
            {
                speakerNameContainer.SetActive(true);
            }
        }

        public void SetDialogueText(string text)
        {
            if (state != State.None)
            {
                Debug.LogError("Dialogue is already playing. Cannot set new dialogue text. new text: " + text);
                return;
            }

            state = State.Typing;
            dialogueText.text = string.Empty;

            if (!dialogueTextContainer.activeSelf)
            {
                dialogueTextContainer.SetActive(true);
            }

            PlayTypeEffect(text);
        }

        private async void PlayTypeEffect(string text)
        {
            await Task.Yield();
            typeEffect.ShowText(text);
        }

        public void TextAnimator_OnTypingCompleted()
        {
            state = State.TypeCompleted;
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                OnInputDetected();
            }
        }

        private void OnInputDetected()
        {
            switch (state)
            {
                case State.Typing:
                    typeEffect.SkipTypewriter();
                    break;
                case State.TypeCompleted:
                    state = State.None;
                    OnDialogueTextCompleted?.Invoke();
                    break;
            }
        }

        public async Task BlackIn(float fadeTime)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            await BlackIn(fadeTime, cts.Token);
        }

        private async Task BlackIn(float fadeTime, CancellationToken token)
        {
            if (blackoutOverlay == null)
            {
                Debug.LogError("BlackoutOverlay is not assigned in DialogueView!");
                return;
            }

            if (fadeTime <= 0)
            {
                Debug.LogError("BlackIn fadeTime must be greater than 0. Setting to 0.");
                fadeTime = 0;
            }

            blackoutOverlay.gameObject.SetActive(true);
            blackoutOverlay.alpha = 0f;

            if (fadeTime == 0)
            {
                // Instant fade in
                blackoutOverlay.alpha = 1f;
            }
            else
            {
                float fadeSpeed = 1f / fadeTime;
                while (blackoutOverlay.alpha < 1f - Time.deltaTime * fadeSpeed)
                {
                    blackoutOverlay.alpha += Time.deltaTime * fadeSpeed;
                    await UniTask.Yield(token);
                }
                blackoutOverlay.alpha = 1f;
            }
        }

        public async Task BlackOut(float fadeTime)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            await BlackOut(fadeTime, cts.Token);
        }

        private async Task BlackOut(float fadeTime, CancellationToken token)
        {
            if (blackoutOverlay == null)
            {
                Debug.LogError("BlackoutOverlay is not assigned in DialogueView!");
                return;
            }

            if (fadeTime <= 0)
            {
                Debug.LogError("BlackOut fadeTime must be greater than 0. Setting to 0.");
                fadeTime = 0;
            }

            blackoutOverlay.alpha = 1f;

            if (fadeTime == 0)
            {
                // Instant fade out
                blackoutOverlay.alpha = 0f;
            }
            else
            {
                float fadeSpeed = 1f / fadeTime;
                while (blackoutOverlay.alpha > Time.deltaTime * fadeSpeed)
                {
                    blackoutOverlay.alpha -= Time.deltaTime * fadeSpeed;
                    await UniTask.Yield(token);
                }
                blackoutOverlay.alpha = 0f;
            }

            blackoutOverlay.gameObject.SetActive(false);
        }

        public void ShowOptions(List<OptionData> options, System.Action<OptionData> onOptionSelected)
        {
            if (optionContainer == null || optionButtonPrefab == null)
            {
                Debug.LogError("[DialogueView] optionContainer or optionButtonPrefab is not assigned!");
                return;
            }

            state = State.WaitingForOption;
            optionContainer.gameObject.SetActive(true);

            foreach (OptionData option in options)
            {
                DialogueView_OptionButton button = Instantiate(optionButtonPrefab, optionContainer);
                button.gameObject.SetActive(true);

                OptionData capturedOption = option;

                button.Bind(capturedOption, (selectedDialogueId) =>
                {
                    HideOptions();
                    state = State.None;
                    onOptionSelected?.Invoke(capturedOption);
                });

                spawnedOptionButtons.Add(button);
            }
        }

        public void HideOptions()
        {
            foreach (DialogueView_OptionButton button in spawnedOptionButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            spawnedOptionButtons.Clear();

            if (optionContainer != null)
            {
                optionContainer.gameObject.SetActive(false);
            }
        }

        public bool HasCG(string cgName)
        {
            return cgDisplayer != null && cgDisplayer.HasCG(cgName);
        }

        public async UniTask ShowCG(Texture2D texture, string cgName, float fadeInTime)
        {
            if (cgDisplayer == null)
            {
                Debug.LogError("[DialogueView] cgDisplayer is not assigned!");
                return;
            }

            await cgDisplayer.ShowCG(texture, cgName, fadeInTime);
        }

        public async UniTask HideCG(string cgName, float fadeOutTime)
        {
            if (cgDisplayer == null)
            {
                Debug.LogError("[DialogueView] cgDisplayer is not assigned!");
                return;
            }

            await cgDisplayer.HideCG(cgName, fadeOutTime);
        }

        public async UniTask ShowCharacter(string slotName, Texture2D texture, float offsetX, float fadeInTime)
        {
            CharacterDisplayer slot = GetCharacterSlot(slotName);
            if (slot == null)
            {
                Debug.LogError($"[DialogueView] Character slot '{slotName}' is not assigned or invalid!");
                return;
            }

            slot.SetTexture(texture);
            slot.SetPositionOffsetX(offsetX);

            await slot.FadeIn(fadeInTime);
        }

        public async UniTask HideCharacter(string slotName, float fadeOutTime)
        {
            CharacterDisplayer slot = GetCharacterSlot(slotName);
            if (slot == null)
            {
                Debug.LogError($"[DialogueView] Character slot '{slotName}' is not assigned or invalid!");
                return;
            }

            await slot.FadeOut(fadeOutTime);

            slot.ResetToDefault();
        }

        public async UniTask MoveCharacterX(string slotName, float addX, float moveTime)
        {
            CharacterDisplayer slot = GetCharacterSlot(slotName);
            if (slot == null)
            {
                Debug.LogError($"[DialogueView] Character slot '{slotName}' is not assigned or invalid!");
                return;
            }

            await slot.MoveX(addX, moveTime);
        }

        public async UniTask MoveCharacterY(string slotName, float addY, float moveTime)
        {
            CharacterDisplayer slot = GetCharacterSlot(slotName);
            if (slot == null)
            {
                Debug.LogError($"[DialogueView] Character slot '{slotName}' is not assigned or invalid!");
                return;
            }

            await slot.MoveY(addY, moveTime);
        }

        public async UniTask CharacterJump(string slotName, float totalTime)
        {
            CharacterDisplayer slot = GetCharacterSlot(slotName);
            if (slot == null)
            {
                Debug.LogError($"[DialogueView] Character slot '{slotName}' is not assigned or invalid!");
                return;
            }

            await slot.Jump(totalTime);
        }

        public async UniTask ScaleCharacter(string slotName, float targetScale, float scaleTime)
        {
            CharacterDisplayer slot = GetCharacterSlot(slotName);
            if (slot == null)
            {
                Debug.LogError($"[DialogueView] Character slot '{slotName}' is not assigned or invalid!");
                return;
            }

            await slot.ScaleTo(targetScale, scaleTime);
        }

        private CharacterDisplayer GetCharacterSlot(string slotName)
        {
            switch (slotName)
            {
                case "Left":
                    return leftCharacterImage;
                case "Middle":
                    return middleCharacterImage;
                case "Right":
                    return rightCharacterImage;
                default:
                    Debug.LogError($"[DialogueView] Unknown character slot: {slotName}");
                    return null;
            }
        }
    }
}