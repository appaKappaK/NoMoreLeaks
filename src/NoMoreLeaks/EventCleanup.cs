using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NoMoreLeaks
{
    internal static class EventCleanup
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static void RemoveGameEvent(object eventSource, object owner, string methodName)
        {
            if (eventSource == null || owner == null) return;

            MethodInfo handlerMethod = FindInstanceMethod(owner.GetType(), methodName);
            if (handlerMethod == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing handler " + owner.GetType().FullName + "." + methodName);
                return;
            }

            MethodInfo removeMethod = FindSingleDelegateParameterMethod(eventSource.GetType(), "Remove");
            if (removeMethod == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing Remove(delegate) on " + eventSource.GetType().FullName);
                return;
            }

            Type delegateType = removeMethod.GetParameters()[0].ParameterType;
            Delegate handler = Delegate.CreateDelegate(delegateType, owner, handlerMethod, false);
            if (handler == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Could not bind " + owner.GetType().FullName + "." + methodName + " as " + delegateType.FullName);
                return;
            }

            removeMethod.Invoke(eventSource, new object[] { handler });
        }

        internal static void RemoveInstanceEventField(object source, string fieldName, object owner, string methodName)
        {
            if (source == null) return;

            FieldInfo field = AccessTools.Field(source.GetType(), fieldName);
            if (field == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing field " + source.GetType().FullName + "." + fieldName);
                return;
            }

            RemoveGameEvent(field.GetValue(source), owner, methodName);
        }

        internal static void RemoveStaticDelegateField(Type type, string fieldName, object owner, string methodName)
        {
            if (type == null || owner == null) return;

            FieldInfo field = type.GetField(fieldName, AnyStatic);
            if (field == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing static delegate field " + type.FullName + "." + fieldName);
                return;
            }

            Delegate current = field.GetValue(null) as Delegate;
            if (current == null) return;

            MethodInfo handlerMethod = FindInstanceMethod(owner.GetType(), methodName);
            if (handlerMethod == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing handler " + owner.GetType().FullName + "." + methodName);
                return;
            }

            Delegate handler = Delegate.CreateDelegate(field.FieldType, owner, handlerMethod, false);
            if (handler == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Could not bind " + owner.GetType().FullName + "." + methodName + " as " + field.FieldType.FullName);
                return;
            }

            field.SetValue(null, Delegate.Remove(current, handler));
        }

        internal static void RemoveDelegatesOwnedBy(object eventOwner, string delegatePropertyName, object callbackOwner)
        {
            if (eventOwner == null || callbackOwner == null) return;

            Type type = eventOwner.GetType();
            PropertyInfo property = type.GetProperty(delegatePropertyName, AnyInstance);
            FieldInfo field = type.GetField(delegatePropertyName, AnyInstance);

            Delegate current = property != null
                ? property.GetValue(eventOwner, null) as Delegate
                : field != null ? field.GetValue(eventOwner) as Delegate : null;

            if (current == null) return;

            Delegate cleaned = current;
            foreach (Delegate callback in current.GetInvocationList())
            {
                if (ReferenceEquals(callback.Target, callbackOwner))
                    cleaned = Delegate.Remove(cleaned, callback);
            }

            if (property != null)
                property.SetValue(eventOwner, cleaned, null);
            else if (field != null)
                field.SetValue(eventOwner, cleaned);
            else
                Debug.LogWarning("[NoMoreLeaks] Missing delegate member " + type.FullName + "." + delegatePropertyName);
        }

        internal static object GetInstanceField(object source, string fieldName)
        {
            if (source == null) return null;
            FieldInfo field = AccessTools.Field(source.GetType(), fieldName);
            return field != null ? field.GetValue(source) : null;
        }

        internal static object GetStaticField(Type type, string fieldName)
        {
            if (type == null) return null;
            FieldInfo field = type.GetField(fieldName, AnyStatic);
            return field != null ? field.GetValue(null) : null;
        }

        internal static object GetStaticMember(Type type, string memberName)
        {
            if (type == null) return null;

            PropertyInfo property = type.GetProperty(memberName, AnyStatic);
            if (property != null) return property.GetValue(null, null);

            FieldInfo field = type.GetField(memberName, AnyStatic);
            return field != null ? field.GetValue(null) : null;
        }

        private static MethodInfo FindSingleDelegateParameterMethod(Type type, string name)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name != name) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && typeof(Delegate).IsAssignableFrom(parameters[0].ParameterType))
                    return method;
            }

            return null;
        }

        private static MethodInfo FindInstanceMethod(Type type, string name)
        {
            while (type != null)
            {
                MethodInfo method = type.GetMethod(name, AnyInstance);
                if (method != null) return method;
                type = type.BaseType;
            }

            return null;
        }
    }
}
