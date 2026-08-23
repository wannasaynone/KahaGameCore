using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Dialogue.View;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace KahaGameCore.Dialogue.Tests
{
    public sealed class DialogueViewTextDisplayTests
    {
        [Test]
        public void SetDialogueText_DefaultRule_ShowsFullTextAndWaitsForAdvanceInput()
        {
            DialogueView view = CreateView(out TextMeshProUGUI dialogueText, out GameObject host);
            bool completed = false;
            view.OnDialogueTextCompleted += () => completed = true;

            try
            {
                view.SetDialogueText("完整文字");

                Assert.That(dialogueText.text, Is.EqualTo("完整文字"));
                Assert.That(completed, Is.False);

                InvokeAdvanceInput(view);

                Assert.That(completed, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SetDialogueText_CustomRule_ControlsVisibleTextAndCanBeCompletedByInput()
        {
            DialogueView view = CreateView(out TextMeshProUGUI dialogueText, out GameObject host);
            var displayRule = new DeferredTextDisplayRule();
            bool completed = false;
            view.OnDialogueTextCompleted += () => completed = true;
            view.SetDialogueTextDisplayRule(displayRule);

            try
            {
                view.SetDialogueText("完整文字");
                displayRule.SetVisibleText("部分");

                Assert.That(dialogueText.text, Is.EqualTo("部分"));
                Assert.That(completed, Is.False);

                InvokeAdvanceInput(view);

                Assert.That(displayRule.IsCancellationRequested, Is.True);
                Assert.That(dialogueText.text, Is.EqualTo("完整文字"));
                Assert.That(completed, Is.False);

                displayRule.SetVisibleText("過期更新");
                Assert.That(dialogueText.text, Is.EqualTo("完整文字"));

                InvokeAdvanceInput(view);
                Assert.That(completed, Is.True);
            }
            finally
            {
                displayRule.Finish();
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static DialogueView CreateView(
            out TextMeshProUGUI dialogueText,
            out GameObject host)
        {
            host = new GameObject("Dialogue View Test");
            host.SetActive(false);
            DialogueView view = host.AddComponent<DialogueView>();

            var textObject = new GameObject("Dialogue Text");
            textObject.transform.SetParent(host.transform);
            dialogueText = textObject.AddComponent<TextMeshProUGUI>();

            SetField(view, "dialogueText", dialogueText);
            SetField(view, "dialogueTextContainer", textObject);
            return view;
        }

        private static void SetField(DialogueView view, string fieldName, object value)
        {
            FieldInfo field = typeof(DialogueView).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(view, value);
        }

        private static void InvokeAdvanceInput(DialogueView view)
        {
            MethodInfo method = typeof(DialogueView).GetMethod(
                "OnInputDetected",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(view, null);
        }

        private sealed class DeferredTextDisplayRule : IDialogueTextDisplayRule
        {
            private readonly UniTaskCompletionSource completion = new UniTaskCompletionSource();
            private Action<string> setVisibleText;
            private CancellationToken cancellationToken;

            public bool IsCancellationRequested => cancellationToken.IsCancellationRequested;

            public UniTask DisplayAsync(
                string text,
                Action<string> setVisibleText,
                CancellationToken cancellationToken)
            {
                this.setVisibleText = setVisibleText;
                this.cancellationToken = cancellationToken;
                return completion.Task;
            }

            public void SetVisibleText(string text)
            {
                setVisibleText(text);
            }

            public void Finish()
            {
                completion.TrySetResult();
            }
        }
    }
}
