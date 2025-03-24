using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Commandify
{
    public class CommandServer
    {
        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning;
        private int port = 12345;

        public bool IsRunning => isRunning;
        public int Port 
        { 
            get => port;
            set 
            {
                if (isRunning)
                    throw new InvalidOperationException("Cannot change port while server is running");
                port = value;
            }
        }

        private static CommandServer instance;
        public static CommandServer Instance => instance ??= new CommandServer();

        private CommandServer() { }

        public void Start()
        {
            if (isRunning) return;

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                isRunning = true;

                Debug.Log($"[Commandify] Server started on port {port}");
                _ = ListenForClientsAsync(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Commandify] Failed to start server: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            try
            {
                cancellationTokenSource?.Cancel();
                listener?.Stop();
                isRunning = false;
                Debug.Log("[Commandify] Server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Commandify] Error stopping server: {ex.Message}");
            }
            finally
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        private async Task ListenForClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client, cancellationToken);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Debug.LogError($"[Commandify] Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                StringBuilder messageBuilder = new StringBuilder();

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead == 0) break;

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuilder.Append(receivedData);

                        if (receivedData.EndsWith("\n"))
                        {
                            string command = messageBuilder.ToString().TrimEnd();
                            messageBuilder.Clear();

                            string response = await ProcessCommandAsync(command);
                            byte[] responseData = Encoding.UTF8.GetBytes(response + "\n");
                            await stream.WriteAsync(responseData, 0, responseData.Length, cancellationToken);
                        }
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Debug.LogError($"[Commandify] Error handling client: {ex.Message}");
                }
            }
        }

        private async Task<string> ProcessCommandAsync(string command)
        {
            try
            {
                // Execute on main thread since we're dealing with Unity API
                string result = await ExecuteOnMainThread(() => CommandProcessor.Instance.ProcessCommand(command));
                return result ?? "Command executed successfully";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private async Task<T> ExecuteOnMainThread<T>(Func<T> action)
        {
            T result = default;
            Exception exception = null;
            
            await Task.Run(() =>
            {
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        result = action();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                };
            });

            if (exception != null)
                throw exception;

            return result;
        }
    }
}
