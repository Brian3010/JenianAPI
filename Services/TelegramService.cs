using JenianAPI.Data;
using JenianAPI.Dtos.TelegramDtos;
using JenianAPI.Helpers;
using JenianAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static JenianAPI.Dtos.TelegramDtos.TelegramFileHandler;

namespace JenianAPI.Services
{
  public class TelegramService
  {
    private readonly JenianAuthDbContext _dbContext;
    private readonly ILogger<TelegramService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IParserService _parserService;

    public TelegramService(JenianAuthDbContext dbContext, ILogger<TelegramService> logger, HttpClient httpClient, IConfiguration configuration, IParserService parserService) {
      _dbContext = dbContext;
      _logger = logger;
      _httpClient = httpClient;
      _configuration = configuration;
      _parserService = parserService;
    }

    // This runs in WebHook
    public async Task HandleUpdateAsync(TelegramUpdate update) {
      /** NOTE: Sending Photos
      * Compressed Photo is identified as "photo" field
      * Original Photo is identified as "document" field
      * Forwarding Photo is identified as "photo" field
      *
      */
      var msg = update.Message;
      //_logger.LogInformation($"Message is {msg.Photo.Count}");
      if (msg == null) {
        _logger.LogInformation("Message is null");
        return;
      }

      // return if no text from user
      if (string.IsNullOrEmpty(msg.Text) && msg.Photo == null && msg.Document == null) {
        _logger.LogInformation("Message is empty (No Photos, Text or Document received)");
        return;
      }

      _logger.LogInformation($"Message from: {msg.From?.Username} (ID: {msg.From?.Id}) | Text: {msg.Text} | Photo: {msg.Photo}");

      // The user send text "/start {linkToken}"
      if (msg.Text != null && msg.Text.StartsWith("/start")) {
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
      _logger.LogInformation($"Message from linked user {linkedUser.UserName}: {msg.Text} | {msg.Photo} | {msg.Document} ");

      /** Ready! to receive text from the user*/
      await SendMessageAsync(msg.Chat.Id, "Message received. Processing now...");

      await HandleMessageAsync(msg);

      return;
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

    private async Task<string> GetDownloadFilePath(string fileId) {
      var telegramBotToken = _configuration["Telegram:BotToken"];
      // Get the file_path using Telegram’s getFile API
      var fileUrl = $"https://api.telegram.org/bot{telegramBotToken}/getFile?file_id={fileId}";
      var res = await _httpClient.GetFromJsonAsync<TelegramFileResponse>(fileUrl);

      if (res == null || !res.Ok) throw new Exception("");

      return $"https://api.telegram.org/file/bot{telegramBotToken}/{res.Result.FilePath}";
    }

    private async Task<string> GetPhotoBase64Async(string downloadFileUrl) {
      var fileStream = await _httpClient.GetStreamAsync(downloadFileUrl);

      // 3. Convert stream to Base64
      using var ms = new MemoryStream();
      await fileStream.CopyToAsync(ms);
      var fileBytes = ms.ToArray();
      var base64Image = Convert.ToBase64String(fileBytes);

      return base64Image;
    }

    private async Task<MemoryStream> ConvertUrlPhotoToMemoryStream(string photoUrl) {
      byte[] photoBytes = await _httpClient.GetByteArrayAsync(photoUrl);
      var photoStream = new MemoryStream(photoBytes);

      return photoStream;
    }

    private async Task<byte[]> ConvertUrlPhotoToBytes(string photoUrl) {
      byte[] photoBytes = await _httpClient.GetByteArrayAsync(photoUrl);
      return photoBytes;
    }

    public async Task HandleMessageAsync(TelegramMessage message) {

      // TODO: implementing parser for photo, document, and text
      /**
       * Get the photo file_id from Telegram
       * Get the file_path using Telegram’s getFile API
       * Download the photo content (as bytes/stream)
       * Send it to OpenAI Vision API (GPT-4-Vision) for parsing
       *  Sending it to OpenAI Vision API (GPT-4-Vision)
       *  Parsing the AI response
       * Process the response (shift info)
       * Save & reply to user
       */
      var imageTelegramStream = new MemoryStream();
      var compressedImage = new MemoryStream();
      string downloadFilePath;
      string photoFileId = "";

      if (message.Photo != null) {
        // Get the photo file_id from Telegram
        photoFileId = message.Photo.Last().FileId;
      }
      if (message.Document != null) {
        photoFileId = message.Document.FileId;
      }

      if (!String.IsNullOrEmpty(photoFileId)) {
        downloadFilePath = await GetDownloadFilePath(photoFileId);
        imageTelegramStream = await ConvertUrlPhotoToMemoryStream(downloadFilePath);
        compressedImage = await ImageHelper.CompressImageInStream(imageTelegramStream);
      }


      //_logger.LogInformation($"Compressed Image: {compressedImage}");
      // TODO: parse Text

      // TODO: Implement Open AI call - sending base64Image to OpenAi
      try {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await _parserService.ParseShiftFromPhotoAsync(compressedImage, cts.Token);
      } catch (Exception e) {

        _logger.LogInformation(e.Message);
      }



      //return new ParseResult {
      //  Success = true,
      //  FileDownloadUrl = base64Image,
      //  ParsedShiftText = caption, // You can parse caption text further with AI
      //  Message = "Photo processed successfully"
      //};

    }




  }
}
