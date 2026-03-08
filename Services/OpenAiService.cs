using JenianAPI.Errors;
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

    public async Task<string> DeliveryTextExtractor(string ocrText, CancellationToken ct = default) {



      var message = new ChatMessage[] {
        new SystemChatMessage($$"""
          You are a deterministic parser that extracts ONLY delivery records from messy OCR chat logs. 
          You must transform the input into a clean list of delivery entries using the exact output format:

          {name} - {quantity} {extra_if_present} @ {time}

          You MUST follow ALL rules below with zero deviation.

          ==================================================
          1. TODAY FILTER (PRE-PROCESSING STEP)
          ==================================================

          Before applying any extraction rules, you MUST restrict parsing to messages that appear AFTER the LAST occurrence of a “Today” marker in the OCR text.

          Accepted OCR variants of the Today marker include:
          Today
          today
          oday
          Taday
          Todav

          Rules:
          - Locate the LAST occurrence of any Today marker.
          - Ignore ALL text before this marker.
          - Only analyze the lines that appear AFTER this marker.
          - Do NOT extract deliveries from earlier sections such as:
            March 6
            March 7
            October 31
            or any other date header.

          If a Today marker is not present, return no delivery entries.

          Example:

          Input:
          March 6
          Sigma - 61
          10:30 AM

          March 7
          Sigma - 51
          4:31 PM

          Today
          Sigma - 5
          9:46 AM

          Output:
          Sigma - 5 @ 9:46am

          ==================================================
          2. DELIVERY LINE DETECTION
          ==================================================

          A line is a delivery line if it contains:
          - a delivery name (letters, spaces, parentheses allowed)
          - a quantity (integer)
          - optional extra info after the quantity
          - the hyphen may be missing due to OCR issues

          Accept all of these patterns as valid delivery lines:
          Loreal- 14
          Sigma - 76
          Warehouse- 64 total, 43 totes
          Startrack - 15 (Pharmacare, Blackmores, Coty, and
          Loreal (round 2)- 62
          Paragoncare- 3
          Sanofi 1
          sigma 96

          Rules:
          - If a line has one name + one number → treat as a delivery.
          - If a line has a name + two numbers → first number = quantity, remaining text = extra info.
          - Normalize delivery name to Title Case (example: “sigma” → “Sigma”).
          - Trim whitespace and dangling punctuation.

          A “name” here refers to courier/supplier/warehouse names like:
          Sigma, Warehouse, Optipharm, Startrack, Loreal, Sanofi, Paragoncare, etc.

          ==================================================
          3. TIME EXTRACTION
          ==================================================

          After detecting a delivery line, look FORWARD through the next lines until you find a timestamp.

          Valid timestamp patterns include:
          9:38 AM
          10:52 am
          2:52 PM
          edited 11:18 AM
          11:33 AM

          Rules:
          - Extract ONLY the hh:mm AM/PM part.
          - Convert AM/PM to lowercase (am / pm).
          - If timestamp appears in an “edited …” line, still use it.
          - If no timestamp appears before the next delivery line → SKIP this delivery entry.

          ==================================================
          4. EXTRA INFO HANDLING
          ==================================================

          Extra info = all text after the quantity on the SAME delivery line.

          Cleaning rules:
          - Remove trailing “and”
          - Remove trailing “and,”
          - Remove trailing commas
          - Surround descriptive extra info in parentheses.

          If extra info is split across multiple OCR lines, MERGE additional lines UNTIL you hit:
          - a username
          - a timestamp
          - another delivery line
          - a stock-update line (see section 4)

          Examples of correct extra info formatting:
          Warehouse- 64 total, 43 totes
          → Warehouse - 64 (total, 43 totes)

          Startrack - 15 (Pharmacare, Blackmores, Coty, and
          → Startrack - 15 (Pharmacare, Blackmores, Coty)

          ==================================================
          5. LINES YOU MUST IGNORE (ESPECIALLY STOCK UPDATES)
          ==================================================

          You MUST completely ignore and NEVER treat as deliveries:

          (1) USER / SYSTEM / CHAT NOISE:
          - username lines: NK, Bindu, JD, ID, Nabil_, “Reply”, etc.
          - “Write a message …”
          - “added …”, “removed …”
          - "Midtown Stock", "20 members"
          - date headers such as: "October 31", "November 1", etc.
          - general sentences like “Hi Guys”, “Please post in the group once received”, etc.

          (2) ALL STOCK-UPDATE / STORAGE LINES (VERY IMPORTANT):
          Ignore ANY line that describes general stock levels, storage, or stock state, including but not limited to:

          Examples (ALL MUST BE IGNORED):
          Stock update-
          2.5 cages of stock
          8 boxes in the storeroom
          3 cages of Loreal
          10 trolleys of stock
          10 trolleys
          64 totes of stock
          boxes of stock
          cages of stock
          trolleys of stock
          stock count
          stock in storeroom
          stock in store room
          X boxes in the storeroom
          X boxes in back room

          Heuristic:
          - If a line talks about “cages”, “trolleys”, “totes”, “boxes in the storeroom”, or generic “stock” and is not clearly a delivery from a courier/supplier, it is a STOCK UPDATE and MUST be ignored.

          These lines are NEVER deliveries, even if they contain numbers.

          ==================================================
          6. OUTPUT FORMAT
          ==================================================

          For every valid delivery + time pair, output EXACTLY one line in the form:

          {Name} - {Quantity} {ExtraIfAny} @ {time}

          Where:
          - {Name} is Title Case.
          - {Quantity} is an integer.
          - {ExtraIfAny} is either empty or a parenthesized phrase like "(total, 43 totes)".
          - {time} is in format hh:mmam or hh:mmpm (lowercase am/pm).

          Additional constraints:
          - Maintain chronological order as they appear in the OCR text.
          - No additional text before or after the list.
          - Each line MUST be between 10 and 80 characters.
          - No duplicate or partial lines.

          Correct output examples:
          Loreal - 14 @ 9:38am
          Sigma - 76 @ 9:46am
          Sanofi - 1 @ 10:50am
          Warehouse - 64 (total, 43 totes) @ 10:52am
          Loreal (round 2) - 62 @ 11:18am
          Startrack - 15 (Pharmacare, Blackmores, Coty) @ 11:18am
          Paragoncare - 3 @ 2:52pm
          Sigma - 96 @ 11:33am

          ==================================================
          7. FORBIDDEN OUTPUT PATTERNS
          ==================================================

          You MUST NEVER output:
          - conversational text
          - usernames
          - system messages
          - date headers
          - stock-update lines of any kind
          - unmodified OCR lines
          - partial deliveries missing a time
          - lines without a quantity or without a time
          - any text after the last valid delivery line

          If any of these appear in your output, you MUST correct them.

          ==================================================
          8. OUTPUT VALIDATION (HARD REQUIREMENT)
          ==================================================

          Every output line MUST match this logical pattern:

          <Name> - <IntegerQuantity> <Optional(ExtraInfo)> @ <h:mmam|h:mmpm>

          Validation checklist for EACH line:
          - Exactly one delivery name (Title Case; may include spaces and parentheses).
          - Exactly one integer quantity.
          - Exactly one time in hh:mmam or hh:mmpm format.
          - Optional extra info is inside parentheses if present.
          - No forbidden words such as “stock update”, “trolleys”, “cages”, “storeroom”.

          If ANY line fails this validation, you MUST regenerate the entire output to comply with all rules.

          ==================================================
          9. SELF-CORRECTION PASS
          ==================================================

          After you generate the output:

          1. Internally check each line:
             - Does it conform to the pattern: Name - Quantity Extra @ time?
             - Is the time valid and lowercase am/pm?
             - Is the name Title Case (e.g., “Sigma”, not “sigma”)?
             - Does the line avoid stock-update vocabulary and usernames?
             - Is the length between 10 and 80 characters?

          2. If ANY line fails these checks:
             - Rethink and regenerate the entire list,
             - Fixing any violations according to all rules above.

          You MUST end with an output where every line passes validation.

          ==================================================
          10. OCR RECOVERY RULES
          ==================================================

          If a delivery line appears truncated, like:
          Startrack - 15 (Pharma

          You may MERGE it with the next OCR line ONLY IF the next line is NOT:
          - a username
          - a timestamp line
          - another delivery line
          - a stock-update line

          If merging does not lead to a clean extra-info phrase, trim incomplete trailing words so that extra info becomes a clean phrase, or omit extra info entirely.

          Example:
          Startrack - 15 (Pharmacare, Blackmores, Coty, and
          → Startrack - 15 (Pharmacare, Blackmores, Coty)

          ==================================================
          11. FEW-SHOT EXAMPLES (MUST FOLLOW)
          ==================================================

          EXAMPLE 1 — CLEAN OCR
          Input:
          Loreal- 14
          9:38 AM
          Sigma- 76
          9:46 AM
          Sanofi 1
          10:50 AM
          Warehouse- 64 total, 43 totes
          10:52 AM
          Stock update-
          8 boxes in the storeroom
          3 cages of Loreal
          10 trolleys of stock

          Output:
          Loreal - 14 @ 9:38am
          Sigma - 76 @ 9:46am
          Sanofi - 1 @ 10:50am
          Warehouse - 64 (total, 43 totes) @ 10:52am

          (Notice: all “Stock update” and related lines are ignored.)

          -----------------------------------------------

          EXAMPLE 2 — MESSY OCR WITH “EDITED” + TRUNCATION
          Input:
          Loreal (round 2)- 62
          edited 11:18 AM
          Startrack - 15 (Pharmacare, Blackmores, Coty, and
          11:18 AM
          Paragoncare- 3
          2:52 PM
          Stock update-
          2.5 cages of stock
          10 trolleys of stock

          Output:
          Loreal (round 2) - 62 @ 11:18am
          Startrack - 15 (Pharmacare, Blackmores, Coty) @ 11:18am
          Paragoncare - 3 @ 2:52pm

          (Again, all stock update lines are completely ignored.)

          ==================================================
          FINAL ENFORCEMENT RULE
          ==================================================

          If the input resembles the examples, ALWAYS follow the same extraction rules and output format. 
          Do NOT improvise, guess, or hallucinate. 
          Do NOT ever treat stock-update lines or storage descriptions as deliveries.
          Your final output MUST be only valid delivery lines in the exact format specified.
          """),

        new UserChatMessage($"""
          Here is the OCR delivery text:\n
          {ocrText}

          Extract ONLY valid delivery lines following the rules and output format you were given.
          """)
      };
      try {
        ChatCompletion completion = await _chatClient.CompleteChatAsync(message);
        return completion.Content[0].Text;
      } catch (Exception e) {
        throw new AppException("OpenAI query failed: " + e.Message);
      }

    }

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
              - Compute the name token’s Y-center. Define a same-row band of ±10 px around that Y (if no other signals).
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
      try {
        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);

        return completion.Content[0].Text;
      } catch (Exception e) {
        throw new AppException("OpenAI query failed: " + e.Message);
      }
    }


  }
}
