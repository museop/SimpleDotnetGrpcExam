using System.Collections.Concurrent;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Chat;

namespace SimpleOpenChatServer.Services;

public class OpenChat
{
    private readonly ILogger<OpenChat> _logger;

    private ConcurrentDictionary<string, IServerStreamWriter<MessageOut>> _users =
        new ConcurrentDictionary<string, IServerStreamWriter<MessageOut>>();

    public OpenChat(ILogger<OpenChat> logger)
    {
        _logger = logger;
    }

    public void Join(string name, IServerStreamWriter<MessageOut> response)
    {
        _users.TryAdd(name, response);
    }

    public void Leave(string name)
    {
        _users.TryRemove(name, out var value);
    }

    public async Task BroadcastMessageAsync(MessageIn messageIn)
    {
        var timestamp = Timestamp.FromDateTime(DateTime.UtcNow);
        var messageOut = new MessageOut
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            Username = messageIn.Username,
            Content = messageIn.Content
        };

        foreach (var user in _users)
        {
            var succeed = await SendMessageToUser(user, messageOut);
            if (!succeed)
            {
                _logger.LogInformation($"There is no user {user.Key} in chat room.");
                Leave(user.Key);
            }
        }
    }

    private async Task<bool> SendMessageToUser(
        KeyValuePair<string, IServerStreamWriter<MessageOut>> user,
        MessageOut message
    )
    {
        try
        {
            await user.Value.WriteAsync(message);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.ToString());
            return false;
        }
    }
}
