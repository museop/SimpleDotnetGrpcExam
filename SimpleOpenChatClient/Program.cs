using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chat;
using Grpc.Core;
using Grpc.Net.Client;

namespace SimpleOpenChatClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to Simple Open Chat!");

        Console.Write("What is your name?: ");
        var userName = Console.ReadLine();

        using var channel = GrpcChannel.ForAddress("http://localhost:5000");
        var client = new ChatRoom.ChatRoomClient(channel);

        using (var chat = client.Join())
        {
            _ = Task.Run(async () =>
            {
                while (await chat.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var response = chat.ResponseStream.Current;
                    Console.WriteLine(
                        $"[{response.Timestamp}] {response.Username}: {response.Content}"
                    );
                }
            });

            await chat.RequestStream.WriteAsync(
                new MessageIn { Username = userName, Content = "Join" }
            );

            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                if (line.ToLower() == "bye")
                {
                    break;
                }
                var message = new MessageIn { Username = userName, Content = line };
                await chat.RequestStream.WriteAsync(message);
            }

            await chat.RequestStream.CompleteAsync();
        }
    }
}
