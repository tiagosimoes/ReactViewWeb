﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReactViewControl.WebServer {
    public static class ServerAPI {

        public static string StarterURL;
        static readonly List<ServerView> ServerViews = new List<ServerView>();

        public enum Operation {
            RegisterObjectName,
            UnregisterObjectName,
            EvaluateScriptFunctionWithSerializedParams,
            Execute,
            ResizePopup,
            ReturnValue,
            OpenURL,
            OpenURLInPopup,
            OpenTooltip,
            OpenContextMenu
        }

        public static Stream GetCustomResource(string nativeobjectname, string customPath, out string extension) {
            var nativeObject = nativeobjectname != "" ? ServerViews.FirstOrDefault(conn => conn.NativeAPIName == nativeobjectname) : ServerViews.Last();
            return nativeObject.GetCustomResource(customPath, out extension);
        }
        public static void AddSocket(WebSocket socket, TaskCompletionSource<object> socketFinishedTcs, string path) {
            var serverView = ServerViews.Find(conn => conn.NativeAPIName == path.Substring(1));
            if (serverView != null) {
                serverView.SetSocket(socket);
            }
        }

        readonly static string ReactViewResources = "ReactViewResources";
        readonly static string CustomResourcePath = "custom/resource";

        internal static void NewNativeObject(ServerView serverView) {
            ServerViews.Add(serverView);
            string url = $"/{ReactViewResources}/index.html?./&true&__Modules__&{serverView.NativeAPIName}&{CustomResourcePath}";
            if (ServerViews.Count == 1) {
                StarterURL = url;
            } else {
                _ = Task.Run(() => {
                    while (LastConnectionWithActivity() == null) {
                        Task.Delay(1);
                    }
                    Operation operation;
                    switch (serverView.GetViewName()) {
                        case "AIContextSuggestionsMenuView":
                        case "ReactViewHostForPlugins":
                        case "OutSystemsBrowserLoadingView":
                        case "DialogView":
                            operation = Operation.OpenURLInPopup;
                            break;
                        case "TooltipView":
                            // TODO TCS, fix tooltips 
                            return;
                        case "WorkspaceView":
                        default:
                            operation = Operation.OpenURL;
                            break;
                    }
                    _ = LastConnectionWithActivity().SendWebSocketMessage(operation, url);
                });
            }
        }

        private static ServerView LastConnectionWithActivity() {
            return ServerViews.Where(serverView => serverView.IsOpen()).OrderByDescending(serverView => serverView.LastActivity).FirstOrDefault();
        }

        internal static void CloseSocket(ServerView serverView) {
            ServerViews.Remove(serverView);
            if (ServerViews.Count == 0) {
                Environment.Exit(0);
            }
        }
        public static void OpenURL(Uri uri) {
            while (LastConnectionWithActivity() == null) {
                Task.Delay(1);
            }
            _ = LastConnectionWithActivity().SendWebSocketMessage(Operation.OpenURL, uri.AbsoluteUri);
        }

    }
}
