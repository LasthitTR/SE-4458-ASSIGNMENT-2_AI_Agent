import { useEffect, useMemo, useRef, useState } from "react";

function createSessionId() {
  if (globalThis.crypto?.randomUUID) {
    return globalThis.crypto.randomUUID();
  }

  return `sess-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

function getErrorMessage(responseStatus, details) {
  const detailsText = typeof details === "string" ? details : JSON.stringify(details ?? "");

  if (responseStatus >= 500) {
    if (detailsText.toLowerCase().includes("ollama") || detailsText.toLowerCase().includes("11434")) {
      return "An error occurred: local Ollama service is unreachable. Please start Ollama and try again.";
    }

    return "An error occurred. The server cannot process this request right now.";
  }

  return `An error occurred: ${detailsText || "Unknown error"}`;
}

function parseJsonPayload(text) {
  if (typeof text !== "string") {
    return null;
  }

  const trimmed = text.trim();
  if (!trimmed) {
    return null;
  }

  const codeBlockPattern = /^```(?:json)?\s*([\s\S]*?)\s*```$/i;
  const codeBlockMatch = codeBlockPattern.exec(trimmed);
  const candidate = codeBlockMatch ? codeBlockMatch[1].trim() : trimmed;

  try {
    return JSON.parse(candidate);
  } catch {
    // Try to recover from texts that contain a JSON object/array among plain text.
    const firstBrace = Math.min(
      ...[candidate.indexOf("{"), candidate.indexOf("[")].filter((idx) => idx >= 0)
    );

    if (!Number.isFinite(firstBrace)) {
      return null;
    }

    const lastBrace = Math.max(candidate.lastIndexOf("}"), candidate.lastIndexOf("]"));
    if (lastBrace <= firstBrace) {
      return null;
    }

    try {
      return JSON.parse(candidate.slice(firstBrace, lastBrace + 1));
    } catch {
      return null;
    }
  }
}

function extractListingsView(text) {
  const payload = parseJsonPayload(text);
  if (!payload) {
    return null;
  }

  if (Array.isArray(payload)) {
    return {
      listings: payload,
      noResults: payload.length === 0
    };
  }

  if (Array.isArray(payload.items)) {
    return {
      listings: payload.items,
      noResults: payload.items.length === 0
    };
  }

  if (Array.isArray(payload.listings)) {
    return {
      listings: payload.listings,
      noResults: payload.listings.length === 0
    };
  }

  return null;
}

function formatPrice(value) {
  if (typeof value !== "number") {
    return "Price unavailable";
  }

  return `$${value.toFixed(0)}/night`;
}

function getListingImageUrl(listing) {
  if (!listing || typeof listing !== "object") {
    return null;
  }

  const candidate =
    listing.imageUrl ||
    listing.photoUrl ||
    listing.thumbnailUrl ||
    listing.image ||
    listing.coverImageUrl;

  return typeof candidate === "string" && candidate.trim() ? candidate : null;
}

const quickActions = [
  {
    label: "🔍 Query Listing",
    prompt: "Query listings in Paris for 2 guests from 2026-05-10 to 2026-05-15."
  },
  {
    label: "⭐ Book a Listing",
    prompt: "Book listing {listingId} from 2026-06-05 to 2026-06-08 for 2 people."
  },
  {
    label: "📝 Review a Listing",
    prompt: "Review booking {bookingId} for listing {listingId} with a 5-star rating and a short comment."
  }
];

export default function App() {
  const sessionId = useMemo(() => createSessionId(), []);
  const [messages, setMessages] = useState([
    {
      role: "assistant",
      text: "Hi! I'm your Airbnb assistant. Tell me the city, guest count, and dates, and I will find matching listings. You can also book and leave reviews automatically."
    }
  ]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const messagesEndRef = useRef(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, loading]);

  async function dispatchMessage(rawText) {
    const clean = rawText.trim();
    if (!clean || loading) {
      return;
    }

    setMessages((prev) => [...prev, { role: "user", text: clean }]);
    setMessage("");
    setLoading(true);

    try {
      const response = await fetch("/api/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          message: clean,
          sessionId
        })
      });

      let data;
      try {
        data = await response.json();
      } catch {
        data = {};
      }

      if (!response.ok) {
        throw new Error(getErrorMessage(response.status, data?.details ?? data?.error));
      }

      setMessages((prev) => [...prev, { role: "assistant", text: data.reply || "No response" }]);
    } catch (error) {
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          text: error.message || "An error occurred.",
          kind: "error"
        }
      ]);
    } finally {
      setLoading(false);
    }
  }

  async function sendMessage(event) {
    event.preventDefault();
    await dispatchMessage(message);
  }

  async function runQuickAction(text) {
    setMessage(text);
    await new Promise((resolve) => globalThis.requestAnimationFrame(resolve));
    await dispatchMessage(text);
  }

  return (
    <main className="page">
      <section className="chat-shell">
        <header className="chat-header">
          <h1>Airbnb Agent</h1>
          <p>Search listings, book stays, and write reviews using natural language.</p>
          <small className="session-chip">Session: {sessionId}</small>
        </header>

        <div className="messages">
          {messages.map((item, index) => {
            const listingView =
              item.role === "assistant" && item.kind !== "error" ? extractListingsView(item.text) : null;
            const listings = listingView?.listings ?? null;

            return (
              <article
                key={`${item.role}-${index}`}
                className={`bubble ${item.role} ${item.kind === "error" ? "error" : ""} ${
                  listings?.length ? "listing-bubble" : ""
                }`}
              >
                {listings?.length ? (
                  <section className="listing-results">
                    <h3>Available Listings</h3>
                    <div className="listing-grid">
                      {listings.map((listing, listingIndex) => {
                        const imageUrl = getListingImageUrl(listing);
                        const title = listing?.title || "Untitled listing";
                        const city = listing?.city || "Unknown city";
                        const country = listing?.country || "";
                        const location = country ? `${city}, ${country}` : city;
                        const listingId = listing?.id || "";
                        const detailsPrompt = listingId
                          ? `Show details for listing ${listingId}.`
                          : `Show more details for "${title}" in ${city}.`;
                        const bookPrompt = listingId
                          ? `Book listing ${listingId} from 2026-06-05 to 2026-06-08 for 2 people.`
                          : `I want to book "${title}" from 2026-06-05 to 2026-06-08 for 2 people.`;

                        return (
                          <article
                            key={listing?.id || `${title}-${listingIndex}`}
                            className={`listing-card ${imageUrl ? "with-image" : "no-image"}`}
                          >
                            {imageUrl ? (
                              <div className="listing-media" aria-hidden="true">
                                <img src={imageUrl} alt={title} loading="lazy" />
                              </div>
                            ) : null}
                            <div className="listing-content">
                              <h4>{title}</h4>
                              <p className="listing-location">{location}</p>
                              <p className="listing-price">{formatPrice(listing?.price)}</p>
                              <div className="listing-actions">
                                <button
                                  type="button"
                                  className="listing-btn secondary"
                                  onClick={() => runQuickAction(detailsPrompt)}
                                >
                                  Details
                                </button>
                                <button
                                  type="button"
                                  className="listing-btn primary"
                                  onClick={() => runQuickAction(bookPrompt)}
                                >
                                  Book
                                </button>
                              </div>
                            </div>
                          </article>
                        );
                      })}
                    </div>
                  </section>
                ) : listingView?.noResults ? (
                  <div className="listing-empty">
                    <strong>No listings found for this search.</strong>
                    <span>Try another city, wider dates, or fewer filters.</span>
                  </div>
                ) : (
                  item.text
                )}
              </article>
            );
            })}
            {loading && (
              <article className="bubble assistant typing">
                <span className="typing-dots" aria-hidden="true">
                  <i />
                  <i />
                  <i />
                </span>
                <span>AI is thinking...</span>
              </article>
            )}
            <div ref={messagesEndRef} />
          </div>

          <div className="quick-actions-container" aria-label="Quick action buttons">
            {quickActions.map((action) => (
              <button
                key={action.label}
                type="button"
                className="quick-action-btn"
                onClick={() => runQuickAction(action.prompt)}
                disabled={loading}
              >
                {action.label}
              </button>
            ))}
          </div>

          <form className="composer" onSubmit={sendMessage}>
          <input
            type="text"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Example: Find a listing in Paris for 2 guests from 2026-05-10 to 2026-05-15, then book it and leave a 5-star review"
          />
          <button type="submit" disabled={loading}>
            Send
          </button>
        </form>
      </section>
    </main>
  );
}
