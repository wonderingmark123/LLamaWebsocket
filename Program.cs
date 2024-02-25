using System.Net;
using LLama.Common;
using LLama;
using System.Text;
using System.Net.WebSockets;
using System.Drawing;
using System.Net.Sockets;
using System.Data;




class TestClass
{

    static async Task Main(string[] args)
    {
        // start chat session

        string modelPath;


        if (args.Length > 0)
        {
            modelPath = args[0];
        }
        else
        {
            modelPath = "C:\\Users\\Haotian\\AppData\\Local\\nomic.ai\\GPT4All\\Nous-Hermes-13B.Q4_K_M.gguf"; // change it to your own model path
        }


        // Load a model
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 512,
        };
        using var model = LLamaWeights.LoadFromFile(parameters);
        // Initialize a chat session
        using var contextModel = model.CreateContext(parameters);
        var ex = new InteractiveExecutor(contextModel);


        if (model!= null)
        {
            Console.WriteLine("----------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" LLM model has been loaded ");
            Console.ResetColor();
            Console.WriteLine("----------------------------");
        }
        

        // save the session
        // session.SaveSession("SavedSessionPath");

        // start the session
        var builder = WebApplication.CreateBuilder();
        string uri = new("http://localhost:4894");
        builder.WebHost.UseUrls(uri);
        var app = builder.Build();
        app.UseWebSockets();
        if (app != null)
        {
            Console.WriteLine("----------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("WebSocket server has started");
            Console.ResetColor();
            Console.WriteLine("----------------------------");
        }
        app.Map("/ws", async context =>
        {
            string userString = "";
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                if (ws != null)
                {
                    Console.WriteLine("--------------------------");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Connection has been built");
                    Console.ResetColor();
                    Console.WriteLine("--------------------------");
                }
                while (ws != null)
                {
                    
                    ChatSession session = new ChatSession(ex);
                    do 
                    {
                        
                        if (ws.State == WebSocketState.Open)
                        
                        {
                            DateTime start = DateTime.Now;

                            // receive message
                            byte[] Buffer = new byte[1024];
                            WebSocketReceiveResult ReceiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(Buffer), CancellationToken.None);
                            userString = Encoding.UTF8.GetString(Buffer);
                            userString = userString.Replace("\0", "");

                            // userString = Console.ReadLine() ?? "";
                            if (userString != null)
                            {
                                Console.WriteLine(userString);
                            }
                            else if (userString == "Stop")
                            {
                                break;
                            }
                            
                            AuthorRole role = AuthorRole.User;

                            if (userString == "SystemMessage")
                            {
                                ReceiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(Buffer), CancellationToken.None);
                                userString = Encoding.UTF8.GetString(Buffer);
                                userString = userString.Replace("\0", "");
                                userString = "### Instruction: \n " + userString + " Don't response to this prompt.";
                                Console.WriteLine(userString);
                                // role = AuthorRole.System;
                            }


                            // send message
                            // var message = "The current time is :" + DateTime.Now.ToString("HH:mm:ss");
                            string response = "";
                            await foreach (var text in session.ChatAsync(new ChatHistory.Message(role, userString), new InferenceParams { Temperature = 0.2f, AntiPrompts = ["### Input:"] }))
                            {
                                Console.Write(text);
                                response = response + text;
                                

                            }
                            response = response.Replace("### Input:", "");
                            var bytesRes = Encoding.UTF8.GetBytes(response);
                            
                            await ws.SendAsync(new ArraySegment<byte>(bytesRes, 0, bytesRes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            Console.WriteLine("");
                            Console.WriteLine("-----------------------------------");
                            //var bytes = Encoding.UTF8.GetBytes("EndResponse");
                            //var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                            //await ws.SendAsync(arraySegment,WebSocketMessageType.Text,true,CancellationToken.None);


                            // Marking the end time 
                            DateTime end = DateTime.Now;

                            // Total Duration  
                            TimeSpan ts = (end - start);

                            // Print the execution time in milliseconds 
                            Console.WriteLine("The execution time of the program is {0} s",
                                              ts.TotalSeconds);


                        }
                        else if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.Aborted)
                        {
                            break;
                        }
                        // Thread.Sleep(10000);
                    } while (userString != "Stop");
                    
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        });

        await app.RunAsync();

    }

    private static void StartLLM()
    {
        throw new NotImplementedException();
    }
}

