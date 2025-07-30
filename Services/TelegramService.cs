using JenianAPI.Data;
using JenianAPI.Dtos.TelegramDtos;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Services
{
  public class TelegramService
  {
    private readonly JenianAuthDbContext _dbContext;
    private readonly ILogger<TelegramService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TelegramService(JenianAuthDbContext dbContext, ILogger<TelegramService> logger, HttpClient httpClient, IConfiguration configuration) {
      _dbContext = dbContext;
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
    }

    // This runs in WebHook
    public async Task HandleUpdateAsync(TelegramUpdate update) {
      var msg = update.Message;
      if (msg == null) {
        _logger.LogInformation("Message is null");
        return;
      }

      _logger.LogInformation($"Message from: {msg.From?.Username} (ID: {msg.From?.Id}) | Text: {msg.Text}");

      // return if no text from user
      if (string.IsNullOrEmpty(msg.Text)) return;


      // The user send text "/start {linkToken}"
      if (msg.Text.StartsWith("/start")) {
        // extract {linkToken}
        var linkToken = msg.Text.Split(" ")[1];

        // Check if the token matches any of the users
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramLinkToken == linkToken);

        // if not, meaning the Token is Invalid or Expired
        if (user == null) {
          _logger.LogInformation("Invalid or expired link token.");
          await SendMessageAsync(msg.Chat.Id, "Invalid or expired token, please try again");
          return;
        } else {
          // if yes, save the telegram user Id to the Jenian database
          var isRegistered = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == msg.From.Id.ToString());
          if (isRegistered != null) {
            await SendMessageAsync(msg.Chat.Id, "Looks like this Telegram account is already connected to another Jenian user. Please use a different one.");
            return;
          }
          user.TelegramUserId = msg.From.Id.ToString();
          user.TelegramLinkToken = null; // invalidate the token
          await _dbContext.SaveChangesAsync();
          await SendMessageAsync(msg.Chat.Id, "Your Telegram account is now linked to Jenian App");
        }
        return;
      }

      // After Telegram user id has been saved
      // For future communication, we will use linkedUser to check if the user is authorised to communicate in our chatbot
      var linkedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == msg.From.Id.ToString());
      if (linkedUser == null) {
        _logger.LogInformation("Unauthorized Telegram ID tried to send message.");
        await SendMessageAsync(msg.Chat.Id, "You're not authorized. Please connect your Telegram in the Jenian app first.");
        return;
      }

      // After passing all the protection barriers above 
      _logger.LogInformation($"Message from linked user {linkedUser.UserName}: {msg.Text}");

      // Ready! to receive text from the user
      await SendMessageAsync(msg.Chat.Id, "Message received. Shift parser not implemented yet.");

    }

    private async Task SendMessageAsync(long chatId, string text) {

      var url = $"https://api.telegram.org/bot{_configuration["Telegram:BotToken"]}/sendMessage";

      var payload = new Dictionary<string, object> {
        ["chat_id"] = chatId,
        ["text"] = text
      };

      var response = await _httpClient.PostAsJsonAsync(url, payload);
      response.EnsureSuccessStatusCode();
    }


  }
}
