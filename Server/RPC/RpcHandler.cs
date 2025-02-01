using System.Net.WebSockets;
using System.Reflection;
using Chatting.Controllers;

namespace Chatting.RPCHandler
{
    public class RpcHandler
    {
        private Dictionary<string, MethodInfo> _rpcMethodMap = new Dictionary<string, MethodInfo>();
        private Dictionary<Type, object> _classMethodMap = new Dictionary<Type, object>();
        private WebSocketController _webSocketController;

        public RpcHandler(WebSocketController webSocketController)
        {
            Console.WriteLine("RpcHandler called");
            _webSocketController = webSocketController;
            RegisterHandler();
        }

        private void RegisterHandler()
        {
            Console.WriteLine("RegisterHandler called");

            var classList = Assembly.GetExecutingAssembly()
                                     .GetTypes()
                                     .Where(t => t.Namespace == "Chatting.RPC" && t.IsClass);

            foreach (var classType in classList)
            {
                try
                {
                    Console.WriteLine($"Processing class: {classType.Name}");

                    // 이미 생성된 인스턴스인지 확인 (중복 생성 방지)
                    if (_classMethodMap.ContainsKey(classType))
                        continue;

                    var instance = Activator.CreateInstance(classType, _webSocketController);
                    _classMethodMap[classType] = instance;

                    // 클래스의 메서드를 안전하게 가져오기
                    var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                           .Where(m =>
                                           {
                                               try
                                               {
                                                   return m.GetCustomAttribute<RpcMethodAttribute>() != null;
                                               }
                                               catch (Exception ex)
                                               {
                                                   Console.WriteLine($"Error processing method {m.Name} in class {classType.Name}: {ex.Message}");
                                                   return false;
                                               }
                                           });

                    foreach (var method in methods)
                    {
                        if (!_rpcMethodMap.ContainsKey(method.Name))
                        {
                            _rpcMethodMap[method.Name] = method;
                            Console.WriteLine($"Registered method: {method.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error registering methods for class {classType.Name}: {ex.Message}");
                }
            }
        }

        public void HandlePacket(WebSocketRequestData requestData, WebSocket webSocket)
        {
            if (!_rpcMethodMap.TryGetValue(requestData.eventType, out var method))
            {
                Console.WriteLine($"error {requestData.eventType}");
                return;
            }

            var metehod = _classMethodMap[method.DeclaringType];
            method.Invoke(metehod, [requestData, webSocket]);
        }

    }
}
