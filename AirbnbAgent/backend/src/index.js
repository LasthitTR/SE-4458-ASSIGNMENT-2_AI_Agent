import express from "express";
import cors from "cors";
import { config } from "./config.js";
import { processUserMessage } from "./llm/chatService.js";

const app = express();

app.use(cors());
app.use(express.json());

app.get("/health", (_req, res) => {
  res.json({ ok: true, service: "airbnb-agent-backend" });
});

app.post("/api/chat", async (req, res) => {
  const { message, authToken, sessionId, xClientId } = req.body ?? {};

  if (!message || typeof message !== "string") {
    return res.status(400).json({ error: "message alani zorunludur." });
  }

  try {
    const result = await processUserMessage({
      message,
      authToken,
      sessionId,
      xClientId
    });
    return res.json(result);
  } catch (error) {
    console.error("/api/chat error:", error?.stack || error);
    const status = error.response?.status ?? 500;
    const details = error.response?.data ?? error.message;
    return res.status(status).json({ error: "Chat islenemedi.", details });
  }
});

app.listen(config.port, () => {
  console.log(`Airbnb Agent backend ayakta: http://localhost:${config.port}`);
});
