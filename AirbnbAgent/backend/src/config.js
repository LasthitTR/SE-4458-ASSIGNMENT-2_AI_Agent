import "dotenv/config";

export const config = {
  port: Number(process.env.PORT ?? 4000),
  ollamaBaseUrl: process.env.OLLAMA_BASE_URL ?? "http://localhost:11434",
  ollamaModel: process.env.OLLAMA_MODEL ?? "llama3.1",
  gatewayBaseUrl:
    process.env.GATEWAY_BASE_URL ?? process.env.GATEWAY_URL ?? "https://emre-gateway-vize.azurewebsites.net",
  defaultClientId: process.env.DEFAULT_CLIENT_ID ?? "final-check-april12-v1"
};
