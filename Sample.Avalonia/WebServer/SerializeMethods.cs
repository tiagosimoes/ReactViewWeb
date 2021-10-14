﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Sample.Avalonia.WebServer {
    class TypeDTO {
        public string AssemblyName;
        public string ClassName;

        public static TypeDTO FromType(Type type) {
            return new TypeDTO() {
                AssemblyName = type.Assembly.FullName,
                ClassName = type.FullName
            };
        }
    }

    class MethodSignatureDTO {
        public TypeDTO ReturnType;
        public string MethodName;
        public TypeDTO[] ParameterTypes;

        public static MethodSignatureDTO FromMethod(MethodInfo method) {
            return new MethodSignatureDTO() {
                ReturnType = TypeDTO.FromType(method.ReturnType),
                MethodName = method.Name,
                ParameterTypes = method.GetParameters().Select(t => TypeDTO.FromType(t.ParameterType)).ToArray()
            };
        }
    }

    class SerializedObject {
        public TypeDTO type;
        public MethodSignatureDTO[] methods;
        public SerializedObject(object obj) {
            type = TypeDTO.FromType(obj.GetType());
            var methodSignatures = new List<MethodSignatureDTO>();
            foreach (var method in obj.GetType().GetMethods()) {
                methodSignatures.Add(MethodSignatureDTO.FromMethod(method));
            }
            methods = methodSignatures.ToArray();
        }

        public static string SerializeObject(object obj) {
            var serializedObject = new SerializedObject(obj);
            return JsonSerializer.Serialize(serializedObject, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true});
        }

        [Serializable]
        public struct MethodCall {
            public string ObjectName;
            public string MethodName;
            public object Args;
            public int CallKey;
        }

        public static MethodCall DeserializeMethodCall(string text) {
            return JsonSerializer.Deserialize<MethodCall>(text, new JsonSerializerOptions { IncludeFields = true });
        }

        private static object GetJSONValue(JsonElement elem, Type type) {
            return elem.ValueKind switch {
                JsonValueKind.Null => null,
                JsonValueKind.False => false,
                JsonValueKind.True => true,
                _ => JsonSerializer.Deserialize(elem.GetRawText(), type),
            };
            throw new NotImplementedException();
        }

        internal static object ExecuteMethod(object obj, MethodCall methodCall) {
            var method = obj.GetType().GetMethod(methodCall.MethodName);
            List<object> arguments = new List<object>();
            if (methodCall.Args is JsonElement elem) {
                if (method.GetParameters().Length > 0) {
                    foreach (var item in elem.EnumerateArray().Select((value, index) => new { index, value })) {
                        var parameter = method.GetParameters()[item.index];
                        if (item.value.ValueKind == JsonValueKind.Array && !parameter.ParameterType.IsArray) {
                            foreach (var subitem in item.value.EnumerateArray().Select((value, index) => new { index, value })) {
                                var subparameter = method.GetParameters()[subitem.index];
                                arguments.Add(GetJSONValue(subitem.value, subparameter.ParameterType));
                            }
                            break;
                        } else {
                            arguments.Add(GetJSONValue(item.value, parameter.ParameterType));
                        }
                    }
                }
            }
            if (method.ReturnType == typeof(void)) {
                obj.GetType().GetMethod(methodCall.MethodName).Invoke(obj, arguments.ToArray());
                return null;
            } else {
                return obj.GetType().GetMethod(methodCall.MethodName).Invoke(obj, arguments.ToArray());
            }
        }
    }
}
