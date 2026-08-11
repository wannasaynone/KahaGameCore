using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Foundation.Messaging.Editor
{
    public static class MessageTypeDiscovery
    {
        public static IReadOnlyList<Type> FindMessageTypes()
        {
            return TypeCache.GetTypesDerivedFrom<MessageBase>()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.ContainsGenericParameters &&
                    !IsTestAssembly(type.Assembly))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsTestAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            return name.EndsWith(".Tests", StringComparison.Ordinal) ||
                   name.Contains(".Tests.");
        }
    }

    public sealed class AutomaticMessageForm
    {
        private readonly ConstructorInfo[] constructors;
        private readonly Type messageType;
        private int constructorIndex;
        private ValueNode[] parameters;

        public AutomaticMessageForm(Type messageType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

            if (!typeof(MessageBase).IsAssignableFrom(messageType) ||
                messageType.IsAbstract ||
                messageType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"{messageType.FullName} is not a concrete MessageBase type.",
                    nameof(messageType));
            }

            this.messageType = messageType;
            constructors = messageType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(GetConstructorLabel, StringComparer.Ordinal)
                .ToArray();
            RebuildParameters();
        }

        public Type MessageType => messageType;

        public bool IsSupported => constructors.Length > 0 &&
                                   parameters.All(parameter => parameter.IsSupported);

        public string UnsupportedReason
        {
            get
            {
                if (constructors.Length == 0)
                {
                    return $"{messageType.FullName} has no public constructor.";
                }

                ValueNode unsupported = parameters.FirstOrDefault(parameter => !parameter.IsSupported);
                return unsupported?.UnsupportedReason;
            }
        }

        public void DrawParameters()
        {
            if (constructors.Length == 0)
            {
                EditorGUILayout.HelpBox(UnsupportedReason, UnityEditor.MessageType.Error);
                return;
            }

            if (constructors.Length > 1)
            {
                string[] labels = constructors.Select(GetConstructorLabel).ToArray();
                int selected = EditorGUILayout.Popup("Constructor", constructorIndex, labels);
                if (selected != constructorIndex)
                {
                    constructorIndex = selected;
                    RebuildParameters();
                }
            }

            foreach (ValueNode parameter in parameters)
            {
                parameter.Draw();
            }
        }

        public bool TryCreateMessage(out MessageBase message, out string error)
        {
            if (!IsSupported)
            {
                message = null;
                error = UnsupportedReason;
                return false;
            }

            try
            {
                object[] arguments = parameters.Select(parameter => parameter.CreateValue()).ToArray();
                message = (MessageBase)constructors[constructorIndex].Invoke(arguments);
                error = null;
                return true;
            }
            catch (TargetInvocationException exception)
            {
                message = null;
                error = exception.InnerException?.Message ?? exception.Message;
                return false;
            }
            catch (Exception exception)
            {
                message = null;
                error = exception.Message;
                return false;
            }
        }

        private void RebuildParameters()
        {
            if (constructors.Length == 0)
            {
                parameters = Array.Empty<ValueNode>();
                return;
            }

            parameters = constructors[constructorIndex]
                .GetParameters()
                .Select(parameter => ValueNode.Create(
                    parameter.ParameterType,
                    ObjectNames.NicifyVariableName(parameter.Name),
                    new HashSet<Type>()))
                .ToArray();
        }

        private static string GetConstructorLabel(ConstructorInfo constructor)
        {
            string parameters = string.Join(", ", constructor
                .GetParameters()
                .Select(parameter => parameter.ParameterType.Name + " " + parameter.Name));
            return $"{constructor.DeclaringType.Name}({parameters})";
        }
    }

    public static class AutomaticMessagePublisher
    {
        private static readonly MethodInfo PublishMethod = typeof(MessageBus)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(MessageBus.Publish) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 1);

        public static void Publish(MessageBase message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            PublishMethod.MakeGenericMethod(message.GetType())
                .Invoke(null, new object[] { message });
        }
    }

    public sealed class MessagePublisherEditor : EditorWindow
    {
        private readonly List<AutomaticMessageForm> forms =
            new List<AutomaticMessageForm>();
        private readonly Dictionary<Type, string> feedback =
            new Dictionary<Type, string>();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Message Publisher")]
        public static void ShowWindow()
        {
            GetWindow<MessagePublisherEditor>("Message Publisher");
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Publish Messages", EditorStyles.boldLabel);
            if (GUILayout.Button("Reload", GUILayout.Width(70f)))
            {
                Reload();
            }
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (AutomaticMessageForm form in forms)
            {
                DrawMessage(form);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawMessage(AutomaticMessageForm form)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(form.MessageType.Name, EditorStyles.boldLabel);
            form.DrawParameters();

            using (new EditorGUI.DisabledScope(!form.IsSupported))
            {
                if (GUILayout.Button("Publish " + form.MessageType.Name))
                {
                    Publish(form);
                }
            }

            if (!form.IsSupported)
            {
                EditorGUILayout.HelpBox(form.UnsupportedReason, MessageType.Error);
            }
            else if (feedback.TryGetValue(form.MessageType, out string message))
            {
                EditorGUILayout.HelpBox(
                    message,
                    message == "Published." ? MessageType.Info : MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void Publish(AutomaticMessageForm form)
        {
            if (!form.TryCreateMessage(out MessageBase message, out string error))
            {
                feedback[form.MessageType] = error;
                return;
            }

            try
            {
                AutomaticMessagePublisher.Publish(message);
                feedback[form.MessageType] = "Published.";
            }
            catch (TargetInvocationException exception)
            {
                feedback[form.MessageType] = exception.InnerException?.Message ?? exception.Message;
            }
            catch (Exception exception)
            {
                feedback[form.MessageType] = exception.Message;
            }
        }

        private void Reload()
        {
            forms.Clear();
            foreach (Type messageType in MessageTypeDiscovery.FindMessageTypes())
            {
                forms.Add(new AutomaticMessageForm(messageType));
            }
            feedback.Clear();
        }
    }

    internal abstract class ValueNode
    {
        protected ValueNode(Type valueType, string label)
        {
            ValueType = valueType;
            Label = label;
        }

        protected Type ValueType { get; }
        protected string Label { get; }
        public abstract bool IsSupported { get; }
        public virtual string UnsupportedReason => null;
        public abstract void Draw();
        public abstract object CreateValue();

        public static ValueNode Create(Type type, string label, HashSet<Type> ancestors)
        {
            if (type == typeof(string) || type == typeof(bool) || IsNumeric(type) || type.IsEnum ||
                type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Color) || typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return new ScalarValueNode(type, label);
            }

            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return new NullableValueNode(type, label, nullableType, ancestors);
            }

            if (type.IsInterface || type.IsAbstract || type.IsArray || type.ContainsGenericParameters)
            {
                return new UnsupportedValueNode(
                    type,
                    label,
                    $"{label} ({type.FullName}) cannot be constructed automatically.");
            }

            if (ancestors.Contains(type))
            {
                return new UnsupportedValueNode(
                    type,
                    label,
                    $"{label} contains a recursive {type.FullName} reference.");
            }

            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
            if (!type.IsValueType && constructor == null)
            {
                return new UnsupportedValueNode(
                    type,
                    label,
                    $"{label} ({type.FullName}) has no public parameterless constructor.");
            }

            HashSet<Type> nestedAncestors = new HashSet<Type>(ancestors) { type };
            return new ObjectValueNode(type, label, nestedAncestors);
        }

        private static bool IsNumeric(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double);
        }
    }

    internal sealed class ScalarValueNode : ValueNode
    {
        private object value;

        public ScalarValueNode(Type valueType, string label) : base(valueType, label)
        {
            value = valueType == typeof(string)
                ? string.Empty
                : valueType.IsValueType
                    ? Activator.CreateInstance(valueType)
                    : null;
        }

        public override bool IsSupported => true;

        public override void Draw()
        {
            if (ValueType == typeof(string))
            {
                value = EditorGUILayout.TextField(Label, (string)value);
            }
            else if (ValueType == typeof(bool))
            {
                value = EditorGUILayout.Toggle(Label, (bool)value);
            }
            else if (ValueType == typeof(float))
            {
                value = EditorGUILayout.FloatField(Label, (float)value);
            }
            else if (ValueType == typeof(double))
            {
                value = EditorGUILayout.DoubleField(Label, (double)value);
            }
            else if (ValueType == typeof(long) || ValueType == typeof(ulong))
            {
                long current = ValueType == typeof(long) ? (long)value : unchecked((long)(ulong)value);
                long edited = EditorGUILayout.LongField(Label, current);
                value = ValueType == typeof(long) ? edited : (ulong)Math.Max(0L, edited);
            }
            else if (ValueType == typeof(Vector2))
            {
                value = EditorGUILayout.Vector2Field(Label, (Vector2)value);
            }
            else if (ValueType == typeof(Vector3))
            {
                value = EditorGUILayout.Vector3Field(Label, (Vector3)value);
            }
            else if (ValueType == typeof(Vector4))
            {
                value = EditorGUILayout.Vector4Field(Label, (Vector4)value);
            }
            else if (ValueType == typeof(Color))
            {
                value = EditorGUILayout.ColorField(Label, (Color)value);
            }
            else if (ValueType.IsEnum)
            {
                value = EditorGUILayout.EnumPopup(Label, (Enum)value);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(ValueType))
            {
                value = EditorGUILayout.ObjectField(Label, (UnityEngine.Object)value, ValueType, false);
            }
            else
            {
                int edited = EditorGUILayout.IntField(Label, Convert.ToInt32(value));
                value = ConvertInteger(edited);
            }
        }

        public override object CreateValue()
        {
            return value;
        }

        private object ConvertInteger(int edited)
        {
            if (ValueType == typeof(byte))
            {
                return (byte)Mathf.Clamp(edited, byte.MinValue, byte.MaxValue);
            }
            if (ValueType == typeof(sbyte))
            {
                return (sbyte)Mathf.Clamp(edited, sbyte.MinValue, sbyte.MaxValue);
            }
            if (ValueType == typeof(short))
            {
                return (short)Mathf.Clamp(edited, short.MinValue, short.MaxValue);
            }
            if (ValueType == typeof(ushort))
            {
                return (ushort)Mathf.Clamp(edited, ushort.MinValue, ushort.MaxValue);
            }
            if (ValueType == typeof(uint))
            {
                return (uint)Math.Max(0, edited);
            }
            return edited;
        }
    }

    internal sealed class NullableValueNode : ValueNode
    {
        private readonly ValueNode value;
        private bool hasValue;

        public NullableValueNode(
            Type valueType,
            string label,
            Type underlyingType,
            HashSet<Type> ancestors) : base(valueType, label)
        {
            value = Create(underlyingType, label, ancestors);
        }

        public override bool IsSupported => value.IsSupported;
        public override string UnsupportedReason => value.UnsupportedReason;

        public override void Draw()
        {
            hasValue = EditorGUILayout.Toggle(Label + " Has Value", hasValue);
            if (hasValue)
            {
                value.Draw();
            }
        }

        public override object CreateValue()
        {
            return hasValue
                ? Activator.CreateInstance(ValueType, value.CreateValue())
                : null;
        }
    }

    internal sealed class ObjectValueNode : ValueNode
    {
        private readonly MemberValue[] members;
        private bool expanded = true;

        public ObjectValueNode(Type valueType, string label, HashSet<Type> ancestors)
            : base(valueType, label)
        {
            IEnumerable<MemberInfo> fields = valueType
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsInitOnly)
                .Cast<MemberInfo>();
            IEnumerable<MemberInfo> properties = valueType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property =>
                    property.GetIndexParameters().Length == 0 &&
                    property.GetSetMethod(true) != null)
                .Cast<MemberInfo>();

            members = fields
                .Concat(properties)
                .OrderBy(member => member.MetadataToken)
                .Select(member => new MemberValue(
                    member,
                    Create(GetMemberType(member), ObjectNames.NicifyVariableName(member.Name), ancestors)))
                .ToArray();
        }

        public override bool IsSupported => members.All(member => member.Value.IsSupported);

        public override string UnsupportedReason => members
            .Select(member => member.Value)
            .FirstOrDefault(value => !value.IsSupported)
            ?.UnsupportedReason;

        public override void Draw()
        {
            expanded = EditorGUILayout.Foldout(expanded, Label, true);
            if (!expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            foreach (MemberValue member in members)
            {
                member.Value.Draw();
            }
            EditorGUI.indentLevel--;
        }

        public override object CreateValue()
        {
            object instance = Activator.CreateInstance(ValueType);
            foreach (MemberValue member in members)
            {
                object memberValue = member.Value.CreateValue();
                if (member.Member is FieldInfo field)
                {
                    field.SetValue(instance, memberValue);
                }
                else
                {
                    ((PropertyInfo)member.Member).SetValue(instance, memberValue);
                }
            }
            return instance;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member is FieldInfo field
                ? field.FieldType
                : ((PropertyInfo)member).PropertyType;
        }

        private sealed class MemberValue
        {
            public MemberValue(MemberInfo member, ValueNode value)
            {
                Member = member;
                Value = value;
            }

            public MemberInfo Member { get; }
            public ValueNode Value { get; }
        }
    }

    internal sealed class UnsupportedValueNode : ValueNode
    {
        private readonly string reason;

        public UnsupportedValueNode(Type valueType, string label, string reason)
            : base(valueType, label)
        {
            this.reason = reason;
        }

        public override bool IsSupported => false;
        public override string UnsupportedReason => reason;

        public override void Draw()
        {
            EditorGUILayout.HelpBox(reason, MessageType.Error);
        }

        public override object CreateValue()
        {
            throw new InvalidOperationException(reason);
        }
    }
}
