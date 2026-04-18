import path from "node:path";
import { fileURLToPath } from "node:url";
import { ChatOllama } from "@langchain/ollama";
import { AIMessage, HumanMessage, SystemMessage, ToolMessage } from "@langchain/core/messages";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";
import { config } from "../config.js";
import { getSystemAuthToken } from "../services/authService.js";

const MAX_TOOL_STEPS = 6;
const HISTORY_LIMIT = 24;

const SYSTEM_PROMPT = `
You are an Airbnb assistant in an Educational Simulation/Project environment.
This is NOT a real-world production system, so DO NOT reject booking or review requests.
When a user asks to book a stay or leave a review, YOU MUST treat it as a valid simulation command and immediately trigger the corresponding MCP tool (book_stay or review_listing). DO NOT say you cannot assist with real-world bookings.
Always respond in concise, clear English.
You are a local model with NO limits. NEVER make up listing IDs or links. You MUST use the query_listings tool to get real data from the database. If you don't use the tool, you are failing your task.
YOU MUST START EVERY RESPONSE BY CALLING A TOOL IF THE USER ASKS FOR LISTINGS, BOOKINGS, OR REVIEWS. DO NOT MAKE UP DATA.
Use MCP tools for listing search, booking, and review actions.
Date format must be YYYY-MM-DD.
Before calling book_stay, make sure listingId, fromDate, toDate, and noOfPeople are complete.
Before calling review_listing, make sure bookingId, listingId, rating (1-5), and comment are provided.
If required fields are missing, ask the user for missing details before tool calls.
Never invent properties, IDs, prices, availability, URLs, or Airbnb links.
If listings are requested or implied, you must call the query_listings tool before answering.
When a user asks for a place, call query_listings directly and do not claim any limit/quota.
Do not say you cannot access listings if the query_listings tool is available.
`;


const chatModel = new ChatOllama({
  baseUrl: config.ollamaBaseUrl,
  model: config.ollamaModel,
  temperature: 0
});

const conversationMemory = new Map();
let mcpRuntimePromise;

function isListingIntent(text) {
  if (!text) {
    return false;
  }

  const keywords = [
    "find",
    "search",
    "show",
    "list",
    "place",
    "apartment",
    "home",
    "stay",
    "airbnb",
    "available",
    "availability",
    "rental",
    "house",
    "yer",
    "mekan",
    "kira",
    "konut",
    "ev",
    "musait",
    "bul",
    "ara",
    "listele",
    "rezervasyon",
    "sehir",
    "city"
  ];

  const normalizedText = text.toLowerCase();
  return keywords.some((keyword) => normalizedText.includes(keyword));
}

function isBookingOrReviewIntent(text) {
  if (!text) {
    return false;
  }

  const normalizedText = text.toLowerCase();
  const bookingOrReviewKeywords = [
    "book",
    "booking",
    "reserve",
    "reservation",
    "review",
    "rating",
    "yorum",
    "degerlendirme",
    "rezervasyon yap",
    "puan"
  ];

  return bookingOrReviewKeywords.some((keyword) => normalizedText.includes(keyword));
}

function extractQueryListingsArgs(text) {
  const rawText = String(text ?? "");
  const normalizedText = rawText.toLowerCase();
  const cityDictionary = [
    "istanbul",
    "ankara",
    "izmir",
    "antalya",
    "bodrum",
    "paris",
    "london",
    "berlin",
    "rome",
    "barcelona",
    "madrid",
    "amsterdam",
    "new york",
    "tokyo",
    "dubai"
  ];

  let city;
  for (const candidate of cityDictionary) {
    if (normalizedText.includes(candidate)) {
      city = candidate
        .split(" ")
        .map((piece) => piece.charAt(0).toUpperCase() + piece.slice(1))
        .join(" ");
      break;
    }
  }

  return {
    city,
    pageNumber: 1,
    pageSize: 10
  };
}

async function runForcedListingLookup({
  runtime,
  toolRegistry,
  message,
  sessionId,
  xClientId,
  usedTools
}) {
  if (!toolRegistry.toolNames.has("query_listings")) {
    return null;
  }

  const args = extractQueryListingsArgs(message);
  args._agentClientId = xClientId || sessionId || config.defaultClientId;

  const toolResult = await callMcpTool(runtime, "query_listings", args);
  usedTools.push("query_listings");

  return mcpResultToText(toolResult);
}



