import axios from "axios";
import { config } from "../config.js";

const gateway = axios.create({
  baseURL: config.gatewayBaseUrl,
  timeout: 30000
});

function buildHeaders({ clientId, authToken } = {}) {
  const headers = {
    "X-Client-Id": clientId || config.defaultClientId
  };

  if (authToken) {
    headers.Authorization = `Bearer ${authToken}`;
  }

  return headers;
}

function sanitizeObject(obj) {
  return Object.fromEntries(
    Object.entries(obj).filter(([, value]) => value !== undefined && value !== null && value !== "")
  );
}

function logGatewayError(scope, error, details) {
  const response = error?.response ?? {};
  const data = response.data ?? null;

  console.error(`[${scope}] HTTP error`, {
    status: response.status ?? "NO_STATUS",
    statusText: response.statusText ?? "",
    message: error?.message,
    url: details.url,
    method: details.method,
    headers: response.headers ?? null,
    responseData: data,
    requestDetails: details.requestDetails ?? null
  });

  if (data !== null && data !== undefined) {
    console.error(`[${scope}] response.data:`, data);
  }
}

export async function queryListings(criteria = {}, options = {}) {
  const params = sanitizeObject({
    country: criteria.country,
    city: criteria.city,
    capacity: criteria.capacity,
    fromDate: criteria.fromDate,
    toDate: criteria.toDate,
    pageNumber: criteria.pageNumber,
    pageSize: criteria.pageSize
  });

  try {
    const response = await gateway.get("/api/listings", {
      params,
      headers: buildHeaders(options)
    });

    return response.data;
  } catch (error) {
    logGatewayError("gateway.queryListings", error, {
      method: "GET",
      url: `${config.gatewayBaseUrl}/api/listings`,
      requestDetails: { params }
    });

    throw error;
  }
}

export async function bookStay(payload, options = {}) {
  if (!options.authToken) {
    throw new Error("book_stay araci icin guest JWT token gerekli.");
  }

  try {
    const response = await gateway.post(
      "/api/bookings",
      {
        listingId: payload.listingId,
        fromDate: payload.fromDate,
        toDate: payload.toDate,
        noOfPeople: payload.noOfPeople
      },
      {
        headers: buildHeaders(options)
      }
    );

    return response.data;
  } catch (error) {
    logGatewayError("gateway.bookStay", error, {
      method: "POST",
      url: `${config.gatewayBaseUrl}/api/bookings`,
      requestDetails: {
        listingId: payload?.listingId,
        fromDate: payload?.fromDate,
        toDate: payload?.toDate,
        noOfPeople: payload?.noOfPeople
      }
    });

    throw error;
  }
}

export async function reviewListing(payload, options = {}) {
  if (!options.authToken) {
    throw new Error("review_listing araci icin guest JWT token gerekli.");
  }

  try {
    const response = await gateway.post(
      "/api/reviews",
      {
        bookingId: payload.bookingId,
        listingId: payload.listingId,
        rating: payload.rating,
        comment: payload.comment
      },
      {
        headers: buildHeaders(options)
      }
    );

    return response.data;
  } catch (error) {
    logGatewayError("gateway.reviewListing", error, {
      method: "POST",
      url: `${config.gatewayBaseUrl}/api/reviews`,
      requestDetails: {
        listingId: payload?.listingId,
        rating: payload?.rating,
        comment: payload?.comment
      }
    });

    throw error;
  }
}
