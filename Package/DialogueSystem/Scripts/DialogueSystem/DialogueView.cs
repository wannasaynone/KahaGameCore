using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueView : MonoBehaviour, IDialogueView
    {
        public bool IsVisible => gameObject.activeSelf;

        [SerializeField] private Transform leftCharacterTransform;
        [SerializeField] private Image leftCharacterImage;
        [SerializeField] private Transform rightCharacterTransform;
        [SerializeField] private Image rightCharacterImage;
        [SerializeField] private GameObject dialoguePanelRoot;
        [SerializeField] private GameObject cgDialoguePanelRoot;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI contentText_CG;
        [SerializeField] private TextMeshProUGUI nameText_CG;
        [SerializeField] private DialogueView_OptionButton optionButtonPrefab;
        [SerializeField] private Transform optionButtonParent;
        [SerializeField] private Image cgImage;
        [SerializeField] private Image miniCGImage;
        [SerializeField] private Image lowerBlackScreen;
        [SerializeField] private Image higherBlackScreen;
        [SerializeField] private Image whiteScreen;
        [SerializeField] private AudioClip nextSentenceSound;

        private int currentSelectOptionIndex = -1;
        private int lastSelectOptionIndex = -1;

        public void ShakeCGImage(float strength, float time, Action onCompleted = null)
        {
            float timeGap = time / 5f;
            cgImage.transform.DOLocalMoveY(strength, timeGap).OnComplete(() =>
            {
                cgImage.transform.DOLocalMoveY(-strength, timeGap).OnComplete(() =>
                {
                    cgImage.transform.DOLocalMoveY(strength, timeGap).OnComplete(() =>
                    {
                        cgImage.transform.DOLocalMoveY(-strength, timeGap).OnComplete(() =>
                        {
                            cgImage.transform.DOLocalMoveY(0f, timeGap).OnComplete(() =>
                            {
                                onCompleted?.Invoke();
                            });
                        });
                    });
                });
            });
        }

        public void SetLeftCharacterImage(Sprite sprite, Action onCompleted = null)
        {
            leftCharacterImage.sprite = sprite;
            ShowCharacterImage(leftCharacterImage, leftCharacterTransform, Vector3.left * 300f, onCompleted);
        }

        public void SetLeftCharacterImage(string spriteName, Action onCompleted = null)
        {
            SetLeftCharacterImage(Resources.Load<Sprite>($"Character/{spriteName}"), onCompleted);
        }

        private void ShowUI()
        {
            if (gameObject.activeSelf)
                return;

            gameObject.SetActive(true);
            InputDetector.LockMovement(this);
        }

        private void ShowCharacterImage(Image image, Transform root, Vector3 add, Action onShown)
        {
            ShowUI();

            if (image.enabled)
            {
                onShown?.Invoke();
                return;
            }
            image.enabled = true;
            Vector3 curPos = root.localPosition;
            root.localPosition += add;
            root.localScale = new Vector3(0.9f, 0.9f, 1f);
            image.color = Color.gray;
            image.DOFade(1f, 0.5f);

            root.DOLocalMove(curPos, 0.5f).OnComplete(() => onShown?.Invoke());
        }

        private void HideCharacterImage(Image image, Vector3 add, float time)
        {
            Vector3 originPos = image.transform.localPosition;
            image.DOFade(0f, time);
            image.transform.DOLocalMove(originPos + add, time).OnComplete(() =>
            {
                image.enabled = false;
                image.transform.localPosition = originPos;
            });
        }

        public void SetRightCharacterImage(Sprite sprite, Action onCompleted = null)
        {
            rightCharacterImage.sprite = sprite;
            ShowCharacterImage(rightCharacterImage, rightCharacterTransform, Vector3.right * 300f, onCompleted);
        }

        public void SetRightCharacterImage(string spriteName, Action onCompleted = null)
        {
            SetRightCharacterImage(Resources.Load<Sprite>($"Character/{spriteName}"), onCompleted);
        }

        public void SetContentText(string text, Action onCompleted = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                onCompleted?.Invoke();
                return;
            }

            ShowUI();
            dialoguePanelRoot.SetActive(!cgImage.enabled);
            cgDialoguePanelRoot.SetActive(cgImage.enabled);
            contentText.text = "";
            contentText_CG.text = "";
            text = text.Replace("\uFE0F", "");
            StartCoroutine(IEWaitInputCoroutine(text, onCompleted));
        }

        public void SetNameText(string text)
        {
            nameText.text = text;
            nameText_CG.text = text;
        }

        public IDialogueOptionButton AddOptionButton()
        {
            DialogueView_OptionButton cloneButton = Instantiate(optionButtonPrefab, optionButtonParent);

            if (currentSelectOptionIndex == -1)
            {
                currentSelectOptionIndex = 0;
            }

            return cloneButton;
        }

        public bool IsWaitingSelection()
        {
            return optionButtonParent.childCount > 0;
        }

        private class ResumeData
        {
            public Sprite leftCharacterSprite;
            public Sprite rightCharacterSprite;
            public Color leftCharacterColor;
            public Color rightCharacterColor;
            public bool leftCharacterImageEnabled;
            public bool rightCharacterImageEnabled;
            public Vector3 leftCharacterImagePosition;
            public Vector3 rightCharacterImagePosition;
        }

        private ResumeData resumeData;



        public void ShowCGImage(Sprite sprite, Action onCompleted = null)
        {
            ShowUI();
            if (cgImage.enabled)
            {
                cgImage.sprite = sprite;
                onCompleted?.Invoke();
            }
            else
            {
                if (resumeData == null)
                    resumeData = CreateResumeData();

                lowerBlackScreen.enabled = true;
                lowerBlackScreen.color = new Color(0f, 0f, 0f, 0f);
                lowerBlackScreen.DOFade(1f, 0.5f).OnComplete(delegate
                {
                    OnLowerBlackScreenFadeInCompleted(sprite, onCompleted);
                });
            }
        }

        public void ShowSimplifiedCG(string spriteName, Action onCompleted = null)
        {
            ShowSimplifiedCG(Resources.Load<Sprite>($"CG/{spriteName}"), onCompleted);
        }

        public void ShowSimplifiedCG(Sprite sprite, Action onCompleted = null)
        {
            ShowUI();
            if (cgImage.enabled)
            {
                cgImage.sprite = sprite;
                onCompleted?.Invoke();
            }
            else
            {
                if (resumeData == null)
                    resumeData = CreateResumeData();

                SwitchLayoutToShowCGImage(sprite);
                cgImage.DOFade(1f, 0.5f).OnComplete(delegate
                {
                    onCompleted?.Invoke();
                });
            }
        }

        private void SwitchLayoutToShowCGImage(Sprite sprite)
        {
            cgImage.sprite = sprite;
            cgImage.enabled = true;
            cgImage.color = new Color(1f, 1f, 1f, 0f);

            leftCharacterImage.enabled = false;
            rightCharacterImage.enabled = false;
            dialoguePanelRoot.SetActive(false);
        }

        private void OnLowerBlackScreenFadeInCompleted(Sprite sprite, Action onCompleted = null)
        {
            dialoguePanelRoot.SetActive(false);

            SwitchLayoutToShowCGImage(sprite);

            lowerBlackScreen.DOFade(0.7f, 0.5f).OnComplete(delegate
            {
                cgImage.DOFade(1f, 0.5f).OnComplete(delegate
                {
                    onCompleted?.Invoke();
                });
            });
        }

        public void ShowCGImage(string spriteName, Action onCompleted = null)
        {
            Sprite sprite = Resources.Load<Sprite>($"CG/{spriteName}");
            ShowCGImage(sprite, onCompleted);
        }

        public void HideCGImage(Action onCompleted = null)
        {
            if (cgImage.enabled)
            {
                miniCGImage.enabled = false;
                higherBlackScreen.enabled = true;
                higherBlackScreen.color = new Color(0f, 0f, 0f, 0f);
                higherBlackScreen.DOFade(1f, 0.5f).OnComplete(delegate
                {
                    OnHigherBlackScreenFadeInCompleted(onCompleted);
                });
            }
            else if (miniCGImage.enabled)
            {
                miniCGImage.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    miniCGImage.enabled = false;
                    onCompleted?.Invoke();
                });
            }
        }

        public void ShowMiniCGImage(string sprite, Action onCompleted = null)
        {
            miniCGImage.sprite = Resources.Load<Sprite>($"CG/{sprite}");
            miniCGImage.enabled = true;
            miniCGImage.color = new Color(1f, 1f, 1f, 0f);
            miniCGImage.DOFade(1f, 0.5f).OnComplete(() => onCompleted?.Invoke());
        }


        private void OnHigherBlackScreenFadeInCompleted(Action onCompleted = null)
        {
            higherBlackScreen.DOFade(1f, 0.25f).OnComplete(() =>
            {
                if (resumeData != null)
                {
                    ResuemWithResumeData(resumeData);
                    resumeData = null;
                }

                cgDialoguePanelRoot.SetActive(false);
                cgImage.enabled = false;
                lowerBlackScreen.enabled = false;

                higherBlackScreen.DOFade(0f, 0.25f).OnComplete(() =>
                {
                    higherBlackScreen.enabled = false;
                    onCompleted?.Invoke();
                });
            });
        }

        private ResumeData CreateResumeData()
        {
            return new ResumeData
            {
                leftCharacterSprite = leftCharacterImage.sprite,
                rightCharacterSprite = rightCharacterImage.sprite,
                leftCharacterColor = leftCharacterImage.color,
                rightCharacterColor = rightCharacterImage.color,
                leftCharacterImageEnabled = leftCharacterImage.enabled,
                rightCharacterImageEnabled = rightCharacterImage.enabled,
                leftCharacterImagePosition = leftCharacterImage.transform.localPosition,
                rightCharacterImagePosition = rightCharacterImage.transform.localPosition
            };
        }

        private void ResuemWithResumeData(ResumeData resumeData)
        {
            leftCharacterImage.sprite = resumeData.leftCharacterSprite;
            rightCharacterImage.sprite = resumeData.rightCharacterSprite;
            leftCharacterImage.color = resumeData.leftCharacterColor;
            rightCharacterImage.color = resumeData.rightCharacterColor;
            leftCharacterImage.enabled = resumeData.leftCharacterImageEnabled;
            rightCharacterImage.enabled = resumeData.rightCharacterImageEnabled;
            leftCharacterImage.transform.localPosition = resumeData.leftCharacterImagePosition;
            rightCharacterImage.transform.localPosition = resumeData.rightCharacterImagePosition;
        }

        public void ClearOptions()
        {
            foreach (Transform child in optionButtonParent)
            {
                Destroy(child.gameObject);
            }
            currentSelectOptionIndex = -1;
            lastSelectOptionIndex = -1;
        }

        public void Clear()
        {
            leftCharacterImage.enabled = false;
            rightCharacterImage.enabled = false;
            contentText.text = string.Empty;
            // nameText.text = string.Empty;
            contentText_CG.text = string.Empty;
            nameText_CG.text = string.Empty;
            cgImage.enabled = false;
            miniCGImage.enabled = false;
            lowerBlackScreen.enabled = false;
            higherBlackScreen.enabled = false;
            dialoguePanelRoot.SetActive(false);
            cgDialoguePanelRoot.SetActive(false);
            ClearOptions();
        }

        public void Hide(float fadeOutTime, Action onCompleted = null)
        {
            if (gameObject.activeSelf) StartCoroutine(IEHide(fadeOutTime, onCompleted));
            else onCompleted?.Invoke();
        }

        private System.Collections.IEnumerator IEHide(float fadeOutTime, Action onCompleted = null)
        {
            HideCharacterImage(leftCharacterImage, Vector3.left * 300f, fadeOutTime);
            HideCharacterImage(rightCharacterImage, Vector3.right * 300f, fadeOutTime);
            dialoguePanelRoot.SetActive(false);

            yield return new WaitForSeconds(fadeOutTime);

            gameObject.SetActive(false);
            InputDetector.UnlockMovement(this);
            onCompleted?.Invoke();
        }

        private void Update()
        {
            if (currentSelectOptionIndex != -1)
            {
                if (InputDetector.IsMovingToPreviousOptionInView())
                {
                    currentSelectOptionIndex--;
                    if (currentSelectOptionIndex < 0)
                    {
                        currentSelectOptionIndex = optionButtonParent.childCount - 1;
                    }
                }
                else if (InputDetector.IsMoviongToNextOptionInView())
                {
                    currentSelectOptionIndex++;
                    if (currentSelectOptionIndex >= optionButtonParent.childCount)
                    {
                        currentSelectOptionIndex = 0;
                    }
                }
                else if (InputDetector.IsSelectingInView())
                {
                    optionButtonParent.GetChild(currentSelectOptionIndex).GetComponent<DialogueView_OptionButton>().OnClicked();
                }
            }

            if (currentSelectOptionIndex != -1 && lastSelectOptionIndex != currentSelectOptionIndex)
            {
                for (int i = 0; i < optionButtonParent.childCount; i++)
                {
                    DialogueView_OptionButton optionButton = optionButtonParent.GetChild(i).GetComponent<DialogueView_OptionButton>();
                    optionButton.SetSelect(i == currentSelectOptionIndex);
                }
                lastSelectOptionIndex = currentSelectOptionIndex;
            }
        }

        private System.Collections.IEnumerator IEWaitInputCoroutine(string text, Action onInput)
        {
            yield return null; // for skip one time input

            yield return StartCoroutine(IEType(text));

            yield return null; // for skip one time input

            while (!InputDetector.IsSelectingInView())
            {
                yield return null;
            }

            if (nextSentenceSound != null) SoundManager.Instance.PlaySFX(nextSentenceSound);
            onInput?.Invoke();
        }

        private System.Collections.IEnumerator IEType(string text)
        {
            contentText.text = string.Empty;
            contentText_CG.text = string.Empty;
            float timer = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                contentText.text += text[i];
                contentText_CG.text += text[i];
                while (timer < 0.1f)
                {
                    timer += Time.deltaTime;

                    if (InputDetector.IsSelectingInView())
                    {
                        contentText.text = text;
                        contentText_CG.text = text;
                        yield break;
                    }

                    yield return null;
                }

                timer = 0f;
            }
        }

        private void OnEnable()
        {
            Clear();
        }

        private void OnDisable()
        {
            Clear();
        }

        public void HighlightLeftCharacterImage(Action onCompleted = null)
        {
            HighlightCharacterImage(leftCharacterImage, leftCharacterTransform, onCompleted);
        }

        public void HighlightRightCharacterImage(Action onCompleted = null)
        {
            HighlightCharacterImage(rightCharacterImage, rightCharacterTransform, onCompleted);
        }

        private void HighlightCharacterImage(Image image, Transform root, Action onCompleted)
        {
            root.DOScale(1f, 0.5f);
            image.DOColor(new Color(1f, 1f, 1f, 1f), 0.5f).OnComplete(() => onCompleted?.Invoke());
        }

        public void HighlightAllCharacterImage(Action onCompleted = null)
        {
            HighlightCharacterImage(leftCharacterImage, leftCharacterTransform, null);
            HighlightCharacterImage(rightCharacterImage, rightCharacterTransform, onCompleted);
        }

        public void DehighlightLeftCharacterImage(Action onCompleted = null)
        {
            DehighlightCharacterImage(leftCharacterImage, leftCharacterTransform, onCompleted);
        }

        public void DehighlightRightCharacterImage(Action onCompleted = null)
        {
            DehighlightCharacterImage(rightCharacterImage, rightCharacterTransform, onCompleted);
        }

        private void DehighlightCharacterImage(Image image, Transform root, Action onCompleted)
        {
            root.DOScale(0.9f, 0.5f);
            image.DOColor(Color.gray, 0.5f).OnComplete(() => onCompleted?.Invoke());
        }

        public void DehighlightAllCharacterImage(Action onCompleted = null)
        {
            DehighlightCharacterImage(leftCharacterImage, leftCharacterTransform, null);
            DehighlightCharacterImage(rightCharacterImage, rightCharacterTransform, onCompleted);
        }

        public void Flash(float inTime, float stay, float outTime, Action onCompleted = null)
        {
            whiteScreen.enabled = true;
            whiteScreen.color = new Color(1f, 1f, 1f, 0f);
            whiteScreen.DOFade(1f, inTime).OnComplete(() =>
            {
                KahaGameCore.Common.TimerManager.Schedule(stay, () =>
                {
                    whiteScreen.DOFade(0f, outTime).OnComplete(() =>
                    {
                        whiteScreen.enabled = false;
                        onCompleted?.Invoke();
                    });
                });
            });
        }
        public void Transition(float inTime, float stay, float outTime, Action onCompleted = null)
        {
            whiteScreen.enabled = true;
            whiteScreen.color = new Color(0f, 0f, 0f, 0f);
            whiteScreen.DOFade(1f, inTime).OnComplete(() =>
            {
                KahaGameCore.Common.TimerManager.Schedule(stay, () =>
                {
                    whiteScreen.DOFade(0f, outTime).OnComplete(() =>
                    {
                        whiteScreen.enabled = false;
                        onCompleted?.Invoke();
                    });
                });
            });
        }
    }
}