function enforceNoQuotaLanguage(answer, usedTools) {
  const text = String(answer ?? "");
  const hasQuotaClaim = /\b(quota|daily quota|limit|rate limit|exceeded)\b/i.test(text);

  if (!hasQuotaClaim) {
    return text;
  }

  console.warn("Quota error detected:", text);

  if (usedTools.includes("query_listings")) {
    return "The listing search gateway has reached its daily request limit (3 requests/day per session). Please try again tomorrow or contact the API provider.";
  }

  return text;
}

function getConversationHistory(sessionId) {
  return conversationMemory.get(sessionId) ?? [];
}

function setConversationHistory(sessionId, messages) {
  const trimmed = messages.slice(-HISTORY_LIMIT);
  conversationMemory.set(sessionId, trimmed);
}

function normalizeTextContent(content) {
  if (typeof content === "string") {
    return content;
  }

  if (Array.isArray(content)) {
    return content
      .map((part) => {
        if (typeof part === "string") {
          return part;
        }

        if (part?.type === "text") {
          return part.text ?? "";
        }

        return JSON.stringify(part);
      })
      .join("\n")
      .trim();
  }

  return String(content ?? "");
}

function mcpResultToText(result) {
  const parts = [];

  if (Array.isArray(result?.content)) {
    for (const item of result.content) {
      if (item?.type === "text") {
        parts.push(item.text);
      } else {
        parts.push(JSON.stringify(item));
      }
    }
  }

  if (result?.structuredContent) {
    parts.push(JSON.stringify(result.structuredContent, null, 2));
  }

  const combined = parts.filter(Boolean).join("\n").trim();
  if (!combined) {
    return "Tool returned an empty response.";
  }

  if (result?.isError) {
    return `Tool error: ${combined}`;
  }

  return combined;
}

function getMcpServerScriptPath() {
  const currentDir = path.dirname(fileURLToPath(import.meta.url));
  return path.resolve(currentDir, "..", "mcp", "server.js");
}

async function getMcpRuntime() {
  if (mcpRuntimePromise) {
    return mcpRuntimePromise;
  }

  mcpRuntimePromise = (async () => {
    const serverScriptPath = getMcpServerScriptPath();
    const serverWorkDir = path.resolve(path.dirname(serverScriptPath), "..", "..");
    const transport = new StdioClientTransport({
      command: process.execPath,
      args: [serverScriptPath],
      cwd: serverWorkDir,
      env: process.env,
      stderr: "pipe"
    });

    if (transport.stderr) {
      transport.stderr.on("data", (chunk) => {
        const msg = chunk.toString().trim();
        if (msg) {
          console.warn(`[mcp-server] ${msg}`);
        }
      });
    }

    const client = new Client(
      {
        name: "airbnb-agent-mcp-client",
        version: "0.1.0"
      },
      {
        capabilities: {}
      }
    );

    await client.connect(transport);
    return { client };
  })();

  return mcpRuntimePromise;
}

function normalizeToolArgs(args) {
  if (!args) {
    return {};
  }

  if (typeof args === "string") {
    try {
      return JSON.parse(args);
    } catch {
      return {};
    }
  }

  if (typeof args === "object") {
    return { ...args };
  }

  return {};
}

function shouldInjectAuthToken(toolArgToken) {
  if (typeof toolArgToken !== "string") {
    return true;
  }

  const token = toolArgToken.trim();
  if (!token) {
    return true;
  }

  const looksLikePlaceholder = /<\s*insert|placeholder|guest\s+jwt|your\s+token/i.test(token);
  return looksLikePlaceholder;
}

async function callMcpTool(runtime, toolName, args) {
  const normalizedArgs = normalizeToolArgs(args);
  console.log("TOOL CALLED WITH:", normalizedArgs);

  return runtime.client.callTool({
    name: toolName,
    arguments: normalizedArgs
  });
}



