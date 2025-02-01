using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultNamespace.Attribute;

namespace DefaultNamespace
{
    public static class RpcHandler
    {

        private static readonly Dictionary<string, MethodInfo> _methodDict = new();
        private static readonly Dictionary<MethodInfo, object> _instanceDict = new();
        
        public static MethodInfo GetMethod(string methodName)
        {
            if (!_methodDict.TryGetValue(methodName, out var method))
            {
                var allType = Assembly.GetExecutingAssembly().GetTypes().Where(x=> x.IsClass);
                
                foreach (var type in allType)
                {
                    var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public );
                    foreach (MethodInfo methodInfo in methods)
                    {
                        var attribute = methodInfo.GetCustomAttribute<RpcMethodAttribute>();
                        if (attribute != null)
                        {
                            var instance = Activator.CreateInstance(type);
                            _methodDict.TryAdd(methodName, methodInfo);
                            _instanceDict.Add(methodInfo, instance);
                        }
                    }
                }
                
                return _methodDict[methodName];
            }

            return method;
        }
        
        public static void InvokeMethod(MethodInfo methodInfo, params object[] parameters)
        {
            if (!_instanceDict.TryGetValue(methodInfo, out var instance))
            {
                methodInfo = GetMethod(methodInfo.Name);
            }

            methodInfo.Invoke(instance, parameters);
        }
    }
}