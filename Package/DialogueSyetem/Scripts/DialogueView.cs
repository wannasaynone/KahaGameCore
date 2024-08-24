using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueView : MonoBehaviour, IDialogueView
    {
        [SerializeField] private Transform leftCharacterTransform;
        [SerializeField] private Image leftCharacterImage;
        [SerializeField] private Transform rightCharacterTransform;
        [SerializeField] private Image rightCharacterImage;
        [SerializeField] private Transform centerCharacterTransform;
        [SerializeField] private Image centerCharacterImage;
        [SerializeField] private GameObject dialoguePanelRoot;
        [SerializeField] private GameObject cgDialoguePanelRoot;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI contentText_CG;
        [SerializeField] private TextMeshProUGUI nameText_CG;
        [SerializeField] private DialogueView_OptionButton optionButtonPrefab;
        [SerializeField] private Transform optionButtonParent;
        [SerializeField] private Image cgImage;
        [SerializeField] private Image lowerBlackScreen;
        [SerializeField] private Image higherBlackScreen;

        private int currentSelectOptionIndex = -1;

        private void OnEnable()
        {
            Clear();
            Input.InputEventHanlder.UserInterface.OnMoveToPreviousOptionInView += OnMoveToPreviousOptionInView;
            Input.InputEventHanlder.UserInterface.OnMoveToNextOptionInView += OnMoveToNextOptionInView;
            Input.InputEventHanlder.UserInterface.OnOptionInViewSelected += OnOptionInViewSelected;
            Input.InputEventHanlder.UserInterface.OnOptionInViewSelected += OnPressedSelectButton;
            Input.InputEventHanlder.Mouse.OnSingleTapped += OnPressedSelectButton;
        }

        private void OnDisable()
        {
            Clear();
            Input.InputEventHanlder.UserInterface.OnMoveToPreviousOptionInView -= OnMoveToPreviousOptionInView;
            Input.InputEventHanlder.UserInterface.OnMoveToNextOptionInView -= OnMoveToNextOptionInView;
            Input.InputEventHanlder.UserInterface.OnOptionInViewSelected -= OnOptionInViewSelected;
            Input.InputEventHanlder.UserInterface.OnOptionInViewSelected -= OnPressedSelectButton;
            Input.InputEventHanlder.Mouse.OnSingleTapped -= OnPressedSelectButton;
        }

        private void OnOptionInViewSelected()
        {
            if (currentSelectOptionIndex == -1)
            {
                // means no option to select
                return;
            }
            optionButtonParent.GetChild(currentSelectOptionIndex).GetComponent<DialogueView_OptionButton>().OnClicked();
        }

        private void OnMoveToNextOptionInView()
        {
            if (currentSelectOptionIndex == -1)
            {
                // means no option to select
                return;
            }

            currentSelectOptionIndex++;
            if (currentSelectOptionIndex >= optionButtonParent.childCount)
            {
                currentSelectOptionIndex = 0;
            }
            RefreshOptionButtonSelectState();
        }

        private void OnMoveToPreviousOptionInView()
        {
            if (currentSelectOptionIndex == -1)
            {
                // means no option to select
                return;
            }

            currentSelectOptionIndex--;
            if (currentSelectOptionIndex < 0)
            {
                currentSelectOptionIndex = optionButtonParent.childCount - 1;
            }
            RefreshOptionButtonSelectState();
        }

        private void RefreshOptionButtonSelectState()
        {
            for (int i = 0; i < optionButtonParent.childCount; i++)
            {
                DialogueView_OptionButton optionButton = optionButtonParent.GetChild(i).GetComponent<DialogueView_OptionButton>();
                optionButton.SetSelect(i == currentSelectOptionIndex);
            }
        }

        public void SetLeftCharacterImage(Sprite sprite, Action onCompleted = null)
        {
            leftCharacterImage.sprite = sprite;
            ShowCharacterImage(leftCharacterImage, leftCharacterTransform, Vector3.left * 300f, onCompleted);
        }

        public void SetLeftCharacterImage(string spriteName, Action onCompleted = null)
        {
            SetLeftCharacterImage(Resources.Load<Sprite>(spriteName), onCompleted);
        }

        public void SetCenterCharacterImage(Sprite sprite, Action onCompleted = null)
        {
            centerCharacterImage.sprite = sprite;
            ShowCharacterImage(centerCharacterImage, centerCharacterTransform, Vector3.zero, onCompleted);
        }

        public void SetCenterCharacterImage(string spriteName, Action onCompleted = null)
        {
            SetCenterCharacterImage(Resources.Load<Sprite>(spriteName), onCompleted);
        }

        private void ShowCharacterImage(Image image, Transform root, Vector3 add, Action onShown)
        {
            gameObject.SetActive(true);

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

        private void HideCharacterImage(Image image, Vector3 add)
        {
            Vector3 originPos = image.transform.localPosition;
            image.DOFade(0f, 0.5f);
            image.transform.DOLocalMove(originPos + add, 0.5f).OnComplete(() =>
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
            SetRightCharacterImage(Resources.Load<Sprite>($"{spriteName}"), onCompleted);
        }

        public void SetContentText(string text, Action onCompleted = null)
        {
            gameObject.SetActive(true);
            dialoguePanelRoot.SetActive(!cgImage.enabled);
            cgDialoguePanelRoot.SetActive(cgImage.enabled);
            contentText.text = "";
            contentText_CG.text = "";
            StartCoroutine(IEWaitInputCoroutine(text, onCompleted));
        }

        public void SetNameText(string text)
        {
            nameText.text = text;
            nameText_CG.text = text;
            if (string.IsNullOrEmpty(text))
            {
                nameText.transform.parent.gameObject.SetActive(false);
                nameText_CG.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                nameText.transform.parent.gameObject.SetActive(true);
                nameText_CG.transform.parent.gameObject.SetActive(true);
            }
        }

        public IDialogueOptionButton AddOptionButton()
        {
            DialogueView_OptionButton cloneButton = Instantiate(optionButtonPrefab, optionButtonParent);
            cloneButton.OnHovered += OnHoveredOptionButton;

            if (currentSelectOptionIndex == -1)
            {
                currentSelectOptionIndex = 0;
                RefreshOptionButtonSelectState();
            }

            return cloneButton;
        }

        private void OnHoveredOptionButton(DialogueView_OptionButton button)
        {
            for (int i = 0; i < optionButtonParent.childCount; i++)
            {
                if (optionButtonParent.GetChild(i).GetComponent<DialogueView_OptionButton>() == button)
                {
                    currentSelectOptionIndex = i;
                    RefreshOptionButtonSelectState();
                    break;
                }
            }
        }

        public bool IsWaitingSelection()
        {
            return optionButtonParent.childCount > 0;
        }

        private class ResumeData
        {
            public Sprite leftCharacterSprite;
            public Sprite rightCharacterSprite;
            public Sprite centerCharacterSprite;
            public Color leftCharacterColor;
            public Color rightCharacterColor;
            public Color centerCharacterColor;
            public bool leftCharacterImageEnabled;
            public bool rightCharacterImageEnabled;
            public bool centerCharacterImageEnabled;
            public Vector3 leftCharacterImagePosition;
            public Vector3 rightCharacterImagePosition;
            public Vector3 centerCharacterImagePosition;
        }

        private ResumeData resumeData;



        public void ShowCGImage(Sprite sprite, Action onCompleted = null)
        {
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

        private void OnLowerBlackScreenFadeInCompleted(Sprite sprite, Action onCompleted = null)
        {
            dialoguePanelRoot.SetActive(false);

            cgImage.sprite = sprite;
            cgImage.enabled = true;
            cgImage.color = new Color(1f, 1f, 1f, 0f);

            leftCharacterImage.enabled = false;
            rightCharacterImage.enabled = false;
            dialoguePanelRoot.SetActive(false);

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
            Sprite sprite = Resources.Load<Sprite>($"{spriteName}");
            ShowCGImage(sprite, onCompleted);
        }

        public void HideCGImage(Action onCompleted = null)
        {
            higherBlackScreen.enabled = true;
            higherBlackScreen.color = new Color(0f, 0f, 0f, 0f);
            higherBlackScreen.DOFade(1f, 0.5f).OnComplete(delegate
            {
                OnHigherBlackScreenFadeInCompleted(onCompleted);
            });
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
                centerCharacterSprite = centerCharacterImage.sprite,
                leftCharacterColor = leftCharacterImage.color,
                rightCharacterColor = rightCharacterImage.color,
                centerCharacterColor = centerCharacterImage.color,
                leftCharacterImageEnabled = leftCharacterImage.enabled,
                rightCharacterImageEnabled = rightCharacterImage.enabled,
                centerCharacterImageEnabled = centerCharacterImage.enabled,
                leftCharacterImagePosition = leftCharacterImage.transform.localPosition,
                rightCharacterImagePosition = rightCharacterImage.transform.localPosition,
                centerCharacterImagePosition = centerCharacterImage.transform.localPosition
            };
        }

        private void ResuemWithResumeData(ResumeData resumeData)
        {
            leftCharacterImage.sprite = resumeData.leftCharacterSprite;
            rightCharacterImage.sprite = resumeData.rightCharacterSprite;
            centerCharacterImage.sprite = resumeData.centerCharacterSprite;
            leftCharacterImage.color = resumeData.leftCharacterColor;
            rightCharacterImage.color = resumeData.rightCharacterColor;
            centerCharacterImage.color = resumeData.centerCharacterColor;
            leftCharacterImage.enabled = resumeData.leftCharacterImageEnabled;
            rightCharacterImage.enabled = resumeData.rightCharacterImageEnabled;
            centerCharacterImage.enabled = resumeData.centerCharacterImageEnabled;
            leftCharacterImage.transform.localPosition = resumeData.leftCharacterImagePosition;
            rightCharacterImage.transform.localPosition = resumeData.rightCharacterImagePosition;
            centerCharacterImage.transform.localPosition = resumeData.centerCharacterImagePosition;
        }

        public void ClearOptions()
        {
            foreach (Transform child in optionButtonParent)
            {
                child.GetComponent<DialogueView_OptionButton>().OnHovered -= OnHoveredOptionButton;
                Destroy(child.gameObject);
            }
            currentSelectOptionIndex = -1;
        }

        public void Clear()
        {
            leftCharacterImage.enabled = false;
            rightCharacterImage.enabled = false;
            centerCharacterImage.enabled = false;
            contentText.text = string.Empty;
            nameText.text = string.Empty;
            contentText_CG.text = string.Empty;
            nameText_CG.text = string.Empty;
            cgImage.enabled = false;
            lowerBlackScreen.enabled = false;
            higherBlackScreen.enabled = false;
            dialoguePanelRoot.SetActive(false);
            cgDialoguePanelRoot.SetActive(false);
            ClearOptions();
        }

        public void Hide(Action onCompleted = null)
        {
            if (gameObject.activeSelf) StartCoroutine(IEHide(onCompleted));
        }

        private System.Collections.IEnumerator IEHide(Action onCompleted = null)
        {
            HideCharacterImage(leftCharacterImage, Vector3.left * 300f);
            HideCharacterImage(rightCharacterImage, Vector3.right * 300f);
            dialoguePanelRoot.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            gameObject.SetActive(false);
            onCompleted?.Invoke();
        }

        private bool isSelectButtonPressed = false;
        private System.Collections.IEnumerator IEWaitInputCoroutine(string text, Action onInput)
        {
            isSelectButtonPressed = false;

            yield return null; // for skip one time input

            yield return StartCoroutine(IEType(text));

            isSelectButtonPressed = false;
            yield return null; // for skip one time input

            while (!isSelectButtonPressed)
            {
                yield return null;
            }

            onInput?.Invoke();
        }

        private void OnPressedSelectButton()
        {
            isSelectButtonPressed = true;
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

                    if (isSelectButtonPressed)
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
    }
}