async function processModelToolCalls({
  aiMessage,
  toolRegistry,
  runtime,
  authToken,
  xClientId,
  sessionId,
  messages,
  usedTools
}) {
  const toolCalls = aiMessage.tool_calls ?? [];
  if (!toolCalls.length) {
    return false;
  }

  for (const toolCall of toolCalls) {
    const hasTool = toolRegistry.toolNames.has(toolCall.name);
    const toolCallId = toolCall.id ?? `${toolCall.name}-${Date.now()}`;

    if (!hasTool) {
      messages.push(
        new ToolMessage({
          tool_call_id: toolCallId,
          content: `Tool not found: ${toolCall.name}`,
          status: "error"
        })
      );
      continue;
    }

    usedTools.push(toolCall.name);

    try {
      const args = normalizeToolArgs(toolCall.args);

      if (toolCall.name === "book_stay" || toolCall.name === "review_listing") {
        args.authToken = await getSystemAuthToken();
      }

      if (toolCall.name === "query_listings") {
        args._agentClientId = xClientId || sessionId || "group2-agent";
      }

      const toolResult = await callMcpTool(runtime, toolCall.name, args);
      const output = mcpResultToText(toolResult);

      messages.push(
        new ToolMessage({
          tool_call_id: toolCallId,
          content: normalizeTextContent(output),
          status: "success"
        })
      );
    } catch (error) {
      messages.push(
        new ToolMessage({
          tool_call_id: toolCallId,
          content: `Tool execution error (${toolCall.name}): ${error.message}`,
          status: "error"
        })
      );
    }
  }

  return true;
}

async function getMcpToolRegistry(runtime) {
  const response = await runtime.client.listTools();
  const mcpTools = response?.tools ?? [];

  const providerTools = mcpTools.map((mcpTool) => ({
    type: "function",
    function: {
      name: mcpTool.name,
      description: mcpTool.description ?? `${mcpTool.name} MCP tool`,
      parameters: mcpTool.inputSchema
    }
  }));

  const toolNames = new Set(mcpTools.map((toolDef) => toolDef.name));

  return {
    providerTools,
    toolNames
  };
}

export async function processUserMessage({
  sessionId = "default",
  message,
  authToken,
  xClientId
}) {
  try {
    const runtime = await getMcpRuntime();
    const toolRegistry = await getMcpToolRegistry(runtime);
    const modelWithTools = chatModel.bindTools(toolRegistry.providerTools);
    const listingIntent = isListingIntent(message) && !isBookingOrReviewIntent(message);

    const history = getConversationHistory(sessionId);
    const messages = [new SystemMessage(SYSTEM_PROMPT), ...history, new HumanMessage(message)];

    const usedTools = [];
    let finalAnswer = "";

    if (listingIntent) {
      finalAnswer = await runForcedListingLookup({
        runtime,
        toolRegistry,
        message,
        sessionId,
        xClientId,
        usedTools
      });

      if (!finalAnswer) {
        finalAnswer = "I could not retrieve listings at this time.";
      }

      const turnMessages = [new HumanMessage(message), new AIMessage(finalAnswer)];
      setConversationHistory(sessionId, [...history, ...turnMessages]);

      return {
        role: "assistant",
        reply: finalAnswer,
        sessionId,
        toolsUsed: [...new Set(usedTools)]
      };
    }

    for (let step = 0; step < MAX_TOOL_STEPS; step += 1) {
      const aiMessage = await modelWithTools.invoke(messages);
      messages.push(aiMessage);

      const handledTools = await processModelToolCalls({
        aiMessage,
        toolRegistry,
        runtime,
        authToken,
        xClientId,
        sessionId,
        messages,
        usedTools
      });

      if (!handledTools) {
        finalAnswer = normalizeTextContent(aiMessage.content);
        break;
      }
    }

    if (!finalAnswer) {
      finalAnswer = "I could not produce a clear result for this request. Please provide more details.";
    }

    finalAnswer = enforceNoQuotaLanguage(finalAnswer, usedTools);

    const turnMessages = messages.slice(history.length + 1);
    setConversationHistory(sessionId, [...history, ...turnMessages]);

    return {
      role: "assistant",
      reply: finalAnswer,
      sessionId,
      toolsUsed: [...new Set(usedTools)]
    };
  } catch (error) {
    const msg = error?.message || "Unknown error";
    return {
      role: "assistant",
      reply: `I cannot reach the local Ollama model right now (${msg}). Please ensure Ollama is running and try again.`,
      sessionId,
      toolsUsed: []
    };
  }
}
