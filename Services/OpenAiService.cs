using OpenAI.Chat;

namespace JenianAPI.Services
{
  public class OpenAiService
  {
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(ChatClient chatClient, ILogger<OpenAiService> logger) {
      _chatClient = chatClient;
      _logger = logger;
    }

    //public async Task<string> RosterQuery(string ocrText, string staffName, CancellationToken ct = default) {

    //  var messages = new ChatMessage[] {
    //    new SystemChatMessage("""
    //      You extract shifts from OCR roster text  by {staffName}.
    //      - The format will be {word}:[Bounding Polygon coordinates].
    //      - You are to analyse the [Bounding Polygon coordinates] to determine the  staff's shift day.
    //      - Normalise to 12-hour AM/PM (e.g., 8 - 4 → 8:00 AM - 4:00 PM; 8 - 4.30 → 8:00 AM - 4:30 PM).
    //      - Assume start ≤9 → AM; 1–11 as PM unless contradicted by end.
    //      - Keep tags like MT/AL as "(TAG)" after time.
    //      - Ignore noise (CATALOGUE, FULL-TIME, PART-TIME, CASUAL, OFF-SITE EMPLOYEES, IMPORTANT NOTICE, EMPLOYEES NUMBER, etc.).
    //      Output strictly:
    //      {staffName} has shifts on:
    //      {DAY}: {start - end} (TAG if any)
    //      (Only include days that have shifts.)
    //      If the {staffName} is not found, return "I cannot find the staff name, please make sure to write their full name".
    //      If the {staffName} does not any shift, return "{staffName} is enjoying the holiday".
    //      Do not try to give extra answers, always stick to the above output strictly.
    //      """),
    //    new UserChatMessage($"Extract shifts for {staffName} using the below OCR roster text:" +
    //    $"{ocrText}")
    //  };

    //  //ChatCompletion completion = await _chatClient.CompleteChatAsync("Say 'This is a test.'");
    //  ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);

    //  //var shifts = RosterParser.ExtractPersonShifts(ocrText, staffName);
    //  //_logger.LogInformation("res from rosterParser: {0}", shifts);

    //  return completion.Content[0].Text;

    //  //var input = BinaryData.FromBytes("""
    //  //      {
    //  //         "messages": [
    //  //             {
    //  //                 "role": "user",
    //  //                 "content": "What is the weather tomorow in Melboure, Australia'"
    //  //             }
    //  //         ]
    //  //      }
    //  //      """u8.ToArray());

    //  //using BinaryContent content = BinaryContent.Create(input);
    //  //var result = await _chatClient.CompleteChatAsync(content);

    //  //return result.GetRawResponse().Content.ToString();
    //}

    public async Task<string> RosterQuery(string ocrText, string staffName, CancellationToken ct = default) {
      var messages = new ChatMessage[]
      {
        new SystemChatMessage($$"""
          You extract shift days for a single staff member from OCR tokens that include 4-point bounding polygons.

          INPUT FORMAT
          - Tokens arrive as: `Text,[(x1, y1) (x2, y2) (x3, y3) (x4, y4)]` separated by spaces.
          - Coordinates are pixels in the page image. You must use them to map times to weekdays.

          TASK
          1) Locate weekday headers among tokens: MON, TUE, WED, THU/THUR, FRI, SAT, SUN (case-insensitive). 
              - Compute each header’s X-center (mean of its 4 x’s). Sort left→right.
              - Build day “columns” using midpoints between adjacent header centers; outer columns extend to ±∞.
          2) Find the row that contains the staff name "{{staffName}}" (substring match, case-insensitive). 
              - Compute the name token’s Y-center. Define a same-row band of ±15 px around that Y (if no other signals).
          3) Identify time tokens on that row band (Y-center inside band). Time tokens match:
              - `H - H`, `H - H:MM`, `H - HMM`, `H.HH`, `H - H.HH`, allowing separators `- – . :`
              - Keep any trailing tag (e.g., `AL`, `MT`) and output it in parentheses.
          4) Map each time token to a weekday by its X-center falling inside that weekday’s column.
          5) Normalise times to 12-hour with AM/PM:
              - Add minutes `:00` if missing; `4.30`/`430` ⇒ `4:30`.
              - Assume start ≤ 9 ⇒ AM. Hours 1–11 without AM/PM ⇒ PM unless contradicted by end.
              - Example: `8 - 4.30` ⇒ `8:00 AM - 4:30 PM`; `1 - 9` ⇒ `1:00 PM - 9:00 PM`; `11 - 7` ⇒ `11:00 AM - 7:00 PM`.
          6) Ignore noise tokens (not shifts or weekdays), e.g.: CATALOGUE, FULL-TIME, PART-TIME, CASUAL, OFF-SITE EMPLOYEES, IMPORTANT NOTICE, EMPLOYEES NUMBER, month/year headers, site names, etc.
          7) Do not infer or guess days without a weekday header; only use coordinates to map.

          STRICT OUTPUT
          - If the staff name isn’t present in tokens: `I cannot find the staff name, please make sure to write their full name`
          - If present but no time tokens map to any weekday: `{{staffName}} is enjoying the holiday`
          - Otherwise output exactly:

          {{staffName}} has shifts on:
          {DAY}: {START AM/PM} - {END AM/PM} (TAG if any)
          {DAY}: ...

          Notes:
          - Use `THU` for `THUR`.
          - Order days Mon→Sun.
          - No explanations, no markdown, no extra text.

          EXAMPLE (tiny)
          Tokens:
          `MON,[(365,71) (395,71) (394,84) (365,85)] TUE,[(485,71) (506,72) (506,82) (486,83)] SUN,[(1012,75) (1037,76) (1037,86) (1012,86)] VOLKAN DEMIRBAS,[(41,813) (173,814) (172,829) (41,827)] 8 - 4,[(353,817) (376,818) (376,829) (352,829)] 3 .9,[(474,820) (497,821) (498,830) (473,830)] 9-7,[(1013,829) (1038,829) (1038,838) (1013,839)]`

          Output:
          `VOLKAN DEMIRBAS has shifts on:
          MON: 8:00 AM - 4:00 PM
          TUE: 3:00 PM - 9:00 PM
          SUN: 9:00 AM - 7:00 PM`
          """),
        new UserChatMessage($"""
          Extract shifts for {staffName} using the below OCR roster text:
          {ocrText}
          """)
      };

      // If your SDK supports it, set temperature = 0 for determinism.
      ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
      return completion.Content[0].Text;
    }


  }
}
