using System;
using System.Collections;
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
            RemoveGameEvent(eventSource, owner, null, methodName);
        }

        internal static void RemoveGameEvent(object eventSource, object owner, Type handlerDeclaringType, string methodName)
        {
            if (eventSource == null || owner == null) return;

            MethodInfo handlerMethod = handlerDeclaringType != null
                ? handlerDeclaringType.GetMethod(methodName, AnyInstance)
                : FindInstanceMethod(owner.GetType(), methodName);

            if (handlerMethod == null)
            {
                string typeName = handlerDeclaringType != null ? handlerDeclaringType.FullName : owner.GetType().FullName;
                Debug.LogWarning("[NoMoreLeaks] Missing handler " + typeName + "." + methodName);
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

        internal static int RemoveDestroyedOwners(object eventSource, Type ownerType)
        {
            if (eventSource == null || ownerType == null) return 0;

            FieldInfo eventsField = AccessTools.Field(eventSource.GetType(), "events");
            if (eventsField == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing events list on " + eventSource.GetType().FullName);
                return 0;
            }

            IList events = eventsField.GetValue(eventSource) as IList;
            if (events == null) return 0;

            int removed = 0;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                object eventEntry = events[i];
                if (eventEntry == null) continue;

                FieldInfo originatorField = AccessTools.Field(eventEntry.GetType(), "originator");
                if (originatorField == null)
                {
                    Debug.LogWarning("[NoMoreLeaks] Missing event originator field on " + eventEntry.GetType().FullName);
                    return removed;
                }

                object originator = originatorField.GetValue(eventEntry);
                string originatorTypeName = GetOriginatorTypeName(eventEntry);
                if (!OwnerMatches(ownerType, originator, originatorTypeName)) continue;

                UnityEngine.Object unityObject = originator as UnityEngine.Object;
                bool isDestroyedUnityObject = !ReferenceEquals(unityObject, null) && unityObject == null;
                if (originator != null && !isDestroyedUnityObject) continue;

                events.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        internal static int RemoveDestroyedOwnersByTypeName(object eventSource, string ownerTypeName)
        {
            if (eventSource == null || string.IsNullOrEmpty(ownerTypeName)) return 0;

            FieldInfo eventsField = AccessTools.Field(eventSource.GetType(), "events");
            if (eventsField == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing events list on " + eventSource.GetType().FullName);
                return 0;
            }

            IList events = eventsField.GetValue(eventSource) as IList;
            if (events == null) return 0;

            int removed = 0;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                object eventEntry = events[i];
                if (eventEntry == null) continue;

                FieldInfo originatorField = AccessTools.Field(eventEntry.GetType(), "originator");
                if (originatorField == null)
                {
                    Debug.LogWarning("[NoMoreLeaks] Missing event originator field on " + eventEntry.GetType().FullName);
                    return removed;
                }

                object originator = originatorField.GetValue(eventEntry);
                string originatorTypeName = GetOriginatorTypeName(eventEntry);

                Type originatorType = originator != null ? originator.GetType() : null;
                bool typeMatches = TypeNameMatches(originatorType, originatorTypeName, ownerTypeName);
                if (!typeMatches) continue;

                UnityEngine.Object unityObject = originator as UnityEngine.Object;
                bool isDestroyedUnityObject = !ReferenceEquals(unityObject, null) && unityObject == null;
                if (originator != null && !isDestroyedUnityObject) continue;

                events.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        internal static int RemoveOwner(object eventSource, object owner)
        {
            if (eventSource == null || owner == null) return 0;

            FieldInfo eventsField = AccessTools.Field(eventSource.GetType(), "events");
            if (eventsField == null)
            {
                Debug.LogWarning("[NoMoreLeaks] Missing events list on " + eventSource.GetType().FullName);
                return 0;
            }

            IList events = eventsField.GetValue(eventSource) as IList;
            if (events == null) return 0;

            int removed = 0;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                object eventEntry = events[i];
                if (eventEntry == null) continue;

                FieldInfo originatorField = AccessTools.Field(eventEntry.GetType(), "originator");
                if (originatorField == null)
                {
                    Debug.LogWarning("[NoMoreLeaks] Missing event originator field on " + eventEntry.GetType().FullName);
                    return removed;
                }

                if (!ReferenceEquals(originatorField.GetValue(eventEntry), owner)) continue;

                events.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        private static bool OwnerMatches(Type ownerType, object originator, string originatorTypeName)
        {
            if (originator != null && ownerType.IsInstanceOfType(originator)) return true;
            return TypeNameMatches(null, originatorTypeName, ownerType.Name)
                || TypeNameMatches(null, originatorTypeName, ownerType.FullName);
        }

        private static bool TypeNameMatches(Type originatorType, string originatorTypeName, string ownerTypeName)
        {
            if (originatorType != null)
            {
                if (originatorType.Name == ownerTypeName || originatorType.FullName == ownerTypeName) return true;
            }

            if (originatorTypeName == null) return false;

            return originatorTypeName == ownerTypeName
                || originatorTypeName.EndsWith("." + ownerTypeName, StringComparison.Ordinal)
                || originatorTypeName.EndsWith(":" + ownerTypeName, StringComparison.Ordinal);
        }

        private static string GetOriginatorTypeName(object eventEntry)
        {
            FieldInfo originatorTypeField = AccessTools.Field(eventEntry.GetType(), "originatorType");
            return originatorTypeField != null ? originatorTypeField.GetValue(eventEntry) as string : null;
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
