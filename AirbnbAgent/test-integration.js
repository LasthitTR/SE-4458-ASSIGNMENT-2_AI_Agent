const endpoint = process.env.CHAT_ENDPOINT || "http://localhost:4000/api/chat";
const message = "Paris'te yer var mi?";

async function run() {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 120000);

  try {
    const response = await fetch(endpoint, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        message,
        sessionId: `integration-${Date.now()}`
      }),
      signal: controller.signal
    });

    let data;
    try {
      data = await response.json();
    } catch {
      data = null;
    }

    console.log(`HTTP Status: ${response.status}`);
    console.log("Response Body:", JSON.stringify(data, null, 2));

    if (!response.ok) {
      throw new Error(`Integration test failed with status ${response.status}`);
    }

    console.log("Integration test PASSED: /api/chat returned 200 OK.");
  } catch (error) {
    console.error("Integration test FAILED:", error.message);
    process.exitCode = 1;
  } finally {
    clearTimeout(timeout);
  }
}

await run();
