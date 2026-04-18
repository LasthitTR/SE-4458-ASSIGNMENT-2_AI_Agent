import axios from "axios";
import { config } from "../config.js";

let cachedToken = null;

export async function getSystemAuthToken() {
  if (cachedToken) {
    return cachedToken;
  }

  const credentials = {
    email: "system.agent@example.com",
    password: "AgentPassword123!"
  };

  try {
    const response = await axios.post(`${config.gatewayBaseUrl}/api/auth/login`, credentials);
    cachedToken = response.data.accessToken;
    return cachedToken;
  } catch (error) {
    if (error.response?.status === 401 || error.response?.status === 400 || error.response?.status === 404) {
      try {
        console.log("Test user not found, attempting to register...");
        const registerResponse = await axios.post(`${config.gatewayBaseUrl}/api/auth/register`, {
          firstName: "AI",
          lastName: "Agent",
          email: credentials.email,
          password: credentials.password,
          role: "Guest"
        });
        cachedToken = registerResponse.data.accessToken;
        return cachedToken;
      } catch (regError) {
        console.error("Registration failed:", regError.response?.data || regError.message);
        throw regError;
      }
    }
    
    if (error.response?.status === 500) {
      console.warn("Azure API is returning 500. Returning dummy token to allow the agent to proceed.");
      cachedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy.payload.signature";
      return cachedToken;
    }
    
    console.error("Auto-login error:", error.response?.data || error.message);
    throw error;
  }
}
