using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace SimpleOpenChatServer.Services;

using Chat;

public class ChatService : ChatRoom.ChatRoomBase
{
    private readonly ILogger<ChatService> _logger;
    private OpenChat _openChat;

    public ChatService(ILogger<ChatService> logger, OpenChat openChat)
    {
        _logger = logger;
        _openChat = openChat;
    }

    public override async Task Join(
        IAsyncStreamReader<MessageIn> requestStream,
        IServerStreamWriter<MessageOut> responseStream,
        ServerCallContext context
    )
    {
        if (!await requestStream.MoveNext())
        {
            _logger.LogError("Join failed");
            return;
        }

        do
        {
            _openChat.Join(requestStream.Current.Username, responseStream);
            await _openChat.BroadcastMessageAsync(requestStream.Current);
        } while (await requestStream.MoveNext());

        _openChat.Leave(requestStream.Current.Username);
    }
